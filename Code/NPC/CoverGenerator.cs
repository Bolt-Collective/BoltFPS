using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;

namespace Seekers;

public class CoverGenerator : GameObjectSystem
{
	public class CoverPoint
	{
		public Vector3 Position;
		public Vector3 Direction;
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

			if ( debug )
				DrawChunkGizmo( key );

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
	const bool debug = true;
	public bool coversGenerated = false;
	bool started = false;

	public static CoverGenerator Instance = null;

	float searched = 0;
	public void StartGeneration()
	{
		if ( !Game.IsPlaying )
		{
			started = false;
			Instance = null;
			searched = 0;
			return;
		}

		if (!started)
		{
			coverChunks.Clear();
			Instance = this;	
		}
			

		started = true;
		
		Debug();

		var bounds = Game.ActiveScene.GetBounds();

		if ( Game.ActiveScene.Components.TryGet<MapInstance>( out var instance, FindMode.EnabledInSelfAndChildren ) )
			bounds = instance.Bounds;

		var count = (int)MathF.Round( bounds.Size.Length );

		if ( coversGenerated )
			return;

		if ( searched > count * 25 )
			coversGenerated = true;
		
		GenerateCovers( count, bounds );
		float progress = 1f - (float)searched / (count * 60);
		Log.Info( progress );
		searched += count;
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

		foreach ( var cover in nearestChunk )
		{
			Gizmo.Draw.Color = Color.White;
			if ( cover.Position.Distance( camera.WorldPosition ) > chunkSize )
				continue;
			Gizmo.Draw.Line( cover.Position, cover.Position + cover.Direction.WithZ( 0 ) * 10 );
			if (cover.Position.Distance(camera.WorldPosition) > chunkSize / 4)
				continue;

			var enemyDirection = (camera.WorldPosition - cover.Position).WithZ( 0 ).Normal;

			Gizmo.Draw.Text( Vector3.GetAngle( cover.Direction, enemyDirection ).ToString(), new Transform( cover.Position ) );

			Gizmo.Draw.Color = SimpleGradient.Evaluate( cover.Consistency );
			Gizmo.Draw.Line( cover.Position, cover.Position + Vector3.Up * cover.Height );
		}
	}

	public static void GenerateCovers( float count, BBox bounds )
	{
		var checkedPoints = new List<Vector3>();

		for ( int i = 0; i < count; i++ )
		{
			//random not every 32 chunk because it actually runs better
			Vector3 point = Game.ActiveScene.NavMesh.GetRandomPoint( bounds ) ?? default;

			if (point == default)
				continue;

			point = new Vector3(
				MathF.Floor( point.x / 16 ) * 16,
				MathF.Floor( point.y / 16 ) * 16,
				MathF.Floor( point.z / 16 ) * 16
			);

			Vector3 edge = Game.ActiveScene.NavMesh.GetClosestEdge( point, 32 ) ?? default;

			if ( edge == default )
				continue;

			edge =  new Vector3(
				MathF.Floor( edge.x / 16 ) * 16,
				MathF.Floor( edge.y / 16 ) * 16,
				edge.z
			);

			if ( checkedPoints.Contains( edge.WithZ(MathF.Floor(edge.z / 16) * 16) ) )
				continue;

			checkedPoints.Add( edge.WithZ( MathF.Floor( edge.z / 16 ) * 16 ) );
			 
			var direction = (edge - point).WithZ(0).Normal;

			var radius = Game.ActiveScene.NavMesh.AgentRadius;
			var wallCheck = WallCheck( edge, direction, radius, radius * 0.5f );

			var rotation = Rotation.LookAt( direction );
			var wallCheckLeft = WallCheck( edge + rotation.Left * radius, direction, radius, 5 ); 
			var wallCheckRight = WallCheck( edge + rotation.Right * radius, direction, radius, 5 );

			if ( !wallCheck.Hit || !wallCheckLeft.Hit || !wallCheckRight.Hit )
				continue;

			AddCover( new CoverPoint( edge, -wallCheck.Normal ) );
		}
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
