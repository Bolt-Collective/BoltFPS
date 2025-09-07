using Sandbox;
using Sandbox.Citizen;
using Sandbox.Events;
using Sandbox.Utility;
using Sandbox.VR;
using static Sandbox.UI.PanelTransform;

namespace Seekers;
public class ZombieNPC : NPC
{

	[Property] public SkinnedModelRenderer Body { get; set; }

	public Knowable ClosestEnemy { get; set; }

	[Property] public RangedFloat WonderTime { get; set; } = new RangedFloat( 10, 30 );

	[Property] public float SpeedMult { get; set; } = 1;

	// do not change if using native zombie model
	[Property] public float BaseWalkSpeed { get; set; } = 23.3f;
	[Property] public float BaseRunSpeed { get; set; } = 70f;

	[Property] public RangedFloat StaminaDecayTime { get; set; } = new RangedFloat(4,7);

	[Property] public RangedFloat StaminaRegenTime { get; set; } = new RangedFloat( 4, 7 );

	[Property] public float AttackDistance { get; set; } = 30f;

	[Property] public float AttackAttemptDistance { get; set; } = 60f;

	[Property] public float AttackHeight { get; set; } = 60f;

	[Property] public float StopDistance { get; set; } = 30f;

	[Property] public float TargetAccuracy { get; set; } = 0.12f;
	[Property] public float AccuracyScale { get; set; } = 1;

	float stamina = 1;
	bool regen;

	bool cannotAttack = false;

	float accRandom;

	float type;
	protected override void OnStart()
	{
		type = Game.Random.Next( 0, 101 ) / 100f;
		Body.OnGenericEvent += Event;
		accRandom = Game.Random.Next( 0, 1000 );
		base.OnStart();
	}

	public void Event(SceneModel.GenericEvent genericEvent)
	{
		if ( genericEvent.Type == "Attack" )
			SetAttack(genericEvent.Float);

		if ( genericEvent.Type == "FinishedAttack" )
			cannotAttack = false;
	}

	TimeUntil attackDuration;
	
	public void SetAttack(float duration)
	{
		attackDuration = duration;
	}

	public override void Think()
	{

		// if finishedattack event is not caught then reset cannotAttack
		if ( attackTime > 4 )
			cannotAttack = false;

		if (regen)
		{
			stamina += (1 / StaminaRegenTime.GetValue()) * Time.Delta;
			if ( stamina > 1 )
				regen = false;
		}
		else
		{
			stamina -= (1 / StaminaDecayTime.GetValue()) * Time.Delta;
			if ( stamina < 0 )
				regen = true;
		}

		var speed = regen ? 23.3f : 70;

		Agent.MaxSpeed = speed * SpeedMult;
		ClosestEnemy = GetNearest( true )?.Knowable ?? null;
		if ( !ClosestEnemy.IsValid() || !ClosestEnemy.GameObject.IsValid() )
		{
			None();
			return;
		}

		Attacking();
	}

	protected override void OnFixedUpdate()
	{
		if ( attackDuration > 0 )
			AttackBox();
		else
			hit = false;

		base.OnFixedUpdate();
	}

	bool hit;
	public void AttackBox()
	{
		if ( hit )
			return;

		var pos = WorldPosition + WorldTransform.Forward * AttackDistance * 0.5f + Vector3.Up * AttackHeight / 2;

		var size = new Vector3( AttackDistance, AttackDistance, AttackHeight );

		for (int i = 0; i < 5; i++ )
		{
			var ray = Scene.Trace.Ray( pos, pos )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement" )
			.IgnoreStatic()
			.IgnoreGameObjectHierarchy( GameObject )
			.Size( size )
			.Run();

			if ( !ray.Hit )
				return;

			if ( !Team.IsEnemy( ray.GameObject ) )
				continue;

			if ( ray.GameObject.IsProxy )
				continue;

			hit = true;
			BaseWeapon.DoDamage( ray.GameObject, 10, WorldTransform.Forward * 100000, ray.EndPosition, ownerTeam: Team );
			break;
		}
	}

	public void None()
	{
		Agent.MaxSpeed = 0;
	}

	public void Attacking()
	{
		var dis = WorldPosition.Distance( ClosestEnemy.Position );
		if (dis < StopDistance)
			Agent.MaxSpeed = 0;

		var offsetX = -1 + Noise.Perlin( (Time.Now + accRandom) * AccuracyScale, 0 ) * 2;
		var offsetY = -1 + Noise.Perlin( (Time.Now - accRandom) * AccuracyScale, 0 ) * 2;

		var dir = new Vector3( offsetX, offsetY, 0 );

		Agent.MoveTo(ClosestEnemy.Position + dir * dis * TargetAccuracy);

		TryAttack( dis );
	}

	RealTimeSince attackTime;
	public void TryAttack(float dis)
	{
		if ( dis > AttackAttemptDistance )
			return;

		if ( cannotAttack )
			return;

		attackTime = 0;
		cannotAttack = true;

		Attack();
	}

	public void Attack()
	{
		AttackAnimation();
	}

	[Rpc.Broadcast]
	public void AttackAnimation()
	{
		Body.Set( "attack", true );
	}

	public override void Animate()
	{
		var move = Agent.Velocity.Length;
		var runScale = MathX.LerpInverse( move, 0, 70 );
		SetAnimation( type, move, runScale );
	}

	[Rpc.Broadcast]
	public void SetAnimation( float type, float move, float runScale )
	{
		Body.Set("type", type);
		Body.Set( "move", move );
		Body.Set( "runscale", runScale );
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

	protected override void DrawGizmos()
	{
		var pos = Vector3.Forward * AttackDistance * 0.5f + Vector3.Up * AttackHeight / 2;
		var size = new Vector3( AttackDistance, AttackDistance, AttackHeight );
		Gizmo.Draw.LineBBox(BBox.FromPositionAndSize(pos, size));
	}
}
