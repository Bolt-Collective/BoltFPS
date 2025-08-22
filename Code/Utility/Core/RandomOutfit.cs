using Sandbox;

public sealed class RandomOutfit : Component
{
	[RequireComponent, Property]
	public Dresser Dresser { get; set; }

	[Property, InlineEditor]
	public List<Outfit> Outfits {get;set;} = new();

	public struct Outfit
	{
		public string Name { get; set; }
		public List<ClothingContainer.ClothingEntry> Entrys { get; set; }
	}

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		List<ClothingContainer.ClothingEntry> entrys = new();
		var outfit = Game.Random.FromList(Outfits);

		foreach (var entry in outfit.Entrys)
			entrys.Add(entry);

		Apply( entrys );
	}

	[Rpc.Broadcast]
	public void Apply( List<ClothingContainer.ClothingEntry> entrys )
	{
		Dresser.Clothing = entrys;
		Dresser.Apply();
	}
}
