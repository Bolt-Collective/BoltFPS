using Sandbox.ModelEditor.Nodes;

namespace Seekers;

/// <summary>
/// A component to help deal with props.
/// </summary>
public sealed class PropHelper : Component, Component.ICollisionListener
{
	[Property, Sync] public float Health { get; set; } = 1f;
	[Property, Sync] public Vector3 Velocity { get; set; } = 0f;
	[Property, Sync] public bool Invincible { get; set; } = false;

	[Property, Sync] public bool CanFreeze { get; set; } = true;

	public Prop Prop { get; set; }

	private string _material;

	[Property]
	public string Material
	{
		get
		{
			return _material;
		}
		set
		{
			_material = value;

			SetMaterial( Material );
		}
	}

	public ModelPhysics ModelPhysics { get; set; }
	public Rigidbody Rigidbody { get; set; }

	public Surface Surface => GetSurface();

	private Surface GetSurface()
	{
		if ( !Prop.IsValid() )
			return null;

		var model = Prop.Model;
		if ( !model.IsValid() || model.IsError )
			return null;

		var physics = model.Physics;
		if ( physics == null || physics.Surfaces == null )
			return null;

		Dictionary<Surface, int> counts = new();
		foreach ( var surf in physics.Surfaces )
		{
			if ( surf == null ) continue;
			counts.TryGetValue( surf, out var c );
			counts[surf] = c + 1;
		}

		if ( counts.Count == 0 )
			return null;

		return counts.Aggregate( ( a, b ) => a.Value > b.Value ? a : b ).Key;
	}

	public bool Explosive;

	[Rpc.Broadcast]
	public async void SetMaterial( string material )
	{
		if ( material == null || material == "" )
			return;

		Material setMaterial = null;

		Log.Info( "ppooo" );

		if ( material.Count( x => x == '.' ) == 1 &&
		     !material.EndsWith( ".vmat", StringComparison.OrdinalIgnoreCase ) &&
		     !material.EndsWith( ".vmat_c", StringComparison.OrdinalIgnoreCase ) )
		{
			var package = await Package.Fetch( material, false );

			await package.MountAsync();

			if ( package != null && package.TypeName == "material" && package.Revision != null )
				setMaterial = await Sandbox.Material.LoadAsync( package.GetMeta( "PrimaryAsset", "" ) );
		}
		else
		{
			setMaterial = await Sandbox.Material.LoadAsync( material );
		}

		if ( !setMaterial.IsValid() )
			return;

		foreach ( var modelRenderer in Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
		{
			modelRenderer.MaterialOverride = setMaterial;
		}
	}

	[Property] public List<Joint> Joints { get; set; } = [];

	private Vector3 lastPosition = Vector3.Zero;

	private ModelExplosionBehavior _legacyExplosionData;

	protected override void OnStart()
	{
		Prop ??= GetComponent<Prop>();
		if ( !Prop.IsValid() ) return;
		Prop.OnPropBreak += OnBreak;

		ModelPhysics ??= GetComponent<ModelPhysics>();
		Rigidbody ??= GetComponent<Rigidbody>();

		if ( !Prop.Model.IsValid() )
			return;

		if ( Prop.Model.Data != null )
		{
			var oldExplosiveDataExists = Prop.Model.TryGetData<ModelExplosionBehavior>( out var data );

			Explosive = Prop.Model.Data.Explosive || oldExplosiveDataExists;

			if ( oldExplosiveDataExists )
				_legacyExplosionData = data;
		}

		Health = Prop?.Health ?? 0f;
		Velocity = 0f;

		lastPosition = Prop?.WorldPosition ?? WorldPosition;
	}

	[Rpc.Broadcast]
	public void Damage( float amount )
	{
		if ( IsProxy ) return;
		if ( !Prop.IsValid() ) return;
		if ( Health <= 0f ) return;

		Health -= amount;

		if ( Health <= 0f && !Invincible )
			Prop.Kill();
	}

	public void OnBreak()
	{
		if ( !Prop.IsValid() )
			return;

		var gibs = Prop.CreateGibs();

		if ( gibs.Count > 0 )
		{
			foreach ( var gib in gibs )
			{
				if ( !gib.IsValid() )
					continue;

				gib.Tint = Prop.Tint;
				gib.Tags.Add( "debris" );

				gib.AddComponent<PropHelper>();

				gib.GameObject.NetworkSpawn();
				gib.Network.SetOrphanedMode( NetworkOrphaned.Host );
			}
		}

		if ( Explosive )
		{
			if ( Prop.Model?.Data != null && Prop.Model.Data.Explosive )
			{
				// Use modern explosion data
				var data = Prop.Model.Data;

				Prop.CreateExplosion();

				Explosion(
					"he_grenade_explode",
					WorldPosition,
					data.ExplosionRadius,
					data.ExplosionDamage,
					data.ExplosionForce
				);
			}
			else if ( _legacyExplosionData != null )
			{
				// Use legacy explosion data
				Explosion(
					_legacyExplosionData.Sound,
					WorldPosition,
					_legacyExplosionData.Radius,
					_legacyExplosionData.Damage,
					_legacyExplosionData.Force,
					"weapons/common/effects/medium_explosion.prefab"
				);
			}
		}

		Prop.Model = null; // Prevents prop from spawning more gibs.
		Prop.GameObject.Destroy();
	}

	[Rpc.Host]
	public async void AddDamage( float damage )
	{
		await Task.DelaySeconds( 1f / ProjectSettings.Physics.FixedUpdateFrequency + 0.05f );

		Damage( damage );
	}

	[Rpc.Broadcast]
	public void BroadcastAddDamage( float damage )
	{
		AddDamage( damage );
	}

	protected override void OnFixedUpdate()
	{
		if ( Prop.IsValid() )
		{
			Velocity = (Prop.WorldPosition - lastPosition) / Time.Delta;
			lastPosition = Prop.WorldPosition;
		}
	}

	private ModelPropData GetModelPropData()
	{
		if ( Prop.Model.IsValid() && !Prop.Model.IsError && Prop.Model.TryGetData( out ModelPropData propData ) )
		{
			return propData;
		}

		ModelPropData defaultData = new() { Health = -1, };

		return defaultData;
	}

	public float Mass => GetMass();

	private float GetMass()
	{
		if ( Rigidbody.IsValid() )
			return Rigidbody.Mass;

		float mass = 0;
		foreach ( var body in ModelPhysics.Bodies )
		{
			mass += body.Component.Mass;
		}

		return mass;
	}

	[ConVar( "bolt_disablepropdamage", ConVarFlags.Cheat )]
	public static bool DisableImpactDamage { get; set; } = false;

	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		if ( DisableImpactDamage )
			return;

		if ( IsProxy ) return;

		var propData = GetModelPropData();
		if ( propData == null ) return;

		var minImpactSpeed = 500;
		if ( minImpactSpeed <= 0.0f ) minImpactSpeed = 500;

		float impactDmg = Mass / 10;
		if ( impactDmg <= 0.0f ) impactDmg = 10;

		float speed = collision.Contact.Speed.Length;

		if ( speed > minImpactSpeed )
		{
			// I take damage from high speed impacts
			if ( Health > 0 )
			{
				var damage = speed / minImpactSpeed * impactDmg;
				Damage( damage );
			}

			var other = collision.Other;

			// Whatever I hit takes more damage
			if ( other.GameObject.IsValid() && other.GameObject != GameObject )
			{
				var damage = speed / minImpactSpeed * impactDmg * 0.3f;

				Log.Info( damage );

				if ( other.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
				{
					propHelper.Damage( damage );
				}
				else if ( other.GameObject.Root.Components.TryGet<HealthComponent>( out var player ) )
				{
					player.TakeDamage( this, damage );
				}
			}
		}
	}

	public async void Explosion( string sound,
		Vector3 position, float radius, float damage,
		float forceScale, string particle = "weapons/common/effects/medium_explosion.prefab" )
	{
		await GameTask.Delay( Game.Random.Next( 50, 250 ) );

		BroadcastExplosion( sound, position );

		if ( particle != null && particle != "" )
		{
			var particleGameObject = GameObject.GetPrefab( particle );
			Particles.CreateParticleSystem( particleGameObject, new Transform( position, Rotation.Identity ), 10 );
		}

		if ( Networking.IsHost )
			Seekers.Explosion.AtPoint( position, radius, damage, this, true, this,
				new Curve( new Curve.Frame( 1.0f, 1.0f ), new Curve.Frame( 0.0f, 0.0f ) ) );
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	public static void BroadcastExplosion( string path, Vector3 position )
	{
		if ( string.IsNullOrEmpty( path ) )
		{
			Sound.Play( "he_grenade_explode", position );
			return;
		}

		if ( path.StartsWith( "sound/" ) || path.StartsWith( "sounds/" ) )
		{
			var soundEvent = ResourceLibrary.Get<SoundEvent>( path );
			if ( !soundEvent.IsValid() )
			{
				Sound.Play( "he_grenade_explode", position );
				return;
			}

			Sound.Play( soundEvent, position );
			return;
		}

		Sound.Play( path, position );
	}
}
