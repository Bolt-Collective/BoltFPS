using Sandbox;
using Sandbox.Services;
using Sandbox.UI;
using Sandbox.VR;
using System;
using static Sandbox.ModelPhysics;

public abstract partial class Movement : Component, IScenePhysicsEvents
{
	public bool StepDebug { get; set; } = true;

	// set when we stepped this tick, so at the end of the physics step we can restore our position
	bool _didstep;

	// if we stepped, this holds the position we moved to
	Vector3 _stepPosition;

	/// <summary>
	/// Try to step up. Will trace forward, then up, then across, then down.
	/// </summary>
	internal void TryStep( float maxDistance )
	{
		_didstep = false;

		if ( Velocity.WithZ( 0 ).IsNearlyZero( 0.0001f ) ) return;

		var from = WorldPosition;
		var vel = Velocity.WithZ( 0 ) * Time.Delta;
		BBox bigBox = new BBox( BoundingBox.Mins, BoundingBox.Maxs );
		bigBox.Grow( 5f );

		SceneTraceResult result;

		//
		// Trace forwards, in our current velocity direction
		//
		{
			var a = from - vel.Normal * 0.1f;
			var b = from + vel;

			result = BuildTrace( Scene.Trace.Ray( a, b ), bigBox ).Run();

			// If we're inside something, lose girth until we're not
			if ( result.StartedSolid )
			{
				bigBox = new BBox( BoundingBox.Mins, BoundingBox.Maxs );

				result = BuildTrace( Scene.Trace.Ray( a, b ), bigBox ).Run();
			}

			// If we didn't hit anything, we're done here
			if ( !result.Hit )
				return;

			if ( StepDebug )
			{
				DebugOverlay.Line( a, b, duration: 10, color: Color.Green );
			}

			// Remove the distace travelled from our velocity
			vel = vel.Normal * (vel.Length - result.Distance);
			if ( vel.Length <= 0 ) return;
		}

		//
		// We hit a step, move upwards from this point, one step up
		//
		{
			from = result.EndPosition;
			var uppoint = from + Vector3.Up * maxDistance;

			// move up 
			result = BuildTrace( Scene.Trace.Ray( from, uppoint ), bigBox ).Run();

			if ( result.StartedSolid )
				return;

			// If we hit our head almost immediately, it's too tight to step up
			// we need to draw the line somewhere
			if ( result.Distance < 2 )
			{
				if ( StepDebug ) DebugOverlay.Line( from, result.EndPosition, duration: 10, color: Color.Red );
				return;
			}

			if ( StepDebug )
			{
				DebugOverlay.Line( from, result.EndPosition, duration: 10, color: Color.Green );
			}
		}

		// Move across
		{
			// move across
			var a = result.EndPosition;
			var b = a + vel;

			result = BuildTrace( Scene.Trace.Ray( a, b ), bigBox ).Run();
			if ( result.StartedSolid )
				return;

			if ( StepDebug )
			{
				DebugOverlay.Line( a, b, duration: 10, color: Color.Green );
			}
		}

		//
		// Step Down, back to the ground
		// 
		{
			var dist = result.Distance;
			var top = result.EndPosition;
			var bottom = result.EndPosition + Vector3.Down * maxDistance;

			result = BuildTrace( Scene.Trace.Ray( top, bottom ), bigBox ).Run();

			// no ground here (!)
			if ( !result.Hit )
			{
				if ( StepDebug ) DebugOverlay.Line( top, bottom, duration: 10, color: Color.Red );
				return;
			}

			// can't stand here
			//if ( !Mode.IsStandableSurace( result ) )
			//	return;

			_didstep = true;
			_stepPosition = result.EndPosition + Vector3.Up * 0.1f;

			Body.WorldPosition = Body.WorldPosition.WithZ( _stepPosition.z + 2 );

			if ( StepDebug )
			{
				DebugOverlay.Line( top, _stepPosition, duration: 10, color: Color.Green );
			}
		}
	}

	/// <summary>
	/// If we stepped up on the previous step, we suck our position back to the previous position after the physics step
	/// to avoid adding double velocity. This is technically wrong but doens't seem to cause any harm right now
	/// </summary>
	void RestoreStep()
	{
		if ( _didstep )
		{
			_didstep = false;
			Body.WorldPosition = _stepPosition;
		}
	}
}
