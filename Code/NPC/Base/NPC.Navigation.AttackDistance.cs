using Sandbox.Navigation;
using System.IO;
using System.Net;

namespace Seekers;

public abstract partial class NPC : Knowable
{
	public Vector3? MaintainAttackDistance( Knowable enemy )
	{
		var distance = (Agent.TargetPosition.HasValue ? Agent.TargetPosition.Value : WorldPosition).Distance( enemy.Position );

		if (distance < IdealEngageDistance.LerpTo(MaxEngageDistance, DistancePadding) && distance > IdealEngageDistance.LerpTo(MinEngageDistance, DistancePadding) )
			return null;

		var enemyTo = (WorldPosition - enemy.GameObject.WorldPosition).WithZ( 0 ).Normal;

		var targetPosition = enemy.GameObject.WorldPosition + enemyTo * IdealEngageDistance;

		if (WallCheck(enemy.GameObject.WorldPosition, enemyTo * IdealEngageDistance).Hit)
		{
			var rotateCheck = RotateCheck( enemyTo, enemy );
			if ( rotateCheck.HasValue )
				targetPosition = rotateCheck.Value;
			else
				targetPosition = WallCheck( enemy.WorldPosition + Vector3.Up * 10, enemyTo * IdealEngageDistance ).EndPosition;
		}

		return targetPosition;
	}

	public Vector3? RotateCheck(Vector3 enemyTo, Knowable enemy)
	{
		var leftDir = enemyTo * new Angles( 0, -5, 0 );
		var leftCheck = WallCheck( enemy.WorldPosition + Vector3.Up * 10, leftDir * IdealEngageDistance );

		var rightDir = enemyTo * new Angles( 0, 5, 0 );
		var rightCheck = WallCheck( enemy.WorldPosition + Vector3.Up * 10, rightDir * IdealEngageDistance );

		if ( !leftCheck.Hit && !rightCheck.Hit )
			return null;

		var goLeft = leftCheck.Distance > rightCheck.Distance;

		var rotateDir = goLeft ? -5 : 5;

		var bestCheck = Vector3.Zero;

		float bestCheckDistance = float.MaxValue;

		for ( int i = 2; i < 360 / 5; i++ )
		{
			var dir = enemyTo * new Angles(0,rotateDir * i,0);

			var check = WallCheck( enemy.WorldPosition + Vector3.Up * 5, dir * IdealEngageDistance );

			if ( !check.Hit || check.Distance > IdealEngageDistance.LerpTo( MinEngageDistance, DistancePadding / 2 ) )
			{
				return check.EndPosition - Vector3.Up * 5;
			}


			if ( bestCheck == Vector3.Zero )
				bestCheck = check.EndPosition;

			if ( MathF.Abs( check.Distance - IdealEngageDistance ) < MathF.Abs(bestCheckDistance - IdealEngageDistance) )
			{
				bestCheckDistance = check.Distance;
				bestCheck = check.EndPosition;
			}
		}

		return bestCheck == Vector3.Zero ? null : bestCheck - Vector3.Up * 5;
	}

}

