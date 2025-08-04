using Seekers;
using static Seekers.BaseWeapon;

[GameResource( "Item", "wep", "An item represented in a resource", Icon = "🔫" )]
public class ItemResource : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[KeyProperty] public string Catagory { get; set; }
	[KeyProperty] public bool Duplicates { get; set; } = true;
	[KeyProperty, ShowIf("Duplicates",false)] public bool ReplenishAmmo { get; set; }
	[ImageAssetPath] public string Icon { get; set; }
	
	[KeyProperty, TextArea] public string Description { get; set; } = "An item.";
	
	public string Details { get; set; }

	public string GetDetails()
	{
		return Details == null ? "weapons/gun/w_gun.prefab".Replace( "gun", DisplayName ) : Details;
	}

	public void GiveWeapon()
	{
		var pawn = Pawn.Local;
		if ( !pawn?.Inventory.IsValid() ?? true )
			return;

		foreach (var weapon in pawn.Inventory.Weapons)
		{
			if ( Duplicates )
				break;

			if ( weapon.ItemResource != this )
				continue;

			pawn.Inventory.SetActiveSlot( pawn.Inventory.Weapons.IndexOf( weapon ) );

			if ( !ReplenishAmmo )
				return;

			var prefab = GameObject.GetPrefab( GetDetails() );

			if ( prefab.Components.TryGet<BaseWeapon>(out var baseWeapon) )
				weapon.Ammo = baseWeapon.Ammo;

			return;
		}

		Pawn.Local.Inventory.Pickup( GetDetails() );
	}
}
