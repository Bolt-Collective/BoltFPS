using Sandbox;
using System;

public abstract partial class Movement : Component
{
	[Range( 0, 200 )]
	[Property] public float Radius { get; set; } = 16.0f;

	[Range( 0, 200 )]
	[Property, Sync] public float Height { get; set; } = 64.0f;

	[Range( 0, 50 )]
	[Property] public float StepHeight { get; set; } = 18.0f;

	[Range( 0, 90 )]
	[Property] public float GroundAngle { get; set; } = 45.0f;

	[Property] public bool IgnoreDynamic;

	[Property, RequireComponent] public CapsuleCollider Collider { get; set; }

	[Property]
	public TagSet IgnoreLayers { get; set; } = new();

	public BBox BoundingBox => new BBox( new Vector3( -Radius, -Radius, 0 ), new Vector3( Radius, Radius, Height ) );

	public SceneTrace BuildTrace( SceneTrace source )
	{
		BBox hull = BoundingBox;
		var trace = source.Size( in hull ).IgnoreGameObjectHierarchy( base.GameObject );

		if ( IgnoreDynamic )
			trace = trace.IgnoreDynamic();

		return trace.WithoutTags( IgnoreLayers );
	}

	[Property, Sync] public bool IsGrounded { get; set; } = true;
	public GameObject GroundObject;
	public GameObject PreviousGroundObject;
	public Collider GroundCollider;

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
		GroundCollider = pm.Shape?.Collider as Collider;

		//
		// move to this ground position, if we moved, and hit
		//
		if ( wasOnGround && !pm.StartedSolid && pm.Fraction > 0.0f && pm.Fraction < 1.0f )
		{
			WorldPosition = pm.EndPosition;
		}
	}

	/// <summary>
	/// Disconnect from ground and punch our velocity. This is useful if you want the player to jump or something.
	/// </summary>
	public void Punch( in Vector3 amount )
	{
		ClearGround();
		Velocity += amount;
	}

	void ClearGround()
	{
		IsGrounded = false;
		GroundObject = default;
		GroundCollider = default;
	}

	[Property, Sync] public Vector3 Velocity { get; set; } = new Vector3();

	public void LaunchUpwards( float amount )
	{
		ClearGround();
		Velocity += Vector3.Up * amount;
		Velocity -= 0 * Time.Delta * 0.5f;
	}

	GameObject lastGroundObject;
	Transform lastGroundTransform;
	Vector3 lastPosition;
	private Vector3 PlatformVelocity()
	{
		if ( !GroundObject.IsValid() || !IsGrounded )
		{
			lastGroundObject = null;
			return Vector3.Zero;
		}

		var direction = Vector3.Zero;

		if (GroundObject == lastGroundObject)
		{
			var localPosition = lastGroundTransform.PointToLocal( lastPosition );
			var targetPosition = GroundObject.WorldTransform.PointToWorld( localPosition );
			direction = (targetPosition - lastPosition);
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.Line( lastPosition, targetPosition );
			Velocity += direction;
		}

		lastGroundObject = GroundObject;
		lastGroundTransform = new Transform( GroundObject.WorldPosition, GroundObject.WorldRotation, GroundObject.WorldScale );
		lastPosition = WorldPosition;
		return direction;
	}

	float previousHeight;
	bool wasGrounded;
	public bool noPlatform;
	Vector3 platformVelocity;
	private void Move()
	{
		if (!IsGrounded && wasGrounded && !noPlatform)
		{
			Velocity += platformVelocity / Time.Delta;
		}
		wasGrounded = IsGrounded;

		platformVelocity = PlatformVelocity();

		var ray = Scene.Trace.Ray( WorldPosition, WorldPosition );
		var mover = new CharacterControllerHelper( BuildTrace( ray ), WorldPosition, Velocity );
		var previousVelocity = Velocity;

		if ( IsGrounded )
		{
			mover.TryMoveWithStep( Time.Delta, StepHeight );
		}
		else
		{
			mover.TryMove( Time.Delta );
		}

		WorldPosition = mover.Position;
		if ( !noPlatform )
			WorldPosition += platformVelocity;

		Velocity = mover.Velocity;

		if ( IsStuck() )
			TryUnstuck(previousVelocity);

		CategorizePosition();

		previousHeight = Height;
	}

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
	}


	int _stuckTries;

	public bool IsStuck()
	{
		var result = BuildTrace( Scene.Trace.Ray( WorldPosition, WorldPosition ) ).Run();
		return result.StartedSolid;
	}

	Transform _previousTransform;

	bool TryUnstuck( Vector3 velocity )
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

			if ( !result.StartedSolid )
			{
				Velocity += normal / Time.Delta;
				WorldPosition = pos;
				_previousTransform = Transform.World;
				return false;
			}
		}

		_stuckTries++;

		_previousTransform = Transform.World;
		return true;
	}
}
