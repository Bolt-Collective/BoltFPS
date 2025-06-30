using Seekers;

/// <summary>
/// Extensions for Surfaces
/// </summary>
public static partial class SurfaceExtensions
{
	[ConVar( ConVarFlags.Saved )] public static bool bolt_impactparticles { get; set; } = true;

	public static void DoBulletImpact( this Surface self, SceneTraceResult tr, bool playSound = true )
	{
		if ( tr.Hit )
		{
			var impactPrefab = self.PrefabCollection.BulletImpact ??
			                   self.GetBaseSurface()?.PrefabCollection.BulletImpact;
			if ( impactPrefab is not null )
			{
				var impact = impactPrefab.Clone();
				impact.WorldPosition = tr.EndPosition;
				impact.SetParent( tr.GameObject, true );
				impact.WorldRotation = Rotation.LookAt( -tr.Normal );
			}
		}
	}

	public static SoundHandle PlayPhysicsCollisionSound( this Surface self, Vector3 position, float speed = 320.0f )
	{
		float volume = speed / 1000.0f;
		if ( volume > 1.0f ) volume = 1.0f;

		if ( volume < 0.001f ) return default;

		var sound = self.SoundCollection.ImpactHard;

		if ( self is SurfaceImpacts surf )
		{
			sound = surf.SoundCollection.ImpactHard;
			if ( speed < 430f || !sound.IsValid() )
			{
				sound = surf.SoundCollection.ImpactHard;
			}

			if ( speed < 280f && !surf.SoundCollection.ImpactHard.IsValid() )
			{
				sound = surf.SoundCollection.ImpactSoft;
			}

			if ( speed < 130f || !sound.IsValid() )
			{
				sound = surf.SoundCollection.ImpactSoft;
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

	public static Surface GetRealSurface( this Surface self )
	{
		Surface baseSurface = null;
		if ( self.ResourceName == "default" ) return null;
		baseSurface = ResourceLibrary.Get<Surface>( self.BaseSurface );
		return baseSurface;
	}

	public static Surface Replace( this Surface self )
	{
		var surf = self;

		if ( surf == null )
		{
			ResourceLibrary.TryGet<SurfaceImpacts>( $"surfaces/default.simpact", out var defaultSurface );
			return defaultSurface;
		}
		else
		{
			var surfaceName = self.ResourceName;
			ResourceLibrary.TryGet<SurfaceImpacts>( $"surfaces/{surfaceName}.simpact", out var surfNew );

			surf = surfNew;
		}

		return surf;
	}
}
