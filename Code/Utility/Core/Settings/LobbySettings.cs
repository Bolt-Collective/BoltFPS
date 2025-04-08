using System.Text.Json;

namespace Seekers;

public sealed class LobbySettings
{
	public string Name { get; set; } = "PropHunt Lobby";
	public string Map { get; set; } = "gnomefig.office";
	public int MaxPlayers { get; set; } = 24;
	public int MinPlayers { get; set; } = 2;
	public int Rounds { get; set; } = 4;
	public bool PropAbilities { get; set; } = true;
	public bool HunterEquipment { get; set; } = true;

	public LobbyPrivacy Privacy { get; set; } = LobbyPrivacy.Public;

	public static void Save( LobbySettings settings )
	{
		var json = JsonSerializer.Serialize( settings );

		FileSystem.Data?.WriteAllText( "lobbysettings.json", json );
	}

	public static LobbySettings Load()
	{
		if ( !FileSystem.Data?.FileExists( "lobbysettings.json" ) ?? false )
		{
			return new LobbySettings();
		}

		var json = FileSystem.Data?.ReadAllText( "lobbysettings.json" );

		return JsonSerializer.Deserialize<LobbySettings>( json ?? string.Empty );
	}
}
