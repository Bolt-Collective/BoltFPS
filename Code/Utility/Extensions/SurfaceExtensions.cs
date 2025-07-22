using Seekers;

/// <summary>
/// Extensions for Surfaces
/// </summary>
public static partial class SurfaceExtensions
{
	[ConVar( ConVarFlags.Saved )] public static bool bolt_impactparticles { get; set; } = true;

	public static void DoBulletImpact( this Surface self, SceneTraceResult tr, bool playSound = true )
	{
		var surf = ReplaceSurface( self );

		if ( tr.Hit )
		{
			GameObject particle = self.PrefabCollection.BulletImpact;
			if ( particle == null ) particle = self.PrefabCollection.BluntImpact;
			if (surf == null) return;
			
			while ( particle == null && !surf.IsDefault() )
			{
				surf = ReplaceSurface( surf?.GetProperBaseSurface() );
				particle = surf?.PrefabCollection.BulletImpact;
				if ( particle == null ) particle = surf?.PrefabCollection.BluntImpact;
			}

			var sound = surf?.SoundCollection.Bullet;

			while ( !sound.IsValid() && !surf.IsDefault() )
			{
				if ( !sound.IsValid() )
				{
					var basetemp = surf?.GetProperBaseSurface();
					surf = ReplaceSurface( basetemp );
				}

				sound = surf?.SoundCollection.Bullet;
			}

			if ( sound.IsValid() )
			{
				SoundExtensions.BroadcastSound( sound, tr.EndPosition );
			}

			if ( particle != null )
			{
				var impact = particle.Clone();
				impact.WorldPosition = tr.EndPosition;
				impact.SetParent( tr.GameObject, true );
				impact.WorldRotation = Rotation.LookAt( -tr.Normal );
			}
		}
	}

	public static SoundHandle PlayPhysicsCollisionSound( this Surface self, Vector3 position, float speed = 320.0f )
	{
		var surf = ReplaceSurface( self );

		float volume = speed / 1000.0f;
		if ( volume > 1.0f ) volume = 1.0f;

		if ( volume < 0.001f ) return default;

		var sound = surf.SoundCollection.ImpactHard;

		if ( surf is SurfaceImpacts surface )
		{
			sound = surface.SoundCollection.ImpactHard;
			if ( speed < 430f || !sound.IsValid() )
			{
				sound = surface.SoundCollection.ImpactHard;
				if ( !sound.IsValid() )
				{
					sound = self.SoundCollection.ImpactHard;
				}
			}

			if ( speed < 280f && surface.SoundCollection.ImpactHard.IsValid() )
			{
				sound = surface.SoundCollection.ImpactHard;
			}

			if ( speed < 130f || !sound.IsValid() )
			{
				sound = surface.SoundCollection.ImpactSoft;
				if ( !sound.IsValid() )
				{
					sound = self.SoundCollection.ImpactSoft;
				}
			}
		}
		else
		{
			if ( speed < 130f || !sound.IsValid() )
			{
				sound = self.SoundCollection.ImpactSoft;
			}
		}

		if ( !sound.IsValid() )
			return default;

		var s = Sound.Play( sound, position );
		if ( s is not null )
		{
			s.Volume *= volume;
		}

		return s;
	}

	public static bool IsDefault( this Surface surf )
	{
		if ( surf == null ) return true;
		if ( surf.ResourceName.Contains( "default" ) ) return true;
		return false;
	}

	static Surface GetProperBaseSurface( this Surface self )
	{
		Surface baseSurface = null;
		if ( self.ResourceName == "default" ) return null;
		baseSurface = ResourceLibrary.Get<Surface>( self.BaseSurface );
		return baseSurface;
	}

	public static Surface ReplaceSurface( this Surface self )
	{
		if ( self == null )
		{
			ResourceLibrary.TryGet<SurfaceImpacts>( $"surfaces/default.simpact", out var defaultsurf );
			return defaultsurf;
		}

		var surf = self;
		var name = self.ResourceName;
		if ( ResourceLibrary.TryGet<SurfaceImpacts>( $"surfaces/{name}.simpact", out var surfNew ) )
		{
			surf = surfNew;
		}

		if ( surf == null )
		{
			ResourceLibrary.TryGet<SurfaceImpacts>( $"surfaces/default.simpact", out var defaultsurf );
			return defaultsurf;
		}

		return surf;
	}
}
