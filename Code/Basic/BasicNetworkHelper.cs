using Sandbox;

namespace Seekers;

public sealed class BasicNetworkHelper : Component, Component.INetworkListener
{
	[Property] public bool StartServer { get; set; } = true;
	[Property] public GameObject PlayerPrefab { get; set; }

	[Property] [Group( "Dev" )] private readonly List<long> PlayerWhitelist = new()
	{
		76561198043979097, // trende
		76561198193615491, // trollface
		76561199407136830, // paths
		76561198289339378 // graffiti
	};

	[Property] public List<long> KickedPlayers = new();

	[Property] [Group( "Dev" )] public bool DevMode { get; set; }

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

	void INetworkListener.OnActive( Connection channel )
	{
		var existingClient = Scene.Components.GetAll<Client>().FirstOrDefault( x => x.Connection == channel );
		if ( existingClient.IsValid() && !Application.IsDedicatedServer )
			return;

		var clientObj = Game.ActiveScene.CreateObject();
		clientObj.Name = $"Client - {channel.DisplayName}";
		clientObj.Tags.Add( "engine" );

		var client = clientObj.AddComponent<Client>();
		client.SteamId = channel.SteamId;
		clientObj.NetworkSpawn( channel );

		client.AssignConnection( channel );
		client.Team = TeamManager.Instance.DefaultTeam;

		channel.CanRefreshObjects = true;

		client.Respawn( channel );
	}

	protected override async Task OnLoad()
	{
		if ( StartServer && !Networking.IsActive && !Scene.IsEditor )
		{
			LoadingScreen.Title = "Creating Lobby";
			await Task.DelayRealtimeSeconds( 0.1f );

			var lobbyConfig = new LobbyConfig();
			var lobbySettings = new LobbySettings();

			if ( lobbySettings is not null )
			{
				lobbyConfig.Name = lobbySettings.Name;
				if ( !Game.IsEditor )
				{
					lobbyConfig.Privacy = lobbySettings.Privacy;
				}

				lobbyConfig.MaxPlayers = lobbySettings.MaxPlayers;
			}

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

			client.Respawn( client.Network.Owner );
		}
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
