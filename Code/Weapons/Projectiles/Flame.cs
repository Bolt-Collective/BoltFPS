namespace Seekers;

public class FlameProjectile : BaseProjectile
{
	[Property] public float SpreadInterval { get; set; } = 0.1f;
	[Property] public float Radius { get; set; } = 10f;
	[Property] public float FireDuration { get; set; } = 10f;
	[Property] public Curve Size { get; set; }
	[Property, RequireComponent] public SoundPointComponent sound { get; set; }

	static GameObject FireParticle => GameObject.GetPrefab( "particles/fire/fire.prefab" );

	protected override void OnStart()
	{
		base.OnStart();
		GetComponent<Rigidbody>().Velocity = Speed;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		sound.Volume = Size.Evaluate( (Duration - TimeUntilDestroy.Relative) / Duration );
	}

	TimeUntil nextSpread;
	public override void Update()
	{
		var size = Size.Evaluate( (Duration - TimeUntilDestroy.Relative) / Duration );
		WorldScale = Vector3.One * size;

		var spreadTrace = Trace( Radius * size );
		var stopTrace = Trace( 0 );

		if ( !spreadTrace.Hit && !stopTrace.Hit )
			return;

		if ( !stopTrace.Hit && nextSpread > 0 )
			return;

		nextSpread = SpreadInterval;

		var trace = stopTrace.Hit ? stopTrace : spreadTrace;

		FireEffect.ApplyFireTo( trace.GameObject, this, FireDuration, Damage );

		AddFireParticle( trace.HitPosition, trace.GameObject, size, FireDuration, Damage );

		if ( stopTrace.Hit )
			GameObject.BroadcastDestroy();
	}

	[Rpc.Broadcast]
	public static void AddFireParticle( Vector3 pos, GameObject target, float scale, float duration, float damage )
	{
		if ( !target.IsValid() )
			return;

		var particle = Particles.CreateParticleSystem( FireParticle, new Transform( pos ), 5, target );
		particle.WorldScale = Vector3.One * scale;

		if ( particle.Components.TryGet<FireTrigger>( out var trigger ) )
		{
			trigger.Duration = duration;
			trigger.Damage = damage;
		}
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( Vector3.Zero, Radius ) );
	}
}
