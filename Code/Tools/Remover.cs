namespace Seekers;

[Library( "tool_remover", Description = "Removes objects" )]
[Group( "construction" )]
public partial class Remover : BaseTool
{
	public override bool UseGrid => false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if ( !trace.GameObject.IsValid() )
				return false;

			BroadcastDestroy( trace.GameObject );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	public void BroadcastDestroy( GameObject gameObject )
	{
		if ( !gameObject.Root.Components.TryGet( out PropHelper propHelper ) )
			return;

		propHelper.GameObject?.Destroy();
	}
}
