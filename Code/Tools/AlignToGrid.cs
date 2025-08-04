namespace Seekers;

[Library( "tool_grid_alighn", Title = "Grid Alighn", Description = "Alighn Object To Grid", Group = "construction" )]
public partial class AlighnToGrid : BaseTool
{
	[Property]
	public float Grid { get; set; } = 8;

	public override bool Primary( SceneTraceResult trace )
	{
		if (!trace.Hit)
			return false;

		if ( !Input.Pressed( "attack1" ) )
			return false;

		var currentPos = trace.GameObject.WorldPosition;

		var snappedPos = new Vector3(
			MathF.Round( currentPos.x / Grid ) * Grid,
			MathF.Round( currentPos.y / Grid ) * Grid,
			MathF.Round( currentPos.z / Grid ) * Grid
		);

		trace.GameObject.WorldPosition = snappedPos;

		return true;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !Input.Pressed( "attack2" ) )
			return false;

		var go = trace.GameObject;
		if ( !go.IsValid() )
			return false;

		var hitPos = trace.HitPosition;

		var snappedPos = new Vector3(
			MathF.Round( hitPos.x / Grid ) * Grid,
			MathF.Round( hitPos.y / Grid ) * Grid,
			MathF.Round( hitPos.z / Grid ) * Grid
		);

		var delta = snappedPos - hitPos;

		go.WorldPosition += delta;

		return true;
	}

}
