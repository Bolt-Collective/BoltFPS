using Sandbox;
using Sandbox.Citizen;
using Sandbox.Events;
using Sandbox.Utility;
using Sandbox.VR;
using static Sandbox.UI.PanelTransform;

namespace Seekers;
public class ZombieNPC : NPC
{

	[Group( "References" )]
	[Property] public SkinnedModelRenderer Body { get; set; }

	[Group( "Movement" )]
	[Property] public float WalkSpeed { get; set; } = 23.3f;
	[Group( "Movement" )]
	[Property] public float RunSpeed { get; set; } = 70f;
	[Group( "Movement" )]
	[Property] public RangedFloat WonderTime { get; set; } = new RangedFloat( 10, 30 );

	[Group( "Stamina" )]
	[Property] public RangedFloat StaminaDecayTime { get; set; } = new RangedFloat( 4, 7 );
	[Group( "Stamina" )]
	[Property] public RangedFloat StaminaRegenTime { get; set; } = new RangedFloat( 4, 7 );

	[Group( "Combat" )]
	[Property] public float AttackDistance { get; set; } = 30f;
	[Group( "Combat" )]
	[Property] public float MaxAttackAngle { get; set; } = 75f;
	[Group( "Combat" )]
	[Property] public float AttackAttemptDistance { get; set; } = 60f;
	[Group( "Combat" )]
	[Property] public float AttackHeight { get; set; } = 60f;
	[Group( "Combat" )]
	[Property] public float StopDistance { get; set; } = 30f;
	[Group( "Combat" )]
	[Property] public int AttackCount { get; set; } = 4;
	[Group( "Combat" )]
	[Property] public float Damage { get; set; } = 25;

	[Group( "Targeting" )]
	[Property] public float TargetAccuracy { get; set; } = 0.12f;
	[Group( "Targeting" )]
	[Property] public float AccuracyScale { get; set; } = 1;

	[Group( "Death" )]
	[Property] public Model DeadModel { get; set; }

	[Group( "Sounds" )]
	[Property] public SoundEvent IdleSound { get; set; }

	[Group( "Stamina" )]
	[Property] public RangedFloat IdleSoundTime { get; set; } = new RangedFloat( 5, 7 );

	public Knowable ClosestEnemy { get; set; }

	float stamina = 0;
	bool regen = true;
	bool cannotAttack = false;
	float accRandom;
	float type;
	float regenTime;
	float decayTime;
	bool hit;

	TimeUntil attackDuration;
	TimeUntil nextIdleSound;
	RealTimeSince attackTime;
	RealTimeSince sinceCantAttack;

	protected override void OnStart()
	{
		type = Game.Random.Next( 0, 101 ) / 100f;
		Body.OnGenericEvent += Event;
		accRandom = Game.Random.Next( 0, 1000 );
		regenTime = StaminaRegenTime.GetValue();
		decayTime = StaminaDecayTime.GetValue();
		base.OnStart();
	}

	public void Event(SceneModel.GenericEvent genericEvent)
	{
		if ( genericEvent.Type == "Attack" )
			SetAttack(genericEvent.Float);

		if ( genericEvent.Type == "FinishedAttack" )
			cannotAttack = false;
	}
	
	public void SetAttack(float duration)
	{
		attackDuration = duration;
		hit = false;
	}

	public override void Think()
	{
		// if finishedattack event is not caught then reset cannotAttack
		if ( attackTime > 4 )
			cannotAttack = false;

		if (regen)
		{
			stamina += (1 / regenTime) * Time.Delta;
			if ( stamina > 1 )
			{
				regen = false;
			}
		}
		else
		{
			stamina -= (1 / decayTime) * Time.Delta;
			if ( stamina < 0 )
			{
				regen = true;
			}
		}

		var speed = regen ? WalkSpeed : RunSpeed;

		IdleSoundPlayer();

		Agent.MaxSpeed = speed;
		Agent.Acceleration = speed * 5;
		ClosestEnemy = GetNearest( true )?.Knowable ?? null;
		if ( !ClosestEnemy.IsValid() || !ClosestEnemy.GameObject.IsValid() )
		{
			None();
			return;
		}

		Attacking();
	}

	public void IdleSoundPlayer()
	{
		if ( nextIdleSound > 0 )
			return;

		SoundExtensions.FollowSound( IdleSound, GameObject );

		nextIdleSound = IdleSoundTime.GetValue();
	}

	protected override void OnFixedUpdate()
	{
		if ( attackDuration > 0 )
			AttackBox();
		else
			hit = false;

		base.OnFixedUpdate();
	}

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

			var dir = (ray.HitPosition - WorldPosition).Normal.WithZ(0);

			if ( MathF.Abs(Vector3.GetAngle( WorldTransform.Forward.WithZ( 0 ).Normal, dir )) > MaxAttackAngle )
				return;

			if ( !Team.IsEnemy( ray.GameObject ) )
				continue;

			if ( ray.GameObject.IsProxy )
				continue;

			if (Components.TryGet<FireEffect>(out var fireEffect))
			{
				FireEffect.ApplyFireTo(ray.GameObject, this, fireEffect.Duration, fireEffect.Damage );
			}

			hit = true;
			BaseWeapon.DoDamage( ray.GameObject, Damage, WorldTransform.Forward * 100000, ray.EndPosition, ownerTeam: Team, attacker: this );
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

	public void TryAttack(float dis)
	{
		if ( dis > AttackAttemptDistance )
			return;

		if ( cannotAttack )
		{
			sinceCantAttack = 0;
			return;
		}

		if ( sinceCantAttack < 0.4f )
			return;

		regen = false;
		attackTime = 0;
		cannotAttack = true;

		Attack();
	}

	public void Attack()
	{
		AttackAnimation(Game.Random.Next(AttackCount));
	}

	[Rpc.Broadcast]
	public void AttackAnimation(int attack)
	{	
		Body.Set( "attackchoice", attack );
		Body.Set( "attack", true );
	}

	public override void Animate()
	{
		var move = Agent.Velocity.Length;
		var runScale = MathX.LerpInverse( move, 0, RunSpeed );
		SetAnimation( type, move, runScale );
	}

	[Rpc.Broadcast]
	public void SetAnimation( float type, float move, float runScale )
	{
		Body.Set("type", type);
		Body.Set( "move", move );
		Body.Set( "runscale", runScale );
	}

	public override void OnKilled( DamageInfo damageInfo )
	{
		CreateRagdoll( Body, damageInfo, DeadModel );

		GameObject.Destroy();
	}

	protected override void DrawGizmos()
	{
		var pos = Vector3.Forward * AttackDistance * 0.5f + Vector3.Up * AttackHeight / 2;
		var size = new Vector3( AttackDistance, AttackDistance, AttackHeight );
		Gizmo.Draw.LineBBox(BBox.FromPositionAndSize(pos, size));
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.LineThickness = 2;
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Zero + (Vector3.Forward * new Angles( 0, MaxAttackAngle, 0 )) * 250 );
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Zero + (Vector3.Forward * new Angles( 0, -MaxAttackAngle, 0 )) * 250 );
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Forward * AttackAttemptDistance );
	}
}
