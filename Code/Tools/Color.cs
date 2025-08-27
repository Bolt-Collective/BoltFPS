namespace Seekers;

[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities" )]
[Group("render")]
public partial class ColorTool : BaseTool
{
	public override bool UseGrid => false;

	[Property]
	public Color Color { get; set; } = Color.White;

	[Property, Range( 0, 1 )]
	public float Opacity { get; set; } = 1;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.Hit || !trace.GameObject.IsValid() )
				return false;

			if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
				return false;

			BroadcastColor( propHelper.GameObject, Color.WithAlpha(Opacity) );

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

			if ( !trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
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
