

[GameResource("Weapon", "wep", "A weapon represented in a resource", Icon = "🔫")]
public class WeaponResource : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[ImageAssetPath] public string Icon { get; set; }
	public string Details { get; set; }

}
