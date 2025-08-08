namespace Seekers;

[Library( "tool_material", Title = "Material", Description = "Change render color and alpha of entities", Group = "construction" )]
public partial class MaterialTool : BaseTool
{
	[Property, MaterialPath]
	public string Material { get; set; } 

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() )
				return false;

			if ( !trace.GameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				return false;
			
			BroadcastMaterial( propHelper.GameObject, Material );

			return true;
		}

		return false;
	}

	public override bool Reload( SceneTraceResult trace )
	{

		if ( Input.Pressed( "reload" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() )
				return false;

			if ( !trace.GameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				return false;

			BroadcastMaterial( propHelper.GameObject, null );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	private void BroadcastMaterial( GameObject prop, string material )
	{
		// TODO: Fix this for other clients

		if ( prop.Components.TryGet<PropHelper>( out var propHelper ) )
			propHelper.Material = material;
	}
}
