using Sandbox.Utility;

namespace Seekers;

public class Resizer : BaseTool
{
	
	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Down( "attack1" ) )
		{
			Resize(trace.GameObject, 0.033f);
			return true;
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( Input.Down( "attack2" ) )
		{
			Resize(trace.GameObject, -0.033f);
			return true;
		}

		return false;
	}
	
	[Rpc.Broadcast]
	private void Resize( GameObject go, float size )
	{
		if ( !go.IsValid() ) return;
		if ( go.IsProxy ) return;

		var newScale = go.WorldScale + size;
		if ( newScale.Length < 0.1f ) return;
		if ( newScale.Length > 1000f ) return;

		var scale = Vector3.Max( newScale, 0.01f );
		go.WorldScale = scale;
	}
}
