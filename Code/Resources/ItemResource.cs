using static Seekers.BaseWeapon;

[GameResource( "Item", "wep", "An item represented in a resource", Icon = "🔫" )]
public class ItemResource : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[KeyProperty] public string Catagory { get; set; }
	[ImageAssetPath] public string Icon { get; set; }
	
	[KeyProperty, TextArea] public string Description { get; set; } = "An item.";
	
	public string Details { get; set; }

	public string GetDetails()
	{
		return Details == null ? "weapons/gun/w_gun.prefab".Replace( "gun", DisplayName ) : Details;
	}
}
