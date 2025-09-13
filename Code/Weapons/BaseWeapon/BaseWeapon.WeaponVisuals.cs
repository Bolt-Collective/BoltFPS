using Sandbox.Events;

namespace Seekers;

public partial class BaseWeapon : Component
{
	[Feature( "Weapon Visuals" ), Property] public List<GameObject> DisableInFP { get; set; } = new();
	[Feature( "Weapon Visuals" ), Property] public bool MergeModel { get; set; }
	[Feature( "Weapon Visuals" ), ShowIf("MergeModel", true), Property] public Model FemaleReplacement { get; set; }
	[Feature( "Weapon Visuals" ), ShowIf( "MergeModel", true ), Property] public List<Clothing.BodyGroups> RemoveGroups { get; set; }
	[Feature( "Weapon Visuals" ), ShowIf( "MergeModel", true ), Property] public List<Clothing.ClothingCategory> RemoveClothingCategories { get; set; }
}
