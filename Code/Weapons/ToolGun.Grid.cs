using System.Text.Json.Nodes;

namespace Seekers;
public partial class ToolGun : BaseWeapon
{
	public List<Vector3> CreateGrid( BBox localBounds, GameObject gameObject, SceneTraceResult trace )
	{
		BBox bounds = localBounds;
		var transform = new Transform( Vector3.Zero, gameObject.WorldRotation );
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

			var center = (original[0] + original[1] + original[2] + original[3]) / 4f;

			var shrunk = original.Select( p =>
			{
				var dir = (center - p).Normal;
				return p + dir;
			} ).ToArray();

			return DrawFace( shrunk );
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
		var top = (face[0], face[1]);
		var bottom = (face[3], face[2]);

		var right = (face[1], face[2]);
		var left = (face[0], face[3]);

		var lines = subDiv + 2;

		var intersections = new List<Vector3>();

		for ( int i = 0; i < lines; i++ )
		{
			var frac = (1f / (lines - 1)) * i;

			var point1 = top.Item1.LerpTo( top.Item2, frac );
			var point2 = bottom.Item1.LerpTo( bottom.Item2, frac );

			Gizmo.Draw.Line( point1, point2 );

			var point3 = left.Item1.LerpTo( left.Item2, frac );
			var point4 = right.Item1.LerpTo( right.Item2, frac );

			Gizmo.Draw.Line( point3, point4 );

			for ( int j = 0; j < lines; j++ )
			{
				var subFrac = (1f / (lines - 1)) * j;

				var p1 = point1.LerpTo( point2, subFrac );
				var p2 = point3.LerpTo( point4, subFrac );

				intersections.Add( p1 );
			}
		}

		return intersections;
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
