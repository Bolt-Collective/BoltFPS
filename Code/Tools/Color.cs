namespace Seekers;

[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities" )]
[Group("render")]
public partial class ColorTool : BaseTool
{
	[Property]
	public Color Color { get; set; } = Color.White;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() )
				return false;

			if ( !trace.GameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				return false;

			BroadcastColor( propHelper.GameObject, Color );

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

			BroadcastColor( propHelper.GameObject, Color.White );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	private void BroadcastColor( GameObject prop, Color color )
	{
		// TODO: Fix this for other clients
		if ( prop.Components.TryGet<PropHelper>( out var propHelper ) && propHelper.Prop.IsValid())
			propHelper.Prop.Tint = color;

		if ( prop.Components.TryGet<ModelRenderer>( out var modelRenderer ) )
			modelRenderer.Tint = color;
	}
}
