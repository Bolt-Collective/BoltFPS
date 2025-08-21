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

	[Property] public float BulletHitScareAmount = 0.1f;

	[Property] public float ReactionTime = 0.4f;

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

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( !Networking.IsHost )
			return;

		if ( CoverGenerator.Instance == null || !CoverGenerator.Instance.coversGenerated )
			return;

		Gun = ToolObject?.GetComponent<NPCGun>() ?? null;

		ClosestEnemy = GetNearest(true)?.Knowable ?? null;

		if ( !ClosestEnemy.IsValid() )
		{
			None();
			return;
		}

		CurrentState = StateManager.GetBest();

		CanHitEnemy = CanHitEnemy( ClosestEnemy );
		if ( !CanHitEnemy.HasValue )
			timeSinceNoHit = 0;

		if ( DoShootEnemy )
			Gun?.Shoot( CanHitEnemy.Value, 0.1f );

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

	public override void Animate()
	{
		AnimationHelper.HoldType = CurrentTool.IsValid() ? CurrentTool.HoldTypes : AnimationHelper.HoldTypes.None;
		AnimationHelper.DuckLevel = Crouch ? 1f : 0;
		AnimationHelper.WithVelocity( Agent.Velocity );
	}

	public void CalculateState()
	{
		StateManager.Set( States.Reload, -1 );
		var healthScare = (HealthComponent.Health / HealthComponent.MaxHealth).Clamp( 0.2f, 1 );

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

	public void None()
	{

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
		AttackPosition();
	}
	TimeUntil nextRotate;
	public void AttackPosition()
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

		Rotate();
	}

	public void Rotate()
	{
		if ( nextRotate > 0 )
			return;

		nextRotate = RotateTime.GetValue();

		bool negative = Game.Random.Next( 0, 2 ) == 1;
		var rotation = new Angles( 0, negative ? -RotateAngle.GetValue() : RotateAngle.GetValue(), 0 );

		var targetPos = WorldPosition.RotateAround( ClosestEnemy.GameObject.WorldPosition, rotation );

		var distance = WorldPosition.Distance( targetPos );

		var path = ActiveMesh.GetSimplePath( WorldPosition, targetPos );
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

		Body.GameObject.SetParent( Game.ActiveScene );
		Body.AddComponent<TimedDestroyComponent>().Time = 5;
		Body.UseAnimGraph = false;
		Body.RenderType = ModelRenderer.ShadowRenderType.On;
		if ( DeadModel.IsValid() )
			Body.Model = DeadModel;
		Body.Tags.Add( "ragdoll" );

		var modelPhysics = Body.AddComponent<ModelPhysics>();
		modelPhysics.Model = Body.Model;
		modelPhysics.Renderer = Body;
		foreach ( var body in modelPhysics.Bodies )
		{
			body.Component.Velocity += Agent.Velocity + damageInfo.Force / 15000;
		}

		GameObject.Destroy();
	}
}
