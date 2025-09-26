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

	[Group( "References" )]
	[Property] public Dismemberment Dismemberment { get; set; }

	[Group( "Movement" )]
	[Property] public float WalkSpeed { get; set; } = 23.3f;

	[Group( "Movement" )]
	[Property] public float RunSpeed { get; set; } = 70f;

	[Group( "Movement" )]
	[Property] public float CrawlSpeed { get; set; } = 30f;

	[Group( "Movement" )]
	[Property] public float Crawl1Speed { get; set; } = 30f;

	[Group( "Movement" )]
	[Property] public RangedFloat WonderTime { get; set; } = new RangedFloat( 10, 30 );

	[Group( "Movement" )]
	[Property] public float VaultTime { get; set; } = 2.3f;

	[Group( "Stamina" )]
	[Property] public RangedFloat StaminaDecayTime { get; set; } = new RangedFloat( 4, 7 );
	[Group( "Stamina" )]
	[Property] public RangedFloat StaminaRegenTime { get; set; } = new RangedFloat( 4, 7 );

	[Group( "Combat" )]
	[Property] public float AttackDistance { get; set; } = 30f;

	[Group( "Combat" )]
	[Property] public float AttackHeight { get; set; } = 60f;

	[Group( "Combat" )]
	[Property] public float CrawlAttackHeight { get; set; } = 60f;

	[Group( "Combat" )]
	[Property] public float CrawlAttackOffset { get; set; } = 60f;

	[Group( "Combat" )]
	[Property] public float CrawlAttackDistance { get; set; } = 30f;

	[Group( "Combat" )]
	[Property] public float CrawlAttackAttemptDistance { get; set; } = 60f;

	[Group( "Combat" )]
	[Property] public float MaxAttackAngle { get; set; } = 75f;
	[Group( "Combat" )]
	[Property] public float AttackAttemptDistance { get; set; } = 60f;

	[Group( "Combat" )]
	[Property] public float StopDistance { get; set; } = 30f;

	[Group( "Combat" )]
	[Property] public List<int> LeftAttacks { get; set; }

	[Group( "Combat" )]
	[Property] public List<int> RightAttacks { get; set; }

	[Group( "Combat" )]
	[Property] public List<int> LeftCrawlAttacks { get; set; }

	[Group( "Combat" )]
	[Property] public List<int> RightCrawlAttacks { get; set; }

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
	[Group( "Sounds" )]
	[Property] public SoundEvent AttackSound { get; set; }

	[Group( "Stamina" )]
	[Property] public RangedFloat IdleSoundTime { get; set; } = new RangedFloat( 5, 7 );

	[Property, Sync]
	public bool Crawl { get; set; }

	public Knowable ClosestEnemy { get; set; }

	float stamina = 0;
	bool regen = true;
	bool cannotAttack = false;
	float accRandom;
	float type;
	float regenTime;
	float decayTime;
	bool hit;
	bool move;
	bool run;

	[Sync]
	float attackDistance { get; set; }

	[Sync]
	float attackHeight { get; set; }

	[Sync]
	float attackOffset { get; set; }

	[Sync]
	float attackAttemptDistance { get; set; }

	TimeUntil attackDuration;
	TimeUntil nextIdleSound;
	RealTimeSince attackTime;
	RealTimeSince sinceCantAttack;

	Dismemberment.Dismemberable leftArmDis;
	Dismemberment.Dismemberable rightArmDis;
	Dismemberment.Dismemberable leftLegDis;
	Dismemberment.Dismemberable rightLegDis;

	protected override void OnStart()
	{
		type = Game.Random.Next( 0, 101 ) / 100f;
		Body.OnGenericEvent += Event;
		accRandom = Game.Random.Next( 0, 1000 );
		regenTime = StaminaRegenTime.GetValue();
		decayTime = StaminaDecayTime.GetValue();

		leftArmDis = Dismemberment.GetDismemberable( "Left Arm" );
		rightArmDis = Dismemberment.GetDismemberable( "Right Arm" );
		leftLegDis = Dismemberment.GetDismemberable( "Left Leg" );
		rightLegDis = Dismemberment.GetDismemberable( "Right Leg" );

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

		if (vaulting)
		{
			Vaulting();
			return;
		}

		if ( rightLegDis.Health <= 0 || leftLegDis.Health <= 0 )
			Crawl = true;

		if ( regen )
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

		if ( attackTime > 4 )
			cannotAttack = false;

		move = false;

		var speed = (Body.RootMotion.Position / Time.Delta).Length;

		if ( Crawl )
		{
			SetAttackStats( CrawlAttackDistance, CrawlAttackHeight, CrawlAttackOffset, CrawlAttackAttemptDistance );
		}
		else
		{
			run = !regen;
			SetAttackStats( AttackDistance, AttackHeight, 0, AttackAttemptDistance );
		}

		IdleSoundPlayer();

		Agent.MaxSpeed = 0;
		Agent.Velocity = (Body.RootMotion.Position / Time.Delta) * Body.PlaybackRate * 2f * Body.WorldRotation;
		Agent.UpdateRotation = Agent.Velocity.Length > 5;
		ClosestEnemy = GetNearest( true )?.Knowable ?? null;
		if ( !ClosestEnemy.IsValid() || !ClosestEnemy.GameObject.IsValid() )
		{
			None();
			return;
		}

		Attacking();
	}

	public void Vaulting()
	{
		if (ExitVault < 0)
		{
			Agent.Enabled = true;
			vaulting = false;
			Log.Info( "poo" );
			return;
		}
		Agent.Enabled = false;
		var vel = (Body.RootMotion.Position / Time.Delta) * Body.PlaybackRate * 4f * Body.WorldRotation;
		WorldPosition += vel * Time.Delta;
		WorldRotation.SlerpTo( VaultLinkObject.WorldRotation, Time.Delta );
	}

	public void SetAttackStats(float attackDis, float attackHei, float attackOff, float attackAttempt)
	{
		attackDistance = attackDis;
		attackHeight = attackHei;
		attackOffset = attackOff;
		attackAttemptDistance = attackAttempt;
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

		var pos = WorldPosition + WorldTransform.Forward * (attackDistance * 0.5f + attackOffset) + Vector3.Up * attackHeight / 2;

		var size = new Vector3( attackDistance, attackDistance, attackHeight );

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
		move = dis > StopDistance;

		var offsetX = -1 + Noise.Perlin( (Time.Now + accRandom) * AccuracyScale, 0 ) * 2;
		var offsetY = -1 + Noise.Perlin( (Time.Now - accRandom) * AccuracyScale, 0 ) * 2;

		var dir = new Vector3( offsetX, offsetY, 0 );

		Agent.MoveTo( ClosestEnemy.Position + dir * dis * TargetAccuracy );

		TryAttack( dis );
	}


	public void TryAttack(float dis)
	{
		if ( dis > attackAttemptDistance )
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
		SoundExtensions.BroadcastSound( AttackSound, WorldPosition );

		var choices = new List<int>();

		if ( leftArmDis.Health > 0 )
			choices.AddRange( Crawl ? LeftCrawlAttacks : LeftAttacks );

		if ( rightArmDis.Health > 0 )
			choices.AddRange( Crawl ? RightCrawlAttacks : RightAttacks );

		if ( choices.Count <= 0 )
			return;

		AttackAnimation( choices[Game.Random.Next(choices.Count)] );
	}

	[Rpc.Broadcast]
	public void AttackAnimation(int attack)
	{	
		Body.Set( "attackchoice", attack );
		Body.Set( "attack", true );
	}

	public override void Animate()
	{
		SetAnimation( move, run, leftArmDis.Health > 0, rightArmDis.Health > 0 );
	}

	[Rpc.Broadcast]
	public void SetAnimation( bool move, bool run, bool leftArm, bool rightArm )
	{
		Body.Set("crawl", Crawl );
		Body.Set( "move", move );
		Body.Set( "run", run );
		Body.Set( "leftarm", leftArm );
		Body.Set( "rightarm", rightArm );
	}

	public override void OnKilled( DamageInfo damageInfo )
	{
		CreateRagdoll( Body, damageInfo, DeadModel );

		GameObject.Destroy();
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.Color = Color.Red;

		var pos = Vector3.Forward * AttackDistance * 0.5f + Vector3.Up * AttackHeight / 2;
		var size = new Vector3( AttackDistance, AttackDistance, AttackHeight );
		Gizmo.Draw.LineBBox(BBox.FromPositionAndSize(pos, size));

		Gizmo.Draw.Color = Color.Green;

		pos = Vector3.Forward * (CrawlAttackDistance * 0.5f + CrawlAttackOffset) + Vector3.Up * CrawlAttackHeight / 2;
		size = new Vector3( CrawlAttackDistance, CrawlAttackDistance, CrawlAttackHeight );

		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( pos, size ) );

		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.LineThickness = 2;
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Zero + (Vector3.Forward * new Angles( 0, MaxAttackAngle, 0 )) * 250 );
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Zero + (Vector3.Forward * new Angles( 0, -MaxAttackAngle, 0 )) * 250 );
		Gizmo.Draw.Line( Vector3.Zero, Vector3.Forward * MathF.Max( CrawlAttackAttemptDistance, AttackAttemptDistance) );
	}

	bool vaulting = false;
	GameObject VaultLinkObject;
	TimeUntil ExitVault;
	public void VaultLink( GameObject Link )
	{
		if ( Crawl )
			return;
		VaultLinkObject = Link;
		vaulting = true;
		VaultAnim();
		ExitVault = VaultTime / Body.PlaybackRate;
	}

	[Rpc.Broadcast]
	public void VaultAnim()
	{
		Body.Set( "vault", true );
	}
}
