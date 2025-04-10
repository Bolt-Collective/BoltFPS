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

	public void SetLoadout( Guid player, List<string> loadout )
	{
		var pawn = Pawn.Local;

		var paths = new List<string>();

		foreach ( var weapon in loadout )
		{
			if(weapon.Contains("/"))
				paths.Add( weapon );
			else
				paths.Add( "weapons/gun/w_gun.prefab".Replace( "gun", weapon ) );
		}

		if ( PlayerLoadouts.ContainsKey( player ) )
			PlayerLoadouts[player] = paths;
		else
			PlayerLoadouts.Add( player, paths );
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
