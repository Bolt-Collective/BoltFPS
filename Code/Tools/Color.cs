namespace Seekers;

[Library( "tool_color", Title = "Color", Description = "Change render color and alpha of entities", Group = "construction" )]
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

			BroadcastColor( propHelper, Color );

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

			BroadcastColor( propHelper, Color.White );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	private void BroadcastColor( PropHelper propHelper, Color color )
	{
		// TODO: Fix this for other clients

		propHelper.Prop.Tint = color;
	}
}
