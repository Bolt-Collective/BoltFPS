namespace Seekers;

[Library( "tool_remover", Description = "Remove entities")]
[Group( "construction" )]
public partial class Remover : BaseTool
{

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{

			if (trace.GameObject.Root.Components.TryGet<PropHelper>(out PropHelper helper))
				trace.GameObject.Root.BroadcastDestroy();

			return true;
		}

		return false;
	}
}
