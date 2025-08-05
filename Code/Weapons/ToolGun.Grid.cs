using System.Text.Json.Nodes;

namespace Seekers;
public partial class ToolGun : BaseWeapon
{
	public List<Vector3> CreateGrid( BBox localBounds, GameObject gameObject, SceneTraceResult trace )
	{
		Gizmo.Draw.IgnoreDepth = true;
		BBox bounds = localBounds;
		var transform = new Transform( Vector3.Zero, gameObject.WorldRotation, gameObject.WorldScale );
		Vector3 OffsetToWorld( Vector3 local ) => transform.PointToWorld( local ) + gameObject.WorldPosition;

		var corners = bounds.Corners.Select( OffsetToWorld ).ToArray();

		var faces = new Dictionary<string, int[]>
		{
			{ "Top",    new[] { 7, 6, 5, 4 } }, // +Z
			{ "Bottom", new[] { 3, 2, 1, 0 } }, // -Z
			{ "Front",  new[] { 3, 2, 6, 7} }, // +Y
			{ "Back",   new[] { 4, 5, 1, 0} }, // -Y
			{ "Left",   new[] { 3, 7, 4, 0 } }, // -X
			{  "Right",  new[] { 6, 2, 1, 5 } } // +X
		};

		var traceDir = trace.Normal;

		string bestFace = null;
		Vector3[] bestQuad = null;
		float closestHitDistance = float.MaxValue;

		foreach ( var (name, indices) in faces )
		{
			var p0 = corners[indices[0]];
			var p1 = corners[indices[1]];
			var p2 = corners[indices[2]];
			var p3 = corners[indices[3]];

			DrawCorners( indices.Select( i => corners[i] ).ToArray() );

			var normal = Vector3.Cross( p1 - p0, p3 - p0 ).Normal;

			var denom = Vector3.Dot( normal, trace.Direction );
			if ( MathF.Abs( denom ) < 0.0001f )
				continue;

			float d = Vector3.Dot( normal, p0 );
			float t = (d - Vector3.Dot( normal, trace.StartPosition )) / denom;
			if ( t < 0 ) continue;

			var hitPos = trace.StartPosition + trace.Direction * t;

			if ( PointInTriangle( hitPos, p0, p1, p3 ) || PointInTriangle( hitPos, p1, p2, p3 ) )
			{
				if ( t < closestHitDistance )
				{
					bestFace = name;
					bestQuad = new[] { p0, p1, p2, p3 };
					closestHitDistance = t;
				}
			}
		}

		if ( bestFace != null && faces.TryGetValue( bestFace, out var faceIndices ) )
		{
			var original = faceIndices.Select( i => corners[i] ).ToArray();

			return DrawFace( original );
		}

		return new();
	}

	bool PointInTriangle( Vector3 p, Vector3 a, Vector3 b, Vector3 c )
	{
		var v0 = c - a;
		var v1 = b - a;
		var v2 = p - a;

		float dot00 = Vector3.Dot( v0, v0 );
		float dot01 = Vector3.Dot( v0, v1 );
		float dot02 = Vector3.Dot( v0, v2 );
		float dot11 = Vector3.Dot( v1, v1 );
		float dot12 = Vector3.Dot( v1, v2 );

		float denom = dot00 * dot11 - dot01 * dot01;
		if ( denom == 0 ) return false;

		float u = (dot11 * dot02 - dot01 * dot12) / denom;
		float v = (dot00 * dot12 - dot01 * dot02) / denom;

		return u >= 0 && v >= 0 && (u + v) <= 1;
	}

	public int subDiv = 4;

	List<Vector3> DrawFace( Vector3[] face )
	{
		Gizmo.Draw.Color = Color.White;

		int lines = subDiv + 2;
		var grid = new Vector3[lines, lines];

		// Original face corners
		var topLeft = face[0];
		var topRight = face[1];
		var bottomRight = face[2];
		var bottomLeft = face[3];

		// Step 1: Generate regular grid based on the original face
		for ( int y = 0; y < lines; y++ )
		{
			float fy = y / (float)(lines - 1);
			Vector3 left = Vector3.Lerp( topLeft, bottomLeft, fy );
			Vector3 right = Vector3.Lerp( topRight, bottomRight, fy );

			for ( int x = 0; x < lines; x++ )
			{
				float fx = x / (float)(lines - 1);
				grid[x, y] = Vector3.Lerp( left, right, fx );
			}
		}

		// Step 2: Shrink outermost edge points inward
		float shrinkAmount = 1f / subDiv;

		// Top and bottom rows
		for ( int x = 0; x < lines; x++ )
		{
			Vector3 dir = (grid[x, 1] - grid[x, 0]).Normal; // inward from top
			grid[x, 0] += dir * shrinkAmount;

			dir = (grid[x, lines - 2] - grid[x, lines - 1]).Normal; // inward from bottom
			grid[x, lines - 1] += dir * shrinkAmount;
		}

		// Left and right columns
		for ( int y = 0; y < lines; y++ )
		{
			Vector3 dir = (grid[1, y] - grid[0, y]).Normal; // inward from left
			grid[0, y] += dir * shrinkAmount;

			dir = (grid[lines - 2, y] - grid[lines - 1, y]).Normal; // inward from right
			grid[lines - 1, y] += dir * shrinkAmount;
		}

		// Step 3: Draw straight grid lines
		for ( int y = 0; y < lines; y++ )
		{
			for ( int x = 0; x < lines; x++ )
			{
				if ( x < lines - 1 )
					Gizmo.Draw.Line( grid[x, y], grid[x + 1, y] );
				if ( y < lines - 1 )
					Gizmo.Draw.Line( grid[x, y], grid[x, y + 1] );
			}
		}

		var result = new List<Vector3>();
		foreach ( var point in grid )
			result.Add( point );

		return result;
	}




	public void DrawCorners( Vector3[] face )
	{
		void EdgeLine( int index1, int index2 )
		{
			Gizmo.Draw.Line( face[index1], face[index1].LerpTo( face[index2], 0.1f ) );
			Gizmo.Draw.Line( face[index2], face[index2].LerpTo( face[index1], 0.1f ) );
		}

		Gizmo.Draw.Color = Color.Blue;

		EdgeLine( 0, 1 );
		EdgeLine( 3, 2 );
		EdgeLine( 1, 2 );
		EdgeLine( 0, 3 );
	}


}
