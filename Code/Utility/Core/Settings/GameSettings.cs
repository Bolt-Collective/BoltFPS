namespace Seekers;

public class GameSettings
{
	// [Title( "View Bob" ), Group( "Game" ), Range( 0, 100, 5f )]
	// public float ViewBob { get; set; } = 100f;

	[Title( "Show Dot" ), Group( "Crosshair" )]
	public bool ShowCrosshairDot { get; set; } = true;

	[Title( "Dynamic" ), Group( "Crosshair" )]
	public bool DynamicCrosshair { get; set; } = true;

	[Title( "Length" ), Group( "Crosshair" ), Range( 2, 50, 1 )]
	public float CrosshairLength { get; set; } = 2;

	[Title( "Width" ), Group( "Crosshair" ), Range( 1, 10, 1 )]
	public float CrosshairWidth { get; set; } = 2;

	[Title( "Distance" ), Group( "Crosshair" ), Range( -5, 50, 0.1f )]
	public float CrosshairDistance { get; set; } = 15;

	[Title( "Color" ), Group( "Crosshair" )]
	public Color CrosshairColor { get; set; } = Color.White;
}

public partial class GameSettingsSystem
{
	private static GameSettings current { get; set; }
	public static GameSettings Current
	{
		get
		{
			if ( current is null ) Load();
			return current;
		}
		set
		{
			current = value;
		}
	}

	public static string FilePath => "gamesettings.json";

	public static void Save()
	{
		FileSystem.Data.WriteJson( FilePath, Current );
	}

	public static void Load()
	{
		Current = FileSystem.Data.ReadJson<GameSettings>( FilePath, new() );
	}
}
