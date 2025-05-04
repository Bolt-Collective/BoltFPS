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

		if(ReplacementModel.IsValid())
			ModelRenderer.Model = ReplacementModel;
	}
}
