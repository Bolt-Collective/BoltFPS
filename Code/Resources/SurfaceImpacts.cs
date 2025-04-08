[GameResource( "Surface Extension", "simpact", "Surface Impacts", Icon = "💥", IconBgColor = "#111" )]
public class SurfaceImpacts : Surface
{
	public GameObject BulletImpact { get; set; }
}

public class LoadedSurface
{
	public string Path { get; set; }

	private SurfaceImpacts _surface;

	public LoadedSurface( string surfacePath )
	{
		Path = surfacePath;
		_surface = ResourceLibrary.Get<SurfaceImpacts>( Path );
	}

	public SurfaceImpacts Surface
	{
		get
		{
			if ( !_surface.IsValid() )
			{
				_surface = ResourceLibrary.Get<SurfaceImpacts>( Path );
			}

			return _surface;
		}
	}
}
