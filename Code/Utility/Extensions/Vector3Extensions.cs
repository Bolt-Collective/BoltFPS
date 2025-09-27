namespace Seekers;

public static partial class Vector3Extensions
{
	public static Vector3 ClosestPointOnLine( this Vector3 point, Vector3 lineStart, Vector3 lineEnd, bool clampToSegment = true )
	{
		var lineDir = lineEnd - lineStart;
		var lineLengthSq = lineDir.LengthSquared;

		if ( lineLengthSq == 0 )
			return lineStart;

		var t = Vector3.Dot( point - lineStart, lineDir ) / lineLengthSq;

		if ( clampToSegment )
			t = t.Clamp( 0f, 1f );

		return lineStart + t * lineDir;
	}
}
