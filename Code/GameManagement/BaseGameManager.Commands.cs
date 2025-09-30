using BoltFPS;

namespace Seekers;

public partial class BaseGameManager
{
	[ConCmd( "toast" )]
	public static void DisplayToast( string text, float duration = 5 )
	{
		ToastNotification.Current?.AddToast( text, duration );
	}
	
	[ConCmd( "kill" )]
	public static void Suicide()
	{
		Pawn.Local.TakeDamage( 1000 );
	}
	
	[ConCmd( "respawn_entities" )]
	public static void Cleanup()
	{
		foreach ( var component in Game.ActiveScene.GetAll<DestroyOnMapCleanup>() )
		{
			component.GameObject.Destroy();
		}

		if ( MapLoader.Instance != null )
		{
			MapLoader.Instance.Cleanup();
		}
	}
}
