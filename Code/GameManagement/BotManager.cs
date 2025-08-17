namespace Seekers;

public class BotManager : GameObjectSystem
{
	public static BotManager Instance { get; private set; }
	
	private static readonly string[] BotNames =
	{
		"Gordon",
		"Jamie",
		"Nigella",
		"Heston",
		"Anthony",
		"Ainsley",
		"Delia",
		"Loyd",
		"Paul",
		"Marco"
	};
	public string[] Names;

	[Sync( SyncFlags.FromHost )] private int CurrentBotId { get; set; } = 0;
	
	public BotManager(Scene scene) : base(scene)
	{
		Listen( Stage.SceneLoaded, 1, OnSceneLoad, "ShuffleNames" );
	}

	private void OnSceneLoad()
	{
		Names = BotNames.Shuffle().ToArray();
	}

	public Client AddBot()
	{
		var player = Game.ActiveScene.CreateObject();
		player.Name = $"Client (BOT)";
		player.Tags.Add( "engine" );
		
		// AI
		var botController = player.AddComponent<BotController>();

		var client = player.AddComponent<Client>();
		client.BotId = CurrentBotId;

		// Simpsons
		var steamid = Game.Random.FromArray( [
				76561198076731362,
				76561198115447501,
				76561198081295106,
				76561198165412225,
				76561198023414915,
				76561198176366622,
				76561198092430664,
				76561198066084037,
				76561198368894435,
				76561198389241377,
				76561198158965172,
				76561198306626714,
				76561198208716648,
				76561198835780877,
				76561197970331648,
				76561198051740093,
				76561198111069943,
				76561198075423731,
				76561197965588718,
				76561197960316241,
				76561198361294115,
				76561197960555384,
				76561198021354850,
				76561198207495888,
				76561198040673812,
				76561198241363850,
				76561198151921867,
				76561198095212046,
				76561198169445087
		] );

		client.SteamId = (ulong)steamid;

		GameNetworkManager.Instance.OnPlayerJoined( client, Connection.Host );

		CurrentBotId++;

		return client;
	}

	public string GetName( int id )
	{
		return Names[id % Names.Length];
	}

	[ConCmd( "bot_add", ConVarFlags.Cheat )]
	private static void Command_Add_Bot()
	{
		Instance.AddBot();
	}

	[ConCmd( "bot_kick_all", ConVarFlags.Cheat )]
	private static void Command_Kick_Bots()
	{
		foreach ( var player in Game.ActiveScene.GetAll<Client>() )
		{
			if ( player.IsBot )
			{
				player.Kick();
			}
		}
	}
}
