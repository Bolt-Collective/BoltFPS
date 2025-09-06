using Sandbox.Navigation;
using System.IO;
using System.Net;

namespace Seekers;

public abstract partial class NPC : Knowable
{
	private CoverGenerator.CoverPoint _currentCover;

	public CoverGenerator.CoverPoint CurrentCover
	{
		get
		{
			if ( _currentCover != null && _currentCover.Owner != this )
				_currentCover = null;
			return _currentCover;
		}
		set
		{
			_currentCover = value;
		}
	}

	TimeUntil nextCoverCheck;

	public void SetCoverPoint( CoverGenerator.CoverPoint coverPoint )
	{
		if ( CurrentCover != null )
			CurrentCover.Owner = null;

		if ( coverPoint != null )
			coverPoint.Owner = this;

		CurrentCover = coverPoint;
	}

	public void FindCover( Knowable enemy, bool forceNew = false )
	{
		if ( (CurrentCoverIsValid( enemy ) && !forceNew) || !CoverGenerator.Instance.coversGenerated )
			return;

		if ( CurrentCover != null )
			CurrentCover.Owner = null;
		CurrentCover = null;

		if ( nextCoverCheck > 0 )
			return;

		nextCoverCheck = 0.2f;
		RetrieveCover( enemy );
	}

	public bool CurrentCoverIsValid( Knowable enemy )
	{
		if ( CurrentCover == null )
			return false;

		var enemyDirection = (enemy.GameObject.WorldPosition - CurrentCover.Position).WithZ( 0 ).Normal;

		if ( Vector3.GetAngle( CurrentCover.Direction, enemyDirection ) > MinCoverAngle )
			return false;

		var distance = CurrentCover.Position.Distance( enemy.GameObject.WorldPosition );

		if ( distance > MaxEngageDistance || distance < MinEngageDistance )
			return false;

		return true;
	}

	private List<CoverGenerator.CoverPoint> validCovers = new();

	public void RetreiveCoverOrder( Knowable enemy )
	{
		validCovers.Clear();
		currentCoverCheckPosition = WorldPosition;

		var collectedCovers = (CoverGenerator.GetChunksInRadius( enemy.GameObject.WorldPosition, MaxEngageDistance ) ??
		                       new List<CoverGenerator.CoverPoint>())
			.OrderBy( c => MathF.Abs( c.Position.Distance( WorldPosition ) - IdealEngageDistance ) )
			.Where( c => c.Height >= 20 );

		var crouchCovers = collectedCovers.Where( c => c.Height <= 32 ).ToList();

		var standingCovers = collectedCovers.Where( c => c.Height > 32 ).ToList();

		crouchCovers.AddRange( standingCovers );

		validCovers = crouchCovers;
	}

	public void CheckCoverAngleAndDistance( Knowable enemy )
	{
		foreach ( var cover in new List<CoverGenerator.CoverPoint>( validCovers ) )
		{
			if ( cover.IsOwned )
			{
				validCovers.Remove( cover );
				continue;
			}

			var enemyDirection = (enemy.GameObject.WorldPosition - cover.Position).WithZ( 0 ).Normal;

			if ( Vector3.GetAngle( cover.Direction, enemyDirection ) > MinCoverAngle )
			{
				validCovers.Remove( cover );
				continue;
			}

			var distance = enemy.GameObject.WorldPosition.Distance( cover.Position );

			if ( distance > MaxEngageDistance || distance < MinEngageDistance )
			{
				validCovers.Remove( cover );
				continue;
			}

			var path = ActiveMesh.CalculatePath( new() { Start = enemy.WorldPosition, Target = cover.Position } );

			if ( path.Status == NavMeshPathStatus.PathNotFound )
			{
				validCovers.Remove( cover );
				continue;
			}


			var pathDistance = GetPathLength( path );

			if ( pathDistance > distance * 2 )
			{
				validCovers.Remove( cover );
				continue;
			}
		}
	}

	public void CheckCoverPlayerPathDistance( Knowable enemy )
	{
		foreach ( var cover in new List<CoverGenerator.CoverPoint>( validCovers ) )
		{
			var path = ActiveMesh.CalculatePath( new() { Start = WorldPosition, Target = cover.Position } );
			var currentDistanceToPlayer = WorldPosition.Distance( enemy.GameObject.WorldPosition );

			if ( currentDistanceToPlayer > MinEngageDistance * 1.2f &&
			     GetClosestDistanceOnPath( path, enemy.GameObject.WorldPosition ) < MinEngageDistance )
			{
				validCovers.Remove( cover );
				continue;
			}
		}
	}

	public void FinalizeCoverCheck()
	{
		coverCheckStep = 0;
		SetCoverPoint( validCovers.Count > 0 ? validCovers[0] : null );
	}

	private Vector3 currentCoverCheckPosition;
	private int coverCheckStep = 0;

	public void RetrieveCover( Knowable enemy )
	{
		if ( WorldPosition.Distance( currentCoverCheckPosition ) > 10 )
			coverCheckStep = 0;

		switch ( coverCheckStep )
		{
			case 0:
				RetreiveCoverOrder( enemy );
				break;
			case 1:
				CheckCoverAngleAndDistance( enemy );
				break;
			case 2:
				CheckCoverPlayerPathDistance( enemy );
				break;
		}

		coverCheckStep++;

		if ( coverCheckStep > 2 )
			FinalizeCoverCheck();
	}

	public static float GetClosestDistanceOnPath( NavMeshPath path, Vector3 position )
	{
		if ( path.Status == NavMeshPathStatus.PathNotFound )
			return float.MaxValue;

		float closestDist = float.MaxValue;

		for ( int i = 0; i < path.Points.Count - 1; i++ )
		{
			Vector3 a = path.Points[i].Position;
			Vector3 b = path.Points[i + 1].Position;

			Vector3 ab = b - a;
			Vector3 ap = position - a;

			float t = Vector3.Dot( ap, ab ) / Vector3.Dot( ab, ab );
			t = MathF.Max( 0, MathF.Min( 1, t ) );

			Vector3 projection = a + t * ab;

			float dist = position.Distance( projection );
			if ( dist < closestDist )
			{
				closestDist = dist;
			}
		}

		return closestDist;
	}

	public static float GetPathLength( NavMeshPath path )
	{
		if ( path.Status == NavMeshPathStatus.PathNotFound )
			return float.MaxValue;

		float length = 0f;

		for ( int i = 1; i < path.Points.Count(); i++ )
		{
			length += Vector3.DistanceBetween( path.Points[i - 1].Position, path.Points[i].Position );
		}

		return length;
	}
}
