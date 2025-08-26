namespace Seekers;

[Library( "tool_remover", Description = "Remove entities")]
[Group( "construction" )]
public partial class Remover : BaseTool
{
	public override bool UseGrid => false;
	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{

			if (trace.GameObject.Components.TryGet<PropHelper>(out PropHelper helper))
				trace.GameObject.BroadcastDestroy();

			return true;
		}

		return false;
	}
}
