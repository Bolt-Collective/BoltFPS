namespace Seekers;

public abstract class NPCGun : NPCTool
{
	[Property] public GameObject Muzzle { get; set; }
	[Property] public GameObject TracerEffect { get; set; }
	[Property] public float Ammo { get; set; }
	[Property] public float MaxAmmo { get; set; }
	[Property] public float Rate { get; set; }
	[Property] public float Damage { get; set; } = 15;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( !Networking.IsHost )
			return;
	}
	TimeUntil nextShot;
	public virtual void Shoot( Vector3 pos, float spread = 0.1f )
	{
		if ( nextShot > 0 && Ammo > 0 )
			return;

		nextShot = 1/Rate;

		ShootBullet( Muzzle.WorldPosition, (pos - Muzzle.WorldPosition).Normal, Damage, 1, Spread + spread );
	}

	[Property] public float Spread { get; set; } = 1;
	public float SpreadIncrease;
	private GameObject Tracer
	{
		get
		{
			if ( TracerEffect.IsValid() ) return TracerEffect;

			return GameObject.GetPrefab( $"/weapons/common/effects/tracer.prefab" );
		}
	}

	List<Surface> hitSurfaces = new();

	float shotTime;
	int shots = 0;
	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float damage, float bulletSize,
		float spreadOverride = -1 )
	{
		if ( shotTime != Time.Now )
		{
			shots = 0;
			hitSurfaces = new();
		}

		shotTime = Time.Now;

		shots++;

		var spread = Spread + SpreadIncrease;
		if ( spreadOverride > -1 )
			spread = spreadOverride;

		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		foreach ( var tr in BaseWeapon.TraceBullet( GameObject.Root, pos, pos + forward * 5000, bulletSize ) )
		{
			var tagMaterial = "";

			if ( tr.Tags.Any() )
			{
				foreach ( var tag in tr.Tags )
				{
					if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
					{
						tagMaterial = tag.Remove( 0, 2 );
						break;
					}
				}
			}

			Surface surface = tagMaterial == ""
				? tr.Surface
				: (Surface.FindByName( tagMaterial ) ?? tr.Surface);

			surface.DoBulletImpact( tr, !hitSurfaces.Contains( surface ) || shots < 3 );
			DoTracer( tr.StartPosition, tr.EndPosition, tr.Distance, true );

			hitSurfaces.Add( surface );

			if ( !tr.GameObject.IsValid() ) continue;


			var hitboxTags = tr.GetHitboxTags();

			if ( hitboxTags.Contains( HitboxTags.Head ) )
				damage *= 2;

			var calcForce = forward * 25000 * damage;

			BaseWeapon.DoDamage( tr.GameObject, damage, calcForce, tr.HitPosition, hitboxTags, inflictor: this, ownerTeam: Owner.Team );
		}
	}

	private GameObject muzzle = GameObject.GetPrefab( "weapons/common/effects/muzzle.prefab" );
	protected virtual void ShootEffects()
	{
		AttachParticleSystem( muzzle, "muzzle" );
	}

	[Rpc.Broadcast]
	public void AttachParticleSystem( GameObject prefab, string attachment, float time = 1, GameObject parent = null )
	{
		if ( !prefab.IsValid() )
			return;

		Transform transform = muzzle.WorldTransform;

		Particles.MakeParticleSystem( prefab, transform, time, GameObject );
	}

	public virtual void DoTracer( Vector3 startPosition, Vector3 endPosition, float distance, bool muzzle )
	{
		if ( !BaseWeapon.IsNearby( startPosition ) && !BaseWeapon.IsNearby( endPosition ) ) return;

		var effect =
			Tracer?.Clone( new CloneConfig
			{
				Transform = new Transform().WithPosition( Muzzle.WorldPosition ),
				StartEnabled = true
			} );
		if ( effect.IsValid() && effect.GetComponentInChildren<Tracer>() is { } tracer )
		{
			tracer.EndPoint = endPosition;
		}
	}

}
