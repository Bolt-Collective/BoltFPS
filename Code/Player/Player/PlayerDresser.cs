namespace Seekers;

public class PlayerDresser : Component, Component.INetworkSpawn
{
	[Property] private SkinnedModelRenderer ModelRenderer { get; set; }
	[Property] private Model ReplacementModel { get; set; }

	public void OnNetworkSpawn( Connection owner )
	{
		var clothing = owner.GetUserData( "avatar" );
		var container = new ClothingContainer();
		container.Deserialize( clothing );
		container.Height = 1;
		container.Apply( ModelRenderer );

		if ( ReplacementModel.IsValid() )
			ModelRenderer.Model = ReplacementModel;
	}

	public void DisableClothingGroups( List<Clothing.BodyGroups> bodyGroups,
		List<Clothing.ClothingCategory> clothingCategories )
	{
		if ( bodyGroups == null )
			bodyGroups = new();

		if ( clothingCategories == null )
			clothingCategories = new();

		var clothingData = Network.Owner.GetUserData( "avatar" );
		if ( clothingData == null )
			return;
		var container = new ClothingContainer();
		container.Deserialize( clothingData );

		container.Height = 1;

		if ( container.Clothing != null )
		{
			foreach ( var clothing in new List<ClothingContainer.ClothingEntry>( container.Clothing ) )
			{
				foreach ( var group in bodyGroups )
				{
					if ( group != clothing?.Clothing?.HideBody )
						continue;
					container.Clothing?.Remove( clothing );
				}

				foreach ( var clothingCategory in clothingCategories )
				{
					if ( clothingCategory != clothing?.Clothing?.Category )
						continue;
					container.Clothing?.Remove( clothing );
				}
			}
		}

		foreach ( var group in bodyGroups )
		{
			switch ( group )
			{
				case Clothing.BodyGroups.Head:
					ModelRenderer.SetBodyGroup( "Head", 2 );
					break;
				case Clothing.BodyGroups.Chest:
					ModelRenderer.SetBodyGroup( "Chest", 1 );
					break;
				case Clothing.BodyGroups.Legs:
					ModelRenderer.SetBodyGroup( "Legs", 1 );
					break;
				case Clothing.BodyGroups.Hands:
					ModelRenderer.SetBodyGroup( "Hands", 1 );
					break;
				case Clothing.BodyGroups.Feet:
					ModelRenderer.SetBodyGroup( "Feet", 1 );
					break;
			}
		}

		if ( ReplacementModel.IsValid() )
			ModelRenderer.Model = ReplacementModel;
	}
}
