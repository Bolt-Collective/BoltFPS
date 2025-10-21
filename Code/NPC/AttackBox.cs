using static Sandbox.ModelPhysics;

namespace Seekers;


public class AnimAttackBox : NPC
{
	[Group( "References" )]
	[Property] public SkinnedModelRenderer Body { get; set; }

	[Group( "Combat" )]
	[Property] public float Damage { get; set; } = 25;

	[Sync]
	public float attackDistance { get; set; }

	[Sync]
	public float attackHeight { get; set; }

	[Sync]
	public float attackOffset { get; set; }

	[Group( "Combat" )]
	[Property] public float MaxAttackAngle { get; set; } = 75f;

	public TimeUntil attackDuration;

	public bool hit;

	public bool cannotAttack = false;

	protected override void OnStart()
	{
		Body.OnGenericEvent += Event;

		base.OnStart();
	}

	public void Event( SceneModel.GenericEvent genericEvent )
	{
		Log.Info( "attack box cunt" );
		if ( genericEvent.Type == "Attack" )
			SetAttack( genericEvent.Float );

		if ( genericEvent.Type == "FinishedAttack" )
			cannotAttack = false;
	}

	protected override void OnFixedUpdate()
	{
		if ( attackDuration > 0 )
			AttackBox();
		else
			hit = false;

		base.OnFixedUpdate();
	}

	public void SetAttack( float duration )
	{
		alreadyHit = new List<GameObject>();
		attackDuration = duration;
		hit = false;
	}

	public List<GameObject> alreadyHit = new();
	public void AttackBox()
	{
		var pos = WorldPosition + WorldTransform.Forward * (attackDistance * 0.5f + attackOffset) + Vector3.Up * attackHeight / 2;

		var size = new Vector3( attackDistance, attackDistance, attackHeight );
		
		for ( int i = 0; i < 5; i++ )
		{
			var rayBuilt = Scene.Trace.Ray( pos, pos )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement" )
			.IgnoreStatic()
			.IgnoreGameObjectHierarchy( GameObject )
			.Size( size );

			foreach ( var hit in alreadyHit )
				rayBuilt = rayBuilt.IgnoreGameObjectHierarchy( hit.Root );

			var ray = rayBuilt.Run();

			if ( !ray.Hit )
				break;

			alreadyHit.Add( ray.GameObject );

			var dir = (ray.HitPosition - WorldPosition).WithZ( 0 ).Normal;

			if ( MathF.Abs( Vector3.GetAngle( WorldTransform.Forward.WithZ( 0 ).Normal, dir ) ) > MaxAttackAngle )
				return;

			if ( !Team.IsEnemy( ray.GameObject ) )
				continue;

			if ( ray.GameObject.IsProxy )
				continue;

			if ( Components.TryGet<FireEffect>( out var fireEffect ) )
			{
				FireEffect.ApplyFireTo( ray.GameObject, this, fireEffect.Duration, fireEffect.Damage );
			}

			hit = true;
			BaseWeapon.DoDamage( ray.GameObject, Damage, WorldTransform.Forward * 100000, ray.EndPosition, ownerTeam: Team, attacker: this );
		}
	}

	public void DrawAttackBox( float attackDistance, float attackHeight, float attackOffset = 0 )
	{
		var pos = Vector3.Forward * (attackDistance * 0.5f + attackOffset) + Vector3.Up * attackHeight / 2;
		var size = new Vector3( attackDistance, attackDistance, attackHeight );
		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( pos, size ) );
	}
}
