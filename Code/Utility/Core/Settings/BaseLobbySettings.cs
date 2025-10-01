using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Sandbox.Utility;

namespace BoltFPS;

/// <summary>
/// Represents the base settings for a game lobby, including basic configurations such as name, map, player limits,
/// and privacy settings.
/// </summary>
public class BaseLobbySettings
{
	public virtual string Name { get; set; } = $"{Steam.PersonaName}'s Lobby";
	public virtual string Map { get; set; } = "gnomefig.office";
	public virtual int MaxPlayers { get; set; } = 24;
	public virtual int MinPlayers { get; set; } = 2;
	
	public LobbyPrivacy Privacy { get; set; } = LobbyPrivacy.Public;

	/// <summary>
	/// Saves the current lobby settings to a JSON file.
	/// </summary>
	/// <param name="settings">The <see cref="BaseLobbySettings"/> instance containing the lobby configuration to save.</param>
	public static void Save( BaseLobbySettings settings )
	{
		var json = JsonSerializer.Serialize( settings );

		FileSystem.Data?.WriteAllText( "lobbysettings.json", json );
	}

	/// <summary>
	/// Loads the lobby settings from a JSON file if it exists; otherwise, creates a new default instance of <see cref="BaseLobbySettings"/>.
	/// </summary>
	/// <returns>
	/// An instance of <see cref="BaseLobbySettings"/> representing the loaded or default lobby settings.
	/// </returns>
	public static T Load<T>() where T : BaseLobbySettings, new()
	{
		if ( !FileSystem.Data?.FileExists( "lobbysettings.json" ) ?? false )
			return new T();

		var json = FileSystem.Data?.ReadAllText( "lobbysettings.json" );
		return JsonSerializer.Deserialize<T>( json ?? string.Empty ) ?? new T();
	}

	/// <summary>
	/// Converts the current <see cref="BaseLobbySettings"/> instance to a <see cref="LobbyConfig"/> object.
	/// </summary>
	/// <returns>
	/// A <see cref="LobbyConfig"/> object containing the lobby settings such as Name, MaxPlayers, and Privacy.
	/// </returns>
	public LobbyConfig ToLobbyConfig()
	{
		return new LobbyConfig { Name = Name, MaxPlayers = MaxPlayers, Privacy = Privacy };
	}
}
