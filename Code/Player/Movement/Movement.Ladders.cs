using Sandbox;
using Sandbox.VR;
using System;

public abstract partial class Movement : Component
{
	[Property, Feature( "Ladders" )] public float ClimbSpeed { get; set; } = 100.0f;
	[Property, Feature( "Ladders" )] public string LadderTag { get; set; } = "ladder";

	[Sync]
	public bool IsTouchingLadder { get; set; } = false;
	Vector3 LadderNormal;

	Vector3 LastNonZeroWishLadderVelocity;
	public virtual void CheckLadder()
	{
		var wishvel = new Vector3( WishVelocity.x.Clamp( -1f, 1f ), WishVelocity.y.Clamp( -1f, 1f ), 0 );

		// this is for latching
		if ( wishvel.Length > 0 ) LastNonZeroWishLadderVelocity = wishvel;
		if ( TryLatchNextTickCounter > 0 ) wishvel = LastNonZeroWishLadderVelocity * -1;

		wishvel *= EyeAngles.WithPitch( 0 ).ToRotation();
		wishvel = wishvel.Normal;

		if ( IsTouchingLadder )
		{
			if ( Input.Pressed( "jump" ) )
			{
				var sidem = (Math.Abs( Scene.Camera.WorldRotation.Forward.Abs().z - 1 ) * 3).Clamp( 0, 1 );
				var upm = Scene.Camera.WorldRotation.Forward.z;

				var Eject = new Vector3();

				Eject.x = LadderNormal.x * sidem;
				Eject.y = LadderNormal.y * sidem;
				Eject.z = (3 * upm).Clamp( 0, 1 );

				Velocity += (Eject * 180.0f) * WorldScale;

				IsTouchingLadder = false;

				return;

			}
			else if ( GroundObject != null && LadderNormal.Dot( wishvel ) > 0 )
			{
				IsTouchingLadder = false;

				return;
			}
		}

		const float ladderDistance = 1.0f;
		var start = WorldPosition;
		Vector3 end = start + (IsTouchingLadder ? (LadderNormal * -1.0f) : wishvel) * ladderDistance;

		var pm = Scene.Trace.Ray( start, end )
					.Size( BoundingBox.Mins, BoundingBox.Maxs )
					.WithTag( "ladder" )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();

		// Gizmo.Draw.LineBBox( cc.BoundingBox.Translate( end ) );

		IsTouchingLadder = false;

		if ( pm.Hit )
		{
			IsTouchingLadder = true;
			LadderNormal = pm.Normal;
		}
	}

	public virtual void LadderMove()
	{
		IsGrounded = false;

		var velocity = WishVelocity;
		float normalDot = velocity.Dot( LadderNormal );
		var cross = LadderNormal * normalDot;
		Velocity = (velocity - cross) + (-normalDot * LadderNormal.Cross( Vector3.Up.Cross( LadderNormal ).Normal ));

		Move();
	}

	[ConVar( "debug_movement_ladderlatching" )]
	public static bool LatchDebug { get; set; } = false;

	int TryLatchNextTickCounter = 0;
	Vector3 LastNonZeroWishVelocity;
	private void LadderLatchCheck()
	{

		if ( Input.Down( "jump" ) || Input.Released( "jump" ) )
		{
			TryLatchNextTickCounter = 0;
			return;
		}

		if ( !WishVelocity.Normal.IsNearlyZero( 0.001f ) )
		{
			LastNonZeroWishVelocity = WishVelocity;
		}

		if ( TryLatchNextTickCounter > 0 )
		{
			Velocity = (LastNonZeroWishVelocity.Normal * -100).WithZ( Velocity.z );
			TryLatchNextTickCounter--;
		}

		if ( GroundObject != null ) return;
		if ( PreviousGroundObject == null ) return;

		// Trace downwards and behind the way we are or was walking

		var start = WorldPosition + (Vector3.Down * 16);
		var end = start - (LastNonZeroWishVelocity.Normal * (Radius / 2));

		var pm = Scene.Trace.Ray( start, end )
					.Size( BoundingBox.Mins, BoundingBox.Maxs )
					.WithTag( "ladder" )
					.IgnoreGameObjectHierarchy( GameObject )
					.Run();

		if ( pm.Hit )
		{
			Velocity = Vector3.Zero.WithZ( Velocity.z );
			TryLatchNextTickCounter = 10;
		}

		if ( LatchDebug )
		{
			DebugOverlay.Line( WorldPosition, start, Color.Yellow, 10 );
			DebugOverlay.Line( pm.StartPosition, pm.EndPosition, Color.Red, 10 );
		}
	}
}
