using Sandbox;
using Sandbox.Services;
using Sandbox.VR;
using System;
using static Sandbox.ModelPhysics;

public abstract partial class Movement : Component, IScenePhysicsEvents
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

	[Property, RequireComponent] public BoxCollider Collider { get; set; }
	[Property, RequireComponent] public Rigidbody Body { get; set; }

	[Property]
	public TagSet IgnoreLayers { get; set; } = new();

	public BBox BoundingBox => new BBox( new Vector3( -Radius, -Radius, 1 ), new Vector3( Radius, Radius, Height ) );

	public SceneTrace BuildTrace( SceneTrace source, BBox hull = default )
	{
		if (hull == default)
			hull = BoundingBox;
		var trace = source.Size( in hull ).IgnoreGameObjectHierarchy( GameObject );

		if ( IgnoreDynamic )
			trace = trace.IgnoreDynamic();

		return trace.WithoutTags( IgnoreLayers );
	}

	[Property, Sync] public bool IsGrounded { get; set; } = true;
	public GameObject GroundObject;
	public Vector3 OnGroundVelocity;
	public GameObject PreviousGroundObject;
	public Component GroundComponent;
	public float GroundDistance;

	void CategorizePosition()
	{
		var Position = WorldPosition;
		var point = Position;
		var vBumpOrigin = Position;
		var wasOnGround = IsGrounded;

		// We're flying upwards too fast, never land on ground
		if ( !IsGrounded && Body.Velocity.z > 40.0f )
		{
			ClearGround();
			return;
		}

		//
		// trace down one step height if we're already on the ground "step down". If not, search for floor right below us
		// because if we do StepHeight we'll snap that many units to the ground
		//
		//float zOffset = wasOnGround ? StepHeight : 0.1f;
		point.z -= StepHeight;

		var box = new BBox( BoundingBox.Mins, BoundingBox.Maxs );
		box.Mins *= 0.9f;
		box.Maxs *= 0.9f;

		box.Mins = box.Mins.WithZ( BoundingBox.Mins.z );
		box.Maxs = box.Maxs.WithZ( BoundingBox.Maxs.z );


		var pm = BuildTrace( Scene.Trace.Ray( vBumpOrigin, point ), box ).Run();

		Gizmo.Draw.Line( vBumpOrigin, point );

		Log.Info( pm.Hit );
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

		var body = pm.Body;
		GroundComponent = body?.Component;

		GroundDistance = pm.Distance;
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
		GroundDistance = 100;
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

	float VelocityOff;

	void IScenePhysicsEvents.PrePhysicsStep()
	{

		if(GroundDistance < StepHeight)
			TryStep( StepHeight );
	}

	void IScenePhysicsEvents.PostPhysicsStep()
	{
		UpdateGroundVelocity();

		WorldPosition += OnGroundVelocity.WithZ(0) * Time.Delta;
		Velocity = Body.Velocity;
	}
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

	Vector3 lastGroundVelocity;
	bool canSnap = false;
	private void Move()
	{
		wasGrounded = IsGrounded;

		Body.Velocity = Velocity;

		if ( GroundDistance > 0.1f )
			Body.Velocity += Scene.PhysicsWorld.Gravity * Time.Delta;

		CategorizePosition();
			
		previousHeight = Height;
	}

	public bool IsStuck()
	{
		var result = BuildTrace( Scene.Trace.Ray( WorldPosition, WorldPosition ) ).Run();
		return result.StartedSolid;
	}


	/*
	public virtual void AddVelocity()
	{
		var body = Controller.Body;
		var wish = Controller.WishVelocity;
		if ( wish.IsNearZeroLength ) return;

		var groundFriction = 0.25f + Controller.GroundFriction * 10;
		var groundVelocity = Controller.GroundVelocity;

		var z = body.Velocity.z;

		var velocity = (body.Velocity - Controller.GroundVelocity);
		var speed = velocity.Length;

		var maxSpeed = MathF.Max( wish.Length, speed );

		if ( Controller.IsOnGround )
		{
			var amount = 1 * groundFriction;
			velocity = velocity.AddClamped( wish * amount, wish.Length * amount );
		}
		else
		{
			var amount = 0.05f;
			velocity = velocity.AddClamped( wish * amount, wish.Length );
		}

		if ( velocity.Length > maxSpeed )
			velocity = velocity.Normal * maxSpeed;

		velocity += groundVelocity;

		if ( Controller.IsOnGround )
		{
			velocity.z = z;
		}

		body.Velocity = velocity;
	}
	*/

	protected override void DrawGizmos()
	{
		Gizmo.Draw.LineBBox( BoundingBox );
	}


	int _stuckTries;
}
