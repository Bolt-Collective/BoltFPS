using Seekers;
using static Seekers.BaseWeapon;

[AssetType( Name = "Item", Extension = "wep" )]
public class ItemResource : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[KeyProperty] public string Catagory { get; set; }
	[KeyProperty] public bool Duplicates { get; set; } = true;
	[KeyProperty] public int Reserve { get; set; } = 256;

	[KeyProperty, ShowIf( "Duplicates", false )]
	public bool ReplenishAmmo { get; set; }

	[ImageAssetPath] public string Icon { get; set; }

	[KeyProperty, TextArea] public string Description { get; set; } = "An item.";

	[ResourceType( "prefab" )] public string Details { get; set; }

	public string GetDetails()
	{
		return Details == null ? "weapons/gun/w_gun.prefab".Replace( "gun", DisplayName ) : Details;
	}

	[Rpc.Broadcast]
	public static void GiveWeapon(string connection, string itemResourcePath, int reserve = -1)
	{
		Log.Info( connection );
		if ( Connection.Local.Id.ToString() != connection )
			return;

		if ( !ResourceLibrary.TryGet<ItemResource>( itemResourcePath, out var itemResource ) )
			return;

		itemResource.GiveWeapon( reserve );
	}

	public void GiveWeapon( int reserve = -1 )
	{
		Log.Info( "swah" );
		if ( reserve == -1 )
			reserve = Reserve;

		var pawn = Pawn.Local;
		if ( !pawn?.Inventory.IsValid() ?? true )
			return;

		var prefab = GameObject.GetPrefab( GetDetails() );

		if ( !prefab.IsValid() || !prefab.Components.TryGet<BaseWeapon>( out var baseWeapon ) )
			return;

		foreach ( var weapon in pawn.Inventory.Weapons )
		{
			if ( Duplicates )
				break;

			if ( weapon.ItemResource != this )
				continue;

			pawn.Inventory.SetActiveSlot( pawn.Inventory.Weapons.IndexOf( weapon ) );

			if ( !ReplenishAmmo )
				return;

			weapon.Ammo = baseWeapon.Ammo;
			pawn.Inventory.GiveReserve( weapon.AmmoType, reserve );

			return;
		}

		pawn.Inventory.GiveReserve( baseWeapon.AmmoType, reserve );
		Pawn.Local.Inventory.Pickup( GetDetails() );
	}

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		if ( string.IsNullOrEmpty( Icon ) )
		{
			return CreateSimpleAssetTypeIcon( "🔫", 128, 128, Color.Transparent, Color.White );
		}
		else
		{
			var img = Bitmap.CreateFromBytes( FileSystem.Mounted.ReadAllBytes( Icon ).ToArray() );
			return img;
		}
	}
}
