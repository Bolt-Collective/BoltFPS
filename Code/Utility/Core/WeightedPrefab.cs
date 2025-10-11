public class WeightedPrefab
{
	[KeyProperty] public PrefabFile Prefab { get; set; }
	[KeyProperty] public float Weight { get; set; } = 1f;
}
