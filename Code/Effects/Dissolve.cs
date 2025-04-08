using Sandbox;

namespace Seekers;

public sealed class Dissolve : Component, Component.ExecuteInEditor
{
	public Material DissolveMaterial => Material.Load( "materials/dissolve.vmat" );
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public float DissolveAmount { get; set; } = -5f;

	protected override void OnUpdate()
	{
		if ( !Renderer.IsValid() )
			return;

		Renderer.SceneObject.Batchable = false;
		Renderer.SetMaterialOverride( DissolveMaterial, "" );


		DissolveAmount += Time.Delta * 2f; // Adjust speed by changing this multiplier
		if ( DissolveAmount >= 5f )
			DissolveAmount = -5f;

		Renderer.SceneObject.Attributes.Set( "DissolveAmount", DissolveAmount );


		base.OnUpdate();
	}
}
