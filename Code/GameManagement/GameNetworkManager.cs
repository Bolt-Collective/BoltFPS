using Sandbox;
using Sandbox.Events;

namespace Seekers;

public class GameNetworkManager : SingletonComponent<GameNetworkManager>, Component.INetworkListener
{
	[Property] public bool StartServer { get; set; } = true;
	[Property] public GameObject PlayerPrefab { get; set; }
	
	[Property] public GameObject ClientPrefab { get; set; }

	[Property] [Group( "Dev" )] private readonly List<long> PlayerWhitelist = new()
	{
		76561198043979097, // trende
		76561198193615491, // trollface
		76561199407136830, // paths
		76561198289339378 // graffiti
	};

	[Property] public List<long> KickedPlayers = new();

	[Property] [Group( "Dev" )] public bool DevMode { get; set; }

	[Property] public List<ItemResource> StartingWeapons { get; set; }

	bool INetworkListener.AcceptConnection( Connection channel, ref string reason )
	{
		if ( DevMode && !PlayerWhitelist.Contains( channel.SteamId.Value ) )
		{
			reason = "This is a developer lobby, you can't join!";
			return false;
		}

		if ( KickedPlayers.Contains( channel.SteamId.Value ) )
		{
			reason = "You have been kicked from this game. ta ta";
			return false;
		}

		return true;
	}
	
	private Client GetOrCreateClient( Connection channel = null )
	{
		var Clients = Scene.GetAllComponents<Client>();

		var possibleClient = Clients.FirstOrDefault( x =>
		{
			// A candidate player state has no owner.
			return x.Connection is null && x.SteamId == channel.SteamId;
		} );

		if ( possibleClient.IsValid() )
		{
			Log.Warning( $"Found existing player state for {channel.SteamId} that we can re-use. {possibleClient}" );
			return possibleClient;
		}

		if ( !ClientPrefab.IsValid() )
		{
			Log.Warning( "Could not spawn player as no ClientPrefab assigned." );
			return null;
		}

		var clientObj = ClientPrefab.Clone();
		clientObj.BreakFromPrefab();
		clientObj.Name = $"Client - {channel.DisplayName}";
		clientObj.Network.SetOrphanedMode( NetworkOrphaned.ClearOwner );

		var client = clientObj.GetComponent<Client>();
		client.SteamId = channel.SteamId;
		
		if ( !client.IsValid() )
			return null;

		return client;
	}

	void INetworkListener.OnActive( Connection channel )
	{
		var existingClient = Scene.Components.GetAll<Client>().FirstOrDefault( x => x.Connection == channel );
		if ( existingClient.IsValid() && !Application.IsDedicatedServer )
			return;
		
		Log.Info( $"Player '{channel.DisplayName}' is becoming active" );

		var cl = GetOrCreateClient( channel );
		if ( !cl.IsValid() )
		{
			throw new Exception( $"Something went wrong when trying to create Client for {channel.DisplayName}" );
		}

		OnPlayerJoined( existingClient, channel );
	}

	public virtual void OnPlayerJoined(Client client, Connection channel)
	{
		if (!client.Network.Active)
			client.GameObject.NetworkSpawn( channel );
		
		client.AssignConnection( channel );
		client.Team = TeamManager.Instance.DefaultTeam;

		channel.CanRefreshObjects = true;

		if ( PlayerPrefab.IsValid() )
		{
			client.Respawn( channel, PlayerPrefab, StartingWeapons );
		}
		else
		{
			client.Respawn( channel, weapons: StartingWeapons );
		}
		
		Scene.Dispatch( new PlayerJoinedEvent( client ) );
	}
	
	
	void INetworkListener.OnDisconnected( Connection channel )
	{
		Log.Info( $"Player '{channel.DisplayName}' is disconnecting" );
		var cl = Scene.GetAllComponents<Client>().FirstOrDefault( x => x.Connection == channel );

		if ( !cl.IsValid() )
		{
			Log.Warning( $"No Client found for {channel.DisplayName}" );
			return;
		}

		Scene.Dispatch( new PlayerDisconnectedEvent( cl ) );
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
	

	public Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			var transform = Random.Shared.FromArray( spawnPoints ).Transform.World;
			return transform.WithPosition( transform.Position + Vector3.Up * 5 );
		}

		//
		// Failing that, spawn where we are
		//
		return WorldTransform;
	}
}
