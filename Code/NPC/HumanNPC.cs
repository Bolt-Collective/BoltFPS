using Sandbox.Citizen;
using Sandbox.Events;
using Sandbox.VR;
using static Sandbox.UI.PanelTransform;

namespace Seekers;
public class HumanNPC : NPC, IGameEventHandler<BulletHitEvent>
{
	
	[Property] public SkinnedModelRenderer Body { get; set; }
	[Property] public AnimationHelper AnimationHelper { get; set; }
	[Property] public float CrouchCoverHeight { get; set; } = 45;
	[Property] public float BulletHitAwarenessRange { get; set; } = 256;

	[Property] public float BulletHitScareAmount { get; set; } = 0.1f;

	[Property] public float ReactionTime { get; set; } = 0.4f;

	[Property] public float AimLerp { get; set; } = 20f;

	[Property] public RangedFloat AttackTime { get; set; } = new RangedFloat( 2, 5 );
	[Property] public RangedFloat CoverTime { get; set; } = new RangedFloat( 2, 5 );
	[Property] public RangedFloat RotateTime { get; set; } = new RangedFloat( 1, 2 );
	[Property] public RangedFloat RotateAngle { get; set; } = new RangedFloat( 10, 30 );

	[Sync] public bool Crouch { get; set; }
	public Knowable ClosestEnemy { get; set; }

	public NPCGun Gun { get; set; }

	public Range hideTime = new Range( 10, 30 ); 

	public enum States
	{
		Cover,
		Attack,
		Reload
	}

	private StateManager<States> StateManager = new();

	protected override void OnStart()
	{
		base.OnStart();
		CoverGenerator.doGenerateCover = true;
	}

	[Property] States CurrentState;
	public bool DoShootEnemy => CanHitEnemy.HasValue && timeSinceNoHit > ReactionTime;
	Vector3? CanHitEnemy;
	RealTimeSince timeSinceNoHit;
	Vector3 aimPoint;
	public override void Think()
	{
		Log.Info( "Thinking" );
		if ( CoverGenerator.Instance == null || !CoverGenerator.Instance.coversGenerated )
			return;

		Gun = ToolObject?.GetComponent<NPCGun>() ?? null;

		ClosestEnemy = GetNearest(true)?.Knowable ?? null;
		Agent.UpdateRotation = !ClosestEnemy.IsValid() && Agent.Velocity.Length > 10;
		if ( !ClosestEnemy.IsValid() || !ClosestEnemy.GameObject.IsValid() )
		{
			None();
			return;
		}

		CurrentState = StateManager.GetBest();

		CanHitEnemy = CanHitEnemy( ClosestEnemy );
		if ( !CanHitEnemy.HasValue )
			timeSinceNoHit = 0;

		if ( DoShootEnemy )
		{
			aimPoint = aimPoint.LerpTo( CanHitEnemy.Value, AimLerp * Time.Delta );
			var shot = Gun?.Shoot( aimPoint, 0.1f ) ?? false;
			if ( shot )
				ShootEffects();
		}
		else
			aimPoint = ClosestEnemy.WorldPosition;

		CalculateState();

		switch(CurrentState)
		{
			case States.Cover:
				Cover();
				break;
			case States.Attack:
				Attack();
				break;
			case States.Reload:
				Reload();
				break;
		}

		var enemyDir = (ClosestEnemy.GameObject.WorldPosition - WorldPosition).WithZ(0).Normal;
		GameObject.WorldRotation = Rotation.LookAt( enemyDir );

		//AttackPosition();
	}

	[Rpc.Broadcast]
	public void ShootEffects()
	{
		Body.Set( "b_attack", true );
	}

	public override void Animate()
	{
		var holdType = CurrentTool.IsValid() ? CurrentTool.HoldTypes : AnimationHelper.HoldTypes.None;
		var duckLevel = Crouch ? 1f : 0;
		var velocity = Agent.Velocity;
		SetAnimation(holdType, duckLevel, velocity);
	}

	[Rpc.Broadcast]
	public void SetAnimation(AnimationHelper.HoldTypes holdType, float duckLevel, Vector3 velocity)
	{
		AnimationHelper.HoldType = holdType;
		AnimationHelper.DuckLevel = duckLevel;
		AnimationHelper.WithVelocity( velocity );
	}

	public void CalculateState()
	{
		StateManager.Set( States.Reload, -1 );
		var healthScare = (HealthComponent.Health / HealthComponent.MaxHealth).Clamp( 0.2f, 1 );

		if (Gun.IsValid() && Gun.Ammo <= 0)
		{
			StateManager.Set( States.Reload, 4 );
			return;
		}
		else
		{
			StateManager.Set( States.Reload, -1 );
		}

		switch ( CurrentState )
		{
			case States.Cover:
				TimedStateSwitch( States.Cover, States.Attack, CoverTime, healthScare );
				break;
			case States.Attack:
				TimedStateSwitch( States.Attack, States.Cover, CoverTime, 1 + ( 1 - healthScare ) );
				break;
		}

	}
	public void TimedStateSwitch(States current, States target, RangedFloat time, float mod)
	{
		StateManager.Change( current, -(1 / time.GetValue()) * Time.Delta * mod );
		StateManager.Set( target, 0 );

		if ( StateManager.Get( current ) < 0.1f )
		{
			StateManager.Set( target, 1 );
			StateManager.Set( current, 0 );
		}
	}
	TimeUntil nextWonder;
	[Property] public RangedFloat WonderTime { get; set; } = new RangedFloat( 1, 4 );
	[Property] public float WonderRange { get; set; } = 250;
	public void None()
	{
		if ( nextWonder > 0 )
			return;

		nextWonder = WonderTime.GetValue();

		Vector3 wonderPoint = ActiveMesh.GetRandomPoint( WorldPosition, WonderRange ) ?? default;

		if ( wonderPoint == default )
			return;

		Agent.MoveTo( wonderPoint );
		
	}

	public void Cover()
	{
		AttackPosition();

		Crouch =
			CurrentCover != null &&
			CurrentCover.Height < CrouchCoverHeight &&
			WorldPosition.Distance( CurrentCover.Position ) < 32;
	}

	public void Attack()
	{
		Crouch = false;
		Agent.MoveTo( CanHitEnemy.HasValue ? WorldPosition : ClosestEnemy.Position );
	}

	public void Reload()
	{
		if ( !Gun.IsValid() )
			return;

		if (!Gun.reloading && Gun.Ammo <= 0)
		{
			Gun.Reload();
			ReloadEffects();
		}

		AttackPosition(0.5f);
	}

	[Rpc.Broadcast]
	public void ReloadEffects()
	{
		Body.Set( "b_reload", true );
	}


	TimeUntil nextRotate;
	public void AttackPosition( float rotateMod = 1 )
	{
		FindCover( ClosestEnemy );

		if ( CurrentCover != null )
		{
			Agent.MoveTo( CurrentCover.Position );
			return;
		}

		var targetPosition = MaintainAttackDistance( ClosestEnemy );

		if ( targetPosition.HasValue )
		{
			Agent.MoveTo( targetPosition.Value );
		}

		Rotate(rotateMod);
	}

	public void Rotate(float timeMod = 1)
	{
		if ( nextRotate > 0 )
			return;

		nextRotate = RotateTime.GetValue() * timeMod;

		bool negative = Game.Random.Next( 0, 2 ) == 1;
		var rotation = new Angles( 0, negative ? -RotateAngle.GetValue() : RotateAngle.GetValue(), 0 );

		var targetPos = WorldPosition.RotateAround( ClosestEnemy.GameObject.WorldPosition, rotation );

		var distance = WorldPosition.Distance( targetPos );

		var path = ActiveMesh.CalculatePath( new() { Start = WorldPosition, Target =  targetPos } ); ;
		var length = GetPathLength( path );

		if ( length > distance * 2 )
			return;

		Agent.MoveTo( targetPos );
	}


	//protected override void OnUpdate()
	//{
	//	Gizmo.Draw.IgnoreDepth = true;
	//	Gizmo.Draw.Color = Color.White;
	//	Gizmo.Draw.SolidSphere( WorldPosition, 5 );
	//}

	public void OnGameEvent( BulletHitEvent eventArgs )
	{
		if ( eventArgs.position.Distance( WorldPosition ) > BulletHitAwarenessRange )
			return;

		StateManager.Change( States.Attack, -BulletHitScareAmount );
		StateManager.Change( States.Cover, BulletHitScareAmount );
	}

	[Property] public GameObject DeathPrefab { get; set; }
	[Property] public Model DeadModel { get; set; }

	public override void OnKilled( DamageInfo damageInfo )
	{
		if ( DeathPrefab.IsValid() )
		{
			DeathPrefab.Clone( new Transform( WorldPosition, WorldRotation ) );
			GameObject.Destroy();
			return;
		}

		CreateRagdoll( Body, damageInfo, DeadModel );

		GameObject.Destroy();
	}
}
