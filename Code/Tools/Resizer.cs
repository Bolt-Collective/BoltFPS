using Sandbox.Utility;

namespace Seekers;

[Library( "tool_resizer", Title = "Resizer", Description = "Increases or decreases the size of an object" )]
[Group( "construction" )]
public class Resizer : BaseTool
{
	public override IEnumerable<ToolHint> GetHints()
	{
		yield return new ToolHint( "attack1", "Inflate" );
		yield return new ToolHint( "attack2", "Deflate" );
	}

	[Property, Range( 0.01f, 0.1f )] public float Rate { get; set; } = 0.033f;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Down( "attack1" ) )
			return false;

		var target = trace.GameObject;

		Resize( target, target.WorldScale, Rate );

		return true;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !Input.Down( "attack2" ) )
			return false;

		var target = trace.GameObject;

		Resize( target, target.WorldScale, -Rate );

		return true;
	}

	[Rpc.Broadcast]
	private void Resize( GameObject go, Vector3 currentScale, float size )
	{
		if ( !go.IsValid() ) return;

		if ( !go.Root.Components.TryGet( out PropHelper _ ) )
			return;

		var newScale = currentScale + size;
		if ( newScale.Length < 0.1f ) return;
		if ( newScale.Length > 1000f ) return;

		var scale = Vector3.Max( newScale, 0.01f );
		go.WorldScale = scale;
	}
}
