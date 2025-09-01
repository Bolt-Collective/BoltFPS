namespace Seekers;

public partial class Pawn : ShrimplePawns.Pawn
{
	[Property, Group( "Pickup" )] public float MaxPullDistance => 200.0f;
	[Property, Group( "Pickup" )] public float LinearFrequency => 10.0f;
	[Property, Group( "Pickup" )] public float LinearDampingRatio => 1.0f;
	[Property, Group( "Pickup" )] public float AngularFrequency => 10.0f;
	[Property, Group( "Pickup" )] public float AngularDampingRatio => 1.0f;
	[Property, Group( "Pickup" )] public float ThrowForce => 200.0f;
	[Property, Group( "Pickup" )] public float HoldDistance => 50.0f;
	[Property, Group( "Pickup" )] public float AttachDistance => 100.0f;
	[Property, Group( "Pickup" )] public float DropCooldown => 0.5f;
	[Property, Group( "Pickup" )] public float BreakLinearForce => 2000.0f;

	[Property, Group( "Pickup" )] public bool CanPickupObjects { get; set; } = true;

	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync, Property] public GameObject GrabbedObject { get; set; }
	public bool IsUsing => GrabbedObject.IsValid();
	[Sync] public Vector3 GrabbedPos { get; set; }
	[Sync] public int GrabbedBone { get; set; } = -1;
	public Vector3 AttachPos { get; set; }

	GameObject lastGrabbed = null;

	PhysicsBody _heldBody;

	PhysicsBody HeldBody
	{
		get
		{
			if ( (GrabbedObject != lastGrabbed && !GrabbedObject.IsValid()) || _heldBody == null )
			{
				_heldBody = GetBody( GrabbedObject );
			}

			lastGrabbed = GrabbedObject;
			return _heldBody;
		}
	}

	PhysicsBody GetBody( GameObject gameObject )
	{
		Log.Info( gameObject );
		Rigidbody rigidbody = gameObject.Components.Get<Rigidbody>();
		return rigidbody.PhysicsBody;
	}

	TimeSince timeSinceImpulse;

	void Move()
	{
		if ( !GrabbedObject.IsValid() )
			return;

		if ( GrabbedObject.IsProxy )
			return;

		if ( timeSinceImpulse < Time.Delta * 5 )
			return;

		if ( !HeldBody.IsValid() )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, (HeldBody.Mass / 1000).Clamp( 0.1f, 100 ),
			Time.Delta );
		HeldBody.Velocity = velocity;

		var angularVelocity = HeldBody.AngularVelocity;
		Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, (HeldBody.Mass / 1000).Clamp( 0.1f, 100 ),
			Time.Delta );
		HeldBody.AngularVelocity = angularVelocity;
	}

	TimeSince timeSinceDrop;

	public void Pickup()
	{
		Move();

		if ( IsProxy )
			return;
		if ( !Controller.IsValid() && !Camera.IsValid() )
			return;

		var eyePos = AimRay.Project( MathF.Abs( Camera.LocalPosition.x ) );
		var eyeRot = Controller.EyeAngles;
		var eyeDir = AimRay.Forward;

		if ( GrabbedObject.IsValid() )
		{
			var attachPos = GrabbedObject.WorldTransform.PointToWorld( AttachPos );
			if ( Input.Pressed( "attack2" ) )
			{
				Throw( eyeDir );
			}
			else if ( Input.Released( "use" ) || eyePos.Distance( attachPos ) > AttachDistance )
			{
				GrabEnd();
			}
			else
			{
				GrabMove( eyePos, eyeDir, eyeRot );
			}

			return;
		}

		if ( timeSinceDrop < DropCooldown )
			return;

		var tr = Scene.Trace.Ray( eyePos, eyePos + eyeDir * MaxPullDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "debris", "nocollide" )
			.WithoutTags( "movement" )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Radius( 2.0f )
			.Run();

		if ( !tr.Hit || !tr.GameObject.IsValid() || !tr.GameObject.IsValid() ||
		     tr.Component is MapCollider )
			return;

		var rb = tr.GameObject.Components.Get<Rigidbody>( true );

		if ( !rb.IsValid() )
			return;

		if ( !rb.MotionEnabled )
			return;

		if ( tr.GameObject.Tags.Has( "grabbed" ) )
			return;

		if ( tr.GameObject.Components.TryGet<IPressable>( out var pressable ) )
			return;

		if ( Input.Down( "use" ) && CanPickupObjects )
		{
			var attachPos = tr.HitPosition;

			AttachPos = tr.GameObject.WorldTransform.PointToLocal( attachPos );

			if ( eyePos.Distance( attachPos ) <= AttachDistance )
			{
				var holdDistance = HoldDistance;

				GrabStart( tr.GameObject, eyePos + eyeDir * holdDistance, eyeRot, tr.HitPosition );
			}
		}
	}

	[Rpc.Host]
	public void Throw( Vector3 eyeDir )
	{
		GameObject grabbedObject = GrabbedObject;
		int grabbedBone = GrabbedBone;
		PhysicsBody heldBody = HeldBody;

		GrabEnd();

		if ( !grabbedObject.IsValid() || !heldBody.IsValid() )
			return;

		if ( heldBody.PhysicsGroup.IsValid() && heldBody.PhysicsGroup.BodyCount > 1 )
		{
			for ( int i = 0; i < heldBody.PhysicsGroup.Bodies.Count(); i++ )
			{
				ApplyImpulse( grabbedObject, i, eyeDir * (heldBody.Mass * ThrowForce * 0.5f) );
				ApplyAngularImpulse( grabbedObject, i, heldBody.Mass * Vector3.Random * ThrowForce );
			}
		}
		else
		{
			ApplyImpulse( grabbedObject, grabbedBone, eyeDir * (heldBody.Mass * ThrowForce) );
			ApplyAngularImpulse( grabbedObject, grabbedBone, Vector3.Random * (heldBody.Mass * ThrowForce) );
		}
	}

	[Rpc.Host]
	private void ApplyImpulse( GameObject gameObject, int bodyIndex, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		PhysicsBody body = gameObject.Components.Get<Rigidbody>()?.PhysicsBody;

		if ( !body.IsValid() )
			return;

		if ( body.IsValid() ) body.ApplyImpulse( velocity );
	}

	[Rpc.Broadcast]
	private void ApplyAngularImpulse( GameObject gameObject, int bodyIndex, Vector3 velocity )
	{
		if ( !gameObject.IsValid() )
			return;

		timeSinceImpulse = 0;

		PhysicsBody body = gameObject.Components.Get<Rigidbody>()?.PhysicsBody;

		if ( body.IsValid() ) body.ApplyAngularImpulse( velocity );
	}

	Vector3 heldPos;
	Rotation heldRot;

	private void GrabStart( GameObject gameObject, Vector3 grabPos, Rotation grabRot, Vector3 hitPos )
	{
		GrabEnd();

		GrabbedObject = gameObject;

		heldRot = Controller.EyeAngles.ToRotation().Inverse * GrabbedObject.WorldRotation;
		heldPos = GrabbedObject.WorldTransform.PointToLocal( hitPos );

		HoldPos = GrabbedObject.WorldPosition;
		HoldRot = GrabbedObject.WorldRotation;
	}

	private void GrabMove( Vector3 startPos, Vector3 dir, Rotation rot )
	{
		if ( !GrabbedObject.IsValid() )
			return;

		var holdDistance = HoldDistance;

		HoldPos = startPos - heldPos * GrabbedObject.WorldRotation + dir * holdDistance;
		HoldRot = rot * heldRot;
	}

	[Rpc.Broadcast]
	private void GrabEnd()
	{
		timeSinceDrop = 0;
		heldRot = Rotation.Identity;

		if ( GrabbedObject.IsValid() )
		{
			GrabbedObject.Tags.Remove( "grabbed" );
		}

		GrabbedObject = null;
		lastGrabbed = null;
		_heldBody = null;
	}
}
