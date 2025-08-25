namespace Seekers;

[Library( "tool_push", Title = "Push Pull", Description = "Push and pull objects by set amount." )]
[Group( "construction" )]
public partial class PushPull : BaseTool
{
	[Property, Range( 0, 10 )]
	public float pushAmount { get; set; } = 1;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{

			Push( trace.GameObject, -trace.Normal.Normal * pushAmount );

			return true;
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack2" ) )
		{

			Push( trace.GameObject, trace.Normal.Normal * pushAmount );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	private void Push( GameObject gameObject, Vector3 push)
	{
		if ( gameObject.IsProxy )
			return;

		gameObject.WorldPosition += push;
	}
}
