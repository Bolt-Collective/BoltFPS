using Sandbox;
using Sandbox.Services;
using Sandbox.VR;
using System;
using static Sandbox.ModelPhysics;

public abstract partial class Movement : Component
{
	[Range( 0, 200 )] [Property] public float Radius { get; set; } = 16.0f;

	[Range( 0, 200 )] [Property, Sync] public float Height { get; set; } = 64.0f;

	[Range( 0, 50 )] [Property] public float StepHeight { get; set; } = 18.0f;

	[Range( 0, 90 )] [Property] public float GroundAngle { get; set; } = 45.0f;

	[Property] public TagSet IgnoreLayers { get; set; } = new();

	public BBox BoundingBox => new BBox( new Vector3( -Radius, -Radius, 1 ), new Vector3( Radius, Radius, Height ) );

	public SceneTrace BuildTrace( SceneTrace source, BBox hull = default, bool ignoreDynamic = false )
	{
		if ( hull == default )
			hull = BoundingBox;
		var trace = source.Size( in hull ).IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "movement" );

		if ( ignoreDynamic )
			trace = trace.IgnoreDynamic();

		return trace.WithoutTags( IgnoreLayers );
	}

	public SceneTrace BuildTrace( Vector3 start, Vector3 end, BBox hull = default )
	{
		return BuildTrace( Scene.Trace.Ray( start, end ), hull );
	}

	[Property, Sync] public bool IsGrounded { get; set; }
	public GameObject GroundObject;
	public Surface GroundSurface;
	public Vector3 OnGroundVelocity;
	public GameObject PreviousGroundObject;
	public Component GroundComponent;

	public bool SnapToGround = true;

	void CategorizePosition()
	{
		var Position = WorldPosition;
		var point = Position + Vector3.Down * 2;
		var vBumpOrigin = Position;
		var wasOnGround = IsGrounded;

		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Velocity.z > 40.0f )
		{
			ClearGround();
			return;
		}

		//
		// trace down one step height if we're already on the ground "step down". If not, search for floor right below us
		// because if we do StepHeight we'll snap that many units to the ground
		//
		point.z -= wasOnGround ? StepHeight : 0.1f;


		var pm = BuildTrace( Scene.Trace.Ray( vBumpOrigin, point ) ).Run();

		//
		// we didn't hit - or the ground is too steep to be ground
		//
		if ( !pm.Hit || Vector3.GetAngle( Vector3.Up, pm.Normal ) > GroundAngle )
		{
			ClearGround();
			return;
		}

		//
		// we are on ground
		//
		IsGrounded = true;
		PreviousGroundObject = GroundObject;
		GroundObject = pm.GameObject;
		GroundSurface = pm.Surface;

		var body = pm.Body;
		GroundComponent = body?.Component;

		//
		// move to this ground position, if we moved, and hit
		//
		if ( wasOnGround && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f && SnapToGround )
		{
			WorldPosition = pm.EndPosition;
		}
	}


	/// <summary>
	/// Disconnect from ground and punch our velocity. This is useful if you want the player to jump or something.
	/// </summary>
	[Rpc.Owner]
	public void Punch( Vector3 amount, bool clearGround = true )
	{
		ClearGround();
		Velocity += amount;
	}

	[Rpc.Owner]
	public void Slow( Vector3 amount, bool clearGround = true )
	{
		var newVel = Velocity.Normal + amount;

		if ( Velocity.Length <= 1f )
			return;

		if ( Velocity.Angle(newVel) < 90 )
			Velocity += amount;
		else
			Velocity = 0;

	}

	void ClearGround()
	{
		IsGrounded = false;
		GroundObject = default;
		GroundComponent = default;
	}

	[Property, Sync] public Vector3 Velocity { get; set; } = new Vector3();

	public void LaunchUpwards( float amount )
	{
		ClearGround();
		Velocity += Vector3.Up * amount;
		Velocity -= 0 * Time.Delta * 0.5f;
	}

	float previousHeight;
	bool wasGrounded;


	void UpdateGroundVelocity()
	{
		if ( !IsGrounded )
			return;

		if ( GroundObject is null )
		{
			OnGroundVelocity = 0;
			return;
		}

		if ( GroundComponent is Collider collider )
		{
			OnGroundVelocity = collider.GetVelocityAtPoint( WorldPosition );
		}

		if ( GroundComponent is Rigidbody rigidbody )
		{
			OnGroundVelocity = rigidbody.GetVelocityAtPoint( WorldPosition );
		}
	}

	public bool CanMove = true;
	private void Move()
	{
		wasGrounded = IsGrounded;

		var previousVelocity = Velocity;

		var movePos = MovePos();

		WorldPosition = movePos.pos;

		Velocity = movePos.velocity;

		UpdateGroundVelocity();

		WorldPosition += OnGroundVelocity.WithZ( 0 ) * Time.Delta;

		TryUnstuck( previousVelocity );

		CategorizePosition();

		previousHeight = Height;
	}

	public virtual (Vector3 pos, Vector3 velocity) MovePos()
	{
		var ray = Scene.Trace.Ray( WorldPosition, WorldPosition );
		var mover = new CharacterControllerHelper( BuildTrace( ray ), WorldPosition, Velocity );

		if ( IsGrounded )
		{
			mover.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			mover.TryMove( Time.Delta );
		}

		return (mover.Position, mover.Velocity);
	}

	public void MoveTo( Vector3 targetPosition, bool useStep, BBox hull = default )
	{
		if ( TryUnstuck( Velocity ) )
			return;

		var pos = WorldPosition;
		var delta = targetPosition - pos;

		var mover = new CharacterControllerHelper( BuildTrace( pos, pos, hull ), pos, delta );
		mover.MaxStandableAngle = GroundAngle;

		if ( useStep )
		{
			mover.TryMoveWithStep( 1.0f, StepHeight );
		}
		else
		{
			mover.TryMove( 1.0f );
		}

		WorldPosition = mover.Position;
	}


	public virtual bool IsStuck()
	{
		var result = BuildTrace( Scene.Trace.Ray( WorldPosition, WorldPosition ) ).Run();
		return result.StartedSolid;
	}

	Transform _previousTransform;

	public virtual bool TryUnstuck( Vector3 velocity )
	{
		var result = BuildTrace( Scene.Trace.Ray( WorldPosition, WorldPosition ) ).Run();

		if ( !result.StartedSolid )
		{
			_stuckTries = 0;
			_previousTransform = Transform.World;
			return false;
		}

		int AttemptsPerTick = 150;

		var normal = Vector3.Zero;
		var pos = WorldPosition;
		var startpos = WorldPosition;
		for ( int i = 0; i < AttemptsPerTick; i++ )
		{
			if ( i <= 2 )
			{
				pos = WorldPosition + Vector3.Up * ((i) * 0.2f);
			}

			if ( i < 80 )
			{
				normal = velocity.Normal * Time.Delta;
				normal.z = Math.Max( 0, normal.z );
				normal *= 1f;
				var searchdistance = 0.2f;
				if ( i > 70 ) searchdistance = 1f;
				if ( i > 75 ) searchdistance = 3f;
				normal *= searchdistance;
				pos += normal;
			}
			else if ( i < 4 )
			{
				pos = WorldPosition + Vector3.Up * ((i) * 3f);
			}
			else
			{
				normal = Vector3.Random.Normal * (((float)_stuckTries) * 1.25f);
				pos = WorldPosition + normal;
				normal *= 0.25f;
			}

			result = BuildTrace( Scene.Trace.Ray( pos, pos ) ).Run();

			var body = result.Body;
			var component = body?.Component;

			Vector3 stuckObjectVelocity = 0;
			if ( component is Collider collider )
			{
				stuckObjectVelocity = collider.GetVelocityAtPoint( WorldPosition );
			}

			if ( component is Rigidbody rigidbody )
			{
				stuckObjectVelocity = rigidbody.GetVelocityAtPoint( WorldPosition );
			}

			if ( !result.StartedSolid )
			{
				Velocity += normal * 100 + stuckObjectVelocity;
				WorldPosition = pos;
				_previousTransform = Transform.World;
				return false;
			}
		}

		_stuckTries++;

		_previousTransform = Transform.World;
		return true;
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
	}


	int _stuckTries;
}
