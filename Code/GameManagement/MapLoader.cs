using Sandbox;
using XMovement;
using static Sandbox.Gizmo;

namespace Seekers;

public partial class MapLoader : MapInstance
{
	[Sync] public bool IsLoadingMap { get; set; }
	public bool ClientIsLoadingMap { get; set; }

	public static MapLoader Instance;

	protected override void OnFixedUpdate()
	{
		var pawn = Pawn.Local;

		if ( !pawn.IsValid() || !pawn.Controller.IsValid() )
			return;
		pawn.Controller.CanMove = !ClientIsLoadingMap;
	}

	protected override void OnAwake()
	{
		Instance = this;
		OnMapLoaded += OnMapLoad;
		TagProps();
	}

	public void Cleanup()
	{
		foreach ( var component in Game.ActiveScene.GetAllComponents<DestroyOnMapCleanup>() )
		{
			component.GameObject.Destroy();
		}

		UnloadMap();
		OnMapLoad();
	}

	public void OnMapLoad()
	{
		ClientIsLoadingMap = false;

		RespawnPlayers();
		TagProps();

		if ( Networking.IsHost )
		{
			IsLoadingMap = false;
		}
	}

	[Rpc.Host]
	public void RespawnPlayers()
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		foreach ( var player in Scene.GetAllComponents<Pawn>().ToArray() )
		{
			if ( player.IsProxy )
				continue;

			var randomSpawnPoint = Random.Shared.FromArray( spawnPoints );
			if ( randomSpawnPoint is null ) continue;

			player.WorldPosition = randomSpawnPoint.WorldPosition + Vector3.Up * 10;

			if ( !player.Controller.IsValid() )
				continue;

			player.Controller.EyeAngles = randomSpawnPoint.WorldRotation.Angles();
		}
	}

	[Rpc.Broadcast]
	void TagProps()
	{
		var allProps = Components.GetAll<Prop>( FindMode.EverythingInSelfAndDescendants );

		foreach ( var prop in allProps )
		{
			if ( !prop.IsValid() )
				continue;

			if ( !prop.Tags.Contains( "worldprop" ) )
				prop.Tags.Add( "worldprop" );
		}
	}

	[Rpc.Broadcast]
	public static async void ChangeMap( string map )
	{
		Instance.ClientIsLoadingMap = true;

		if ( Instance.MapName != map )
		{
			await LoadMap( map );
		}
		else
		{
			Instance.IsLoadingMap = true;
			Instance.ClientIsLoadingMap = true;
			Instance.OnMapLoaded();
		}

		foreach ( var envmap in Instance.GetComponentsInChildren<EnvmapProbe>() )
		{
			envmap.Dirty = true;
		}
	}

	static async Task LoadMap( string map )
	{
		foreach ( var gameObject in Instance.GameObject.Children )
		{
			gameObject.Destroy();
		}

		var sceneLoadOptions = new SceneLoadOptions { IsAdditive = true };

		var mapPackage = await Package.FetchAsync( map, false );

		if ( mapPackage == null ) return;

		var primaryAsset = mapPackage.GetMeta<string>( "PrimaryAsset" );

		if ( primaryAsset.EndsWith( ".scene" ) )
		{
			await mapPackage.MountAsync();
			Instance.UnloadMap();
			Instance.Enabled = false;
			var sceneFile = mapPackage.GetMeta<SceneFile>( "PrimaryAsset" );
			sceneLoadOptions.SetScene( sceneFile );
			List<GameObject> NonMapObjects = new();
			foreach ( var gameobject in Game.ActiveScene.Children )
			{
				NonMapObjects.Add( gameobject );
			}

			Game.ActiveScene.Load( sceneLoadOptions );
			var children = new List<GameObject>( Game.ActiveScene.Children );
			foreach ( var gameobject in children )
			{
				if ( NonMapObjects.Contains( gameobject ) )
					continue;
				gameobject.SetParent( Instance.GameObject );
			}

			Instance.OnMapLoaded?.Invoke();
		}
		else
		{
			Instance.Enabled = true;
			Instance.MapName = map;
		}
	}

	protected override void OnCreateObject( GameObject go, Sandbox.MapLoader.ObjectEntry kv )
	{
		if ( !Game.IsPlaying ) return;

		if ( !Networking.IsHost )
		{
			if ( kv.TypeName.Contains( "prop_" ) || kv.TypeName == "ent_door" ||
			     kv.TypeName == "func_shatterglass" )
				go.Destroy();
			return;
		}

		if ( kv.TypeName == "prop_physics" && Game.IsPlaying )
		{
			go.Components.Create<DestroyOnMapCleanup>();
			go.NetworkSpawn( null );
		}
	}
	
	[ConCmd("changemap")]
	private static void ChangeMapCmd( string map )
	{
		if ( string.IsNullOrWhiteSpace( map ) )
		{
			Log.Error( "Map name cannot be empty." );
			return;
		}

		if ( Instance == null )
		{
			Log.Error( "MapLoader instance is not available." );
			return;
		}

		if ( Instance.ClientIsLoadingMap )
		{
			Log.Warning( "Already loading a map, please wait." );
			return;
		}

		ChangeMap( map );
		Instance.Cleanup();
	}
}
