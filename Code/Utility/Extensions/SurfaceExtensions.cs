using Seekers;

/// <summary>
/// Extensions for Surfaces
/// </summary>
public static partial class SurfaceExtensions
{
	/// <summary>
	/// Create a particle effect and play an impact sound for this surface being hit by a bullet
	/// </summary>
	public static List<LoadedSurface> LoadedSurfaces = new();

	public static GameObject DoBulletImpact( this Surface self, SceneTraceResult tr, bool playSound = true )
	{
		//
		// Drop a decal
		//
		var decalPath = Game.Random.FromList( self.ImpactEffects.BulletDecal );

		var surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( decalPath ) && surf != null )
		{
			decalPath = Game.Random.FromList( surf.ImpactEffects.BulletDecal );
			surf = surf.GetBaseSurface();
		}

		if ( !string.IsNullOrWhiteSpace( decalPath ) )
		{
			if ( ResourceLibrary.TryGet<DecalDefinition>( decalPath, out var decal ) )
			{
				if ( !tr.GameObject.IsValid() )
					return null;

				var go = new GameObject
				{
					Name = decalPath,
					Parent = tr.GameObject,
					WorldPosition = tr.EndPosition,
					WorldRotation = Rotation.LookAt( -tr.Normal )
				};

				var renderer = tr.GameObject.GetComponentInChildren<SkinnedModelRenderer>();
				if ( tr.Bone > -1 && renderer.IsValid() )
				{
					var bone = renderer.GetBoneObject( tr.Bone );

					go.SetParent( bone );
				}

				var randomDecal = Game.Random.FromList( decal.Decals );

				var decalRenderer = go.AddComponent<DecalRenderer>();
				decalRenderer.Material = randomDecal.Material;
				decalRenderer.Size = new Vector3( randomDecal.Width.GetValue(), randomDecal.Height.GetValue(),
					randomDecal.Depth.GetValue() );

				go.AddComponent<TimedDestroyComponent>().Time = 10;
				go.NetworkSpawn( null );
				go.Network.SetOrphanedMode( NetworkOrphaned.Host );
				go.DestroyAsync( 10f );
			}
		}

		//
		// Make an impact sound
		//
		var sound = self.Sounds.Bullet;

		surf = self.GetBaseSurface();
		while ( string.IsNullOrWhiteSpace( sound ) && surf != null )
		{
			sound = surf.Sounds.Bullet;
			surf = surf.GetBaseSurface();
		}

		if ( playSound && !string.IsNullOrWhiteSpace( sound ) )
		{
			SoundExtensions.BroadcastSound( sound, tr.EndPosition );
		}

		//
		// Get us a particle effect
		//

		SurfaceImpacts particleReference = null;

		var path = $"surfaces/{self.ResourceName}.simpact";

		foreach ( var loadedSurface in LoadedSurfaces )
		{
			if ( loadedSurface.Path == path )
			{
				particleReference = loadedSurface.Surface;
			}
		}

		if ( !particleReference.IsValid() )
		{
			LoadedSurfaces.Add( new LoadedSurface( path ) );
			particleReference = LoadedSurfaces.Last().Surface;
		}

		if ( particleReference?.BulletImpact.IsValid() ?? false )
		{
			var particle = CreateParticle( particleReference.BulletImpact, tr.GameObject, tr.EndPosition,
				Rotation.LookAt( tr.Normal ) );
			return particle;
		}

		return default;
	}

	public static GameObject CreateParticle( GameObject particle, GameObject parent, Vector3 position,
		Rotation rotation, bool spawn = true )
	{
		var go = particle.Clone();
		go.WorldTransform = new Transform( position, rotation );
		go.AddComponent<TimedDestroyComponent>().Time = 10;

		if ( spawn )
		{
			go.NetworkSpawn( null );
			go.Network.SetOrphanedMode( NetworkOrphaned.Host );
			go.DestroyAsync( 5f );
		}

		return go;
	}

	public static SoundHandle PlayPhysicsCollisionSound( this Surface self, Vector3 position, float speed = 320.0f )
	{
		float volume = speed / 1000.0f;
		if ( volume > 1.0f ) volume = 1.0f;

		if ( volume < 0.001f ) return default;

		var sound = self.Sounds.ImpactHard;

		if ( self is SurfaceImpacts surf )
		{
			sound = surf.Sounds.ImpactHard;
			if ( speed < 430f || string.IsNullOrWhiteSpace( sound ) )
			{
				sound = surf.Sounds.ImpactHard;
				if ( string.IsNullOrWhiteSpace( sound ) )
				{
					sound = self.Sounds.ImpactHard;
				}
			}

			if ( speed < 280f && !string.IsNullOrWhiteSpace( surf.Sounds.ImpactHard ) )
			{
				sound = surf.Sounds.ImpactSoft;
			}

			if ( speed < 130f || string.IsNullOrWhiteSpace( sound ) )
			{
				sound = surf.Sounds.ImpactSoft;
				if ( string.IsNullOrWhiteSpace( sound ) )
				{
					sound = self.Sounds.ImpactSoft;
				}
			}
		}

		if ( string.IsNullOrWhiteSpace( sound ) )
			return default;

		var s = Sound.Play( sound, position );
		if ( s is not null )
		{
			s.Volume *= volume;
		}

		return s;
	}
}
