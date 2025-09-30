using Sandbox;

namespace Seekers;

[EditorHandle("ui/input/controllers/controller_icon_ps3.png")]
public partial class BaseGameManager : SingletonComponent<BaseGameManager>, Component.INetworkListener
{
	[Property] public bool StartServer { get; set; } = true;
	[Property, Header("Player")] public PrefabFile PlayerPrefab { get; set; }

	[Property, Header("Player")] public PrefabFile ClientPrefab { get; set; }
	
	[Property] [Group( "Dev" )] public readonly List<long> PlayerWhitelist = new()
	{
		76561198043979097, // trende
		76561198193615491, // trollface
		76561199407136830, // paths
		76561198289339378 // graffiti
	};

	public static List<long> Devs = new()
	{
		76561198043979097, // trende
		76561198193615491, // trollface
		76561199407136830, // paths
		76561198289339378 // graffiti
	};

	[Property] public List<long> BannedPlayers = new();

	[Property] [Group( "Dev" )] public bool DevMode { get; set; }

	[Property] public List<ItemResource> StartingWeapons { get; set; }
	[Property] public bool RespawnOnTeamChange { get; set; } = true;
	[Property, ShowIf("RespawnOnTeamChange", false)] public List<Team> RespawnTeams { get; set; }

	bool INetworkListener.AcceptConnection( Connection channel, ref string reason )
	{
		if ( DevMode && !PlayerWhitelist.Contains( channel.SteamId.Value ) )
		{
			reason = "This is a developer lobby, you can't join!";
			return false;
		}

		if ( BannedPlayers.Contains( channel.SteamId.Value ) )
		{
			reason = "You have been kicked from this game.";
			return false;
		}

		return true;
	}

	void INetworkListener.OnActive( Connection channel )
	{
		var existingClient = Scene.Components.GetAll<Client>().FirstOrDefault( x => x.Connection == channel );
		if ( existingClient.IsValid() && !Application.IsDedicatedServer )
			return;

		var clientObj = ClientPrefab.GetScene().Clone();
		clientObj.Name = $"Client - {channel.DisplayName}";
		clientObj.Tags.Add( "engine" );

		var client = clientObj.GetComponent<Client>();
		client.SteamId = channel.SteamId;
		clientObj.NetworkSpawn( channel );

		client.AssignConnection( channel );
		client.Team = TeamManager.Instance.DefaultTeam;

		channel.CanRefreshObjects = true;
		channel.CanSpawnObjects = true;


		if ( PlayerPrefab.IsValid() )
		{
			var cachedPrefab = SceneUtility.GetPrefabScene( PlayerPrefab ).Clone();
			client.Respawn( channel, cachedPrefab, StartingWeapons );
		}
		else
		{
			client.Respawn( channel, weapons: StartingWeapons );
		}
	}

	protected override async Task OnLoad()
	{
		if ( StartServer && !Networking.IsActive && !Scene.IsEditor )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );

			var lobbyConfig = new LobbyConfig();

			Networking.CreateLobby( lobbyConfig );
			Log.Info( "Creating lobby" );
		}
	}

	protected override void OnStart()
	{
		base.OnStart();

		if ( !Networking.IsActive )
			return;
	}

	protected override void OnFixedUpdate()
	{
		foreach ( var client in Scene.Components.GetAll<Client>( FindMode.EnabledInSelfAndChildren ) )
		{
			if ( client.GetPawn<Pawn>().IsValid() )
				continue;

			client.Respawn( client.Network.Owner, weapons: StartingWeapons );
		}
	}
}
