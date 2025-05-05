using System.Text.Json.Serialization;
using Sandbox.UI;
using ShrimplePawns;

namespace Seekers;

public partial class EnsuredLoadout : SingletonComponent<EnsuredLoadout>
{
	[Sync] public NetDictionary<Guid, List<string>> PlayerLoadouts { get; set; } = new();

	protected override void OnFixedUpdate()
	{
		var pawn = Pawn.Local;

		if ( !pawn.IsValid() )
			return;

		if ( !pawn.Inventory.IsValid() )
			return;

		if ( !PlayerLoadouts.ContainsKey( Connection.Local.Id ) )
			return;

		var loadout = new List<string>();

		foreach ( var name in PlayerLoadouts?[Connection.Local.Id] )
		{
			loadout?.Add( GameObject.GetPrefab( name )?.GetComponent<BaseWeapon>()?.Name );
		}

		if ( loadout.Count != pawn.Inventory?.Weapons?.Count() )
		{
			ResetLoadout();
			return;
		}

		foreach ( var weapon in loadout )
		{
			bool hasWeapon = false;
			foreach ( var baseWeapon in pawn.Inventory?.Weapons )
			{
				if ( baseWeapon?.Name != weapon )
					continue;

				hasWeapon = true;
				break;
			}

			if ( hasWeapon )
				continue;

			ResetLoadout();

			return;
		}
	}

	[Rpc.Host]
	public void SetLoadout( Guid player, List<string> loadout )
	{
		if ( PlayerLoadouts.ContainsKey( player ) )
			PlayerLoadouts[player] = loadout;
		else
			PlayerLoadouts.Add( player, loadout );
	}
	
	[Rpc.Host]
	public void AddToLoadout(Guid player, string weapon)
	{
		
		if ( PlayerLoadouts.ContainsKey( player ) )
		{
			SetLoadout( player, new List<string>( PlayerLoadouts[player] ) { weapon } );
		}
		else
			SetLoadout( player, new List<string> { weapon } );
		
	}

	void ResetLoadout()
	{
		var pawn = Pawn.Local;

		var weapons = new List<BaseWeapon>( pawn.Inventory.Weapons );

		foreach ( var weapon in weapons )
		{
			pawn.Inventory?.RemoveWeapon( weapon );
		}

		bool equip = true;
		foreach ( var weapon in PlayerLoadouts[Connection.Local.Id] )
		{
			pawn.Inventory.Pickup( weapon, equip );
			equip = false;
		}
	}
}
