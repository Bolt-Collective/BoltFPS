using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography.X509Certificates;
using BoltFPS;
using Sandbox;


namespace Seekers;

public class CoverGenerator : GameObjectSystem
{

	[ConVar]
	public static bool ai_generatecover { get; set; } = true;

	public class CoverPoint
	{
		public Vector3 Position;
		public Vector3 Direction;
		public Knowable Owner;

		public void Own(Knowable owner)
		{
			Owner = owner;
		}

		public bool IsOwned
		{
			get
			{
				if ( Owner.IsValid() )
					return true;

				Owner = null;

				return false;
			}
		}

		public CoverPoint(Vector3 pos, Vector3 dir)
		{
			Position = pos; Direction = dir;
		}

		private bool infoCreated = false;

		private float _height = 5;
		public float Height
		{
			get
			{
				if ( !infoCreated )
					GetInfo();
				return _height;
			}
		}

		private float _consistency = 0;
		public float Consistency
		{
			get
			{
				if (!infoCreated)
					GetInfo();
				return _consistency;
			}
		}

		public void GetInfo()
		{
			infoCreated = true;
			int height = 5;
			var trueHeights = new List<bool>();
			for (int i = 0; i < 64; i++)
			{
				var wallcheck = WallCheck( Position, Direction, i, 1 );
				trueHeights.Add( wallcheck.Hit );
				if ( wallcheck.Hit )
					height = i;
			}


			if ( trueHeights.Count > 0)
			{
				var validHeights = trueHeights.GetRange( 0, height - 1 );

				int trueCount = 0;
				foreach ( var value in validHeights )
				{
					if ( value ) trueCount++;
				}

				_consistency = (float)trueCount / validHeights.Count;

			}

			_height = height;
		}
	}

	private static Dictionary<(int x, int y, int z), List<CoverPoint>> coverChunks = new();
	const float chunkSize = 512;

	public static List<CoverPoint> AllCovers()
	{
		var list = new List<CoverPoint>();
		foreach( var chunk in coverChunks )
			list.AddRange( chunk.Value );

		return list;
	}

	private static (int x, int y, int z) GetChunkCoords( Vector3 pos )
	{
		return (
			(int)MathF.Floor( pos.x / chunkSize ),
			(int)MathF.Floor( pos.y / chunkSize ),
			(int)MathF.Floor( pos.z / chunkSize )
		);
	}

	public static void AddCover( CoverPoint coverPoint )
	{
		var key = GetChunkCoords( coverPoint.Position );
		if ( !coverChunks.TryGetValue( key, out var list ) )
		{
			list = new List<CoverPoint>();
			coverChunks[key] = list;
		}
		list.Add( coverPoint );
	}

	public static bool RemoveCover( CoverPoint coverPoint )
	{
		var key = GetChunkCoords( coverPoint.Position );
		if ( coverChunks.TryGetValue( key, out var list ) )
		{
			return list.Remove( coverPoint );
		}
		return false;
	}

	public static List<CoverPoint>? GetNearestChunk( Vector3 pos)
	{ 
		var coords = GetChunkCoords( pos );

		if ( coverChunks.TryGetValue( coords, out var list ) )
			return list;

		return null;
	}
	static bool SphereIntersectsChunk( Vector3 p, float r, (int x, int y, int z) c )
	{
		var min = new Vector3( c.x * chunkSize, c.y * chunkSize, c.z * chunkSize );
		var max = min + new Vector3( chunkSize, chunkSize, chunkSize );

		float d2 = 0f;

		if ( p.x < min.x ) { float d = min.x - p.x; d2 += d * d; }
		else if ( p.x > max.x ) { float d = p.x - max.x; d2 += d * d; }

		if ( p.y < min.y ) { float d = min.y - p.y; d2 += d * d; }
		else if ( p.y > max.y ) { float d = p.y - max.y; d2 += d * d; }

		if ( p.z < min.z ) { float d = min.z - p.z; d2 += d * d; }
		else if ( p.z > max.z ) { float d = p.z - max.z; d2 += d * d; }

		return d2 <= r * r;
	}

	public static List<CoverPoint>? GetChunksInRadius( Vector3 pos, float radius )
	{
		var results = new List<CoverPoint>();
		float r2 = radius * radius;

		var center = GetChunkCoords( pos );
		int rc = (int)MathF.Ceiling( radius / chunkSize );

		for ( int x = center.x - rc; x <= center.x + rc; x++ )
		for ( int y = center.y - rc; y <= center.y + rc; y++ )
		for ( int z = center.z - rc; z <= center.z + rc; z++ )
		{
			var key = (x, y, z);
			if ( !coverChunks.TryGetValue( key, out var list ) )
				continue;

			if ( !SphereIntersectsChunk( pos, radius, key ) )
				continue;

			//if ( debug )
			//	DrawChunkGizmo( key );

			results.AddRange( list );
		}

		return results.Count == 0 ? null : results;
	}

	public static void DrawChunkGizmo( (int x, int y, int z) coords )
	{
		var min = new Vector3( coords.x * chunkSize, coords.y * chunkSize, coords.z * chunkSize );
		var max = min + new Vector3( chunkSize, chunkSize, chunkSize );

		var bbox = new BBox( min, max );
	
		Gizmo.Draw.LineBBox( bbox );
	}

	public CoverGenerator( Scene scene ) : base( scene )
	{
		Listen(Stage.StartUpdate, 10, StartGeneration, "");
	}

	const bool debug = false;
	public bool coversGenerated = false;
	bool started = false;
	public static bool doGenerateCover = false;

	public static CoverGenerator Instance = null;

	float searched = 0;

	RealTimeSince generateTime;

	bool lastAiGenerateCover;
	public void StartGeneration()
	{
		if ( !Game.IsPlaying )
		{
			started = false;
			doGenerateCover = false;
			Instance = null;
			searched = 0;
			return;
		}

		Instance = this;

		if ( !doGenerateCover )
			return;

		if (!ai_generatecover)
		{
			coversGenerated = true;
			return;
		}
		if ( ai_generatecover && !lastAiGenerateCover )
			coversGenerated = false;


		lastAiGenerateCover = ai_generatecover;

		if (!started)
		{
			checkedPoints = new();
			coverChunks.Clear();
			generateTime = 0;
			ToastNotification.Current?.BroadcastToast( "Generating Cover Points, Expect Stutters", 3);
		}
			

		started = true;
		
		Debug();

		var bounds = Game.ActiveScene.GetBounds();

		if ( Game.ActiveScene.Components.TryGet<MapInstance>( out var instance, FindMode.EnabledInSelfAndChildren ) )
			bounds = instance.Bounds;

		var count = (int)MathF.Round( bounds.Size.Length );

		if ( coversGenerated )
			return;

		if ( searched > count * 5 )
		{
			coversGenerated = true;
			return;
		}
		
		GenerateCovers( count / 160, bounds );
		searched += count / 160;

		if ( searched > count * 5 )
			ToastNotification.Current?.BroadcastToast( $"Finished Generating in {MathF.Round(generateTime)}s", 3 );
	}

	private static void Debug()
	{
		if ( !debug )
			return;

		Gizmo.Draw.IgnoreDepth = true;

		var camera = Game.ActiveScene.Camera;

		if ( !camera.IsValid() )
			return;

		var nearestChunk = GetChunksInRadius( camera.WorldPosition, chunkSize ) ?? new();

		int i = 0;
		foreach ( var cover in nearestChunk )
		{
			i++;
			Gizmo.Draw.Color = Color.White;
			if ( cover.Position.Distance( camera.WorldPosition ) > chunkSize )
				continue;
			Gizmo.Draw.Line( cover.Position, cover.Position + cover.Direction.WithZ( 0 ) * 10 + (Vector3.Up *(i % 16)));

			Gizmo.Draw.Color = Color.Red;

			if ( cover.IsOwned )
				Gizmo.Draw.Line( cover.Position, cover.Owner.GameObject.WorldPosition );


			if (cover.Position.Distance(camera.WorldPosition) > chunkSize / 4)
				continue;

			var enemyDirection = (camera.WorldPosition - cover.Position).WithZ( 0 ).Normal;

			Gizmo.Draw.Text( Vector3.GetAngle( cover.Direction, enemyDirection ).ToString(), new Transform( cover.Position ) );

			Gizmo.Draw.Color = SimpleGradient.Evaluate( cover.Consistency );
			Gizmo.Draw.Line( cover.Position, cover.Position + Vector3.Up * cover.Height );
		}
	}
	private static List<Vector3> checkedPoints = new();
	public static void GenerateCovers( float count, BBox bounds )
	{
		var radius = Game.ActiveScene.NavMesh.AgentRadius;
		for ( int i = 0; i < count; i++ )
		{
			Vector3 point = Game.ActiveScene.NavMesh.GetRandomPoint( bounds ) ?? default;

			if (point == default)
				continue;

			Vector3 edge = Game.ActiveScene.NavMesh.GetClosestEdge( point, 32 ) ?? default;

			if ( edge == default )
				continue;


			var pDir = (edge - point).Normal;
			var pRotation = Rotation.LookAt( pDir );

			var leftPoint = Game.ActiveScene.NavMesh.GetClosestEdge( edge + pRotation.Left, 16 ) ?? default;
			var rightPoint = Game.ActiveScene.NavMesh.GetClosestEdge( edge + pRotation.Right, 16 ) ?? default;

			if ( leftPoint == default || rightPoint == default )
				continue;

			var dir = (leftPoint - rightPoint).Normal;
			var rotation = Rotation.LookAt( dir );

			var faceDir = Vector3.GetAngle(pDir, rotation.Left) > Vector3.GetAngle(pDir, rotation.Right) ? rotation.Right : rotation.Left;

			if ( Vector3.GetAngle( faceDir, Vector3.Up ) < 45 )
				continue;

			var maxCheck = MathF.Round( MathF.Max( bounds.Size.x * 100, bounds.Size.y * 100 ) );

			var point1 = FindEndAlongEdge( edge, dir, maxCheck, radius );
			point1 += (edge - point1).Normal * radius * 2;

			var point1Trace = WallCheck( point1, Vector3.Down, radius * 0.7f );
			point1 = point1Trace.Hit ? point1Trace.EndPosition : edge;

			var point2 = FindEndAlongEdge( edge, -dir, maxCheck, radius );
			point2 += (edge - point2).Normal * radius * 2;

			var point2Trace = WallCheck( point2, Vector3.Down, radius * 0.7f );
			point2 = point2Trace.Hit ? point2Trace.EndPosition : edge;

			TryAddCoverPoint( point1, faceDir, radius );
			TryAddCoverPoint( point2, faceDir, radius );
			TryAddCoverPoint( point2.LerpTo(point1, 0.5f), faceDir, radius );
		}
	}

	public static void TryAddCoverPoint( Vector3 point, Vector3 direction, float radius )
	{
		if ( point == default )
			return;

		var round = (int)MathF.Round( radius * 4 );

		var roundPoint = new Vector3(
			MathF.Floor( point.x / round ) * round,
			MathF.Floor( point.y / round ) * round,
			MathF.Floor( point.z / round ) * round
		);

		if ( checkedPoints.Contains( roundPoint ) )
			return;

		var rotation = Rotation.LookAt( direction );

		var openCheckLeftShort = WallCheck( point + rotation.Left * radius * 2, direction, radius, 5 );
		var openCheckLeftFar = WallCheck( point + rotation.Left * radius * 4, direction, radius, 5 );
		var openCheckRightShort = WallCheck( point + rotation.Right * radius * 2, direction, radius, 5 );
		var openCheckRightFar = WallCheck( point + rotation.Right * radius * 4, direction, radius, 5 );
		var openCheckTop = WallCheck( point, direction, Game.ActiveScene.NavMesh.AgentHeight, 5 );

		if ( openCheckRightShort.Hit && openCheckRightFar.Hit && openCheckLeftShort.Hit && openCheckLeftFar.Hit && openCheckTop.Hit )
			return;

		checkedPoints.Add( roundPoint );

		var wallCheck = WallCheck( point, direction, radius, radius * 0.5f );
		if ( wallCheck.Hit )
			AddCover( new CoverPoint( point, direction ) );
	}

	public static Vector3 FindEndAlongEdge( Vector3 edge, Vector3 dir, float maxCheck, float radius )
	{
		Vector3 point = default;

		for ( int e = 1; e < maxCheck; e++ )
		{
			var ePoint = edge + dir * e * radius;
			var eEdge = Game.ActiveScene.NavMesh.GetClosestEdge( ePoint, 32 ) ?? default;
			if ( eEdge == default )
				break;

			var closetPointOnDirection = ClosestPointOnEdge( eEdge, edge, edge + dir );

			if ( closetPointOnDirection.WithZ(eEdge.z).Distance( eEdge ) < 3 )
				continue;

			point = closetPointOnDirection;
			break;
		}

		return point;
	}

	public static Vector3 ClosestPointOnEdge(Vector3 position, Vector3 a, Vector3 b)
	{
		Vector3 ab = b - a;
		Vector3 ap = position - a;

		float t = Vector3.Dot( ap, ab ) / Vector3.Dot( ab, ab );

		Vector3 projection = a + t * ab;

		return projection;
	}

	public static SceneTraceResult WallCheck( Vector3 position, Vector3 direction, float height, float size = 0 ) => Game.ActiveScene.Trace.Ray( position + Vector3.Up * height, position + Vector3.Up * height + direction * MathF.Abs( Game.ActiveScene.NavMesh.AgentRadius ) * 2 ).IgnoreDynamic().Run();

	public static class SimpleGradient
	{
		// Evaluates a float t in range [0..1]
		public static Color Evaluate( float t )
		{
			t = Math.Clamp( t, 0f, 1f );

			if ( t < 0.5f )
			{
				// Green → Orange
				float lerp = t / 0.5f;
				return LerpColor( Color.Red, Color.Orange, lerp );
			}
			else
			{
				// Orange → Red
				float lerp = (t - 0.5f) / 0.5f;
				return LerpColor( Color.Red, Color.Green, lerp );
			}
		}

		private static Color LerpColor( Color a, Color b, float t )
		{
			return new Color(
				a.r + (b.r - a.r) * t,
				a.g + (b.g - a.g) * t,
				a.b + (b.b - a.b) * t,
				a.a + (b.a - a.a) * t
			);
		}
	}
}
