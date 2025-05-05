

using static Seekers.BaseWeapon;

[GameResource("Weapon", "wep", "A weapon represented in a resource", Icon = "🔫")]
public class WeaponResource : GameResource
{
	[KeyProperty] public string DisplayName { get; set; }
	[ImageAssetPath] public string Icon { get; set; }
	public string Details { get; set; }

	public string GetDetails()
	{
		return Details == null ? "weapons/gun/w_gun.prefab".Replace( "gun", DisplayName ) : Details;
	}
	
}
