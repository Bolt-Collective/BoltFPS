using Sandbox.Audio;

namespace Seekers;

public partial class PhysGun : BaseWeapon, Component.INetworkListener
{
	[Feature( "Physics" ), Property] public float MinTargetDistance { get; set; } = 0.0f;
	[Feature( "Physics" ), Property] public float MaxTargetDistance { get; set; } = 10000.0f;
	[Feature( "Physics" ), Property] public float TargetDistanceSpeed { get; set; } = 25.0f;
	[Feature( "Physics" ), Property] public float RotateSpeed { get; set; } = 0.125f;
	[Feature( "Physics" ), Property] public float RotateSnapAt { get; set; } = 45.0f;

	[Sync] public bool Beaming { get; set; }
	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync] public bool SnapRotation { get; set; }
	[Sync] public GameObject GrabbedObject { get; set; }
	[Sync] public Vector3 GrabbedPos { get; set; }

	GameObject lastGrabbed;
	PhysicsBody _heldBody;

	// the specific PhysicsBody we grabbed (bone or single prop)
	PhysicsBody GrabbedBody;

	// first rigidbody in the object
	PhysicsBody HeldBody
	{
		get
		{
			if ( GrabbedObject != lastGrabbed && GrabbedObject != null )
			{
				_heldBody = GetBody( GrabbedObject )?.PhysicsBody;
			}

			lastGrabbed = GrabbedObject;
			return _heldBody;
		}
	}

	// unified
	PhysicsBody ActiveBody => GrabbedBody.IsValid() ? GrabbedBody : HeldBody;

	Rigidbody GetBody( GameObject gameObject )
	{
		return gameObject.Components.Get<Rigidbody>( true );
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();
		GrabbedObject = null;
		GrabbedBody = null;
	}

	protected override void OnPreRender()
	{
		UpdateEffects();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Owner.IsValid() && Owner.Controller.IsValid() && Owner.Inventory.IsValid() )
		{
			Owner.Controller.IgnoreCam = Input.Down( "use" ) && GrabbedObject.IsValid();
			Owner.Inventory.CanChange = !GrabbedObject.IsValid();
		}

		if ( !GrabbedObject.IsValid() )
			return;

		if ( GrabbedObject.IsProxy )
			return;

		var body = ActiveBody;
		if ( !body.IsValid() )
			return;

		if ( !body.MotionEnabled )
			return;

		var velocity = body.Velocity;
		Vector3.SmoothDamp( body.Position, HoldPos, ref velocity, 0.075f, Time.Delta );
		body.Velocity = velocity;

		if ( SnapRotation )
		{
			var locking = new PhysicsLock { Pitch = true, Yaw = true, Roll = true };
			body.Locking = locking;

			float rotateSpeed = 25f;
			body.Rotation = Rotation.Slerp( body.Rotation, HoldRot, Time.Delta * rotateSpeed );

			if ( body.Rotation.Distance( HoldRot ) < 0.001f )
				body.Rotation = HoldRot;
		}
		else
		{
			body.Locking = new PhysicsLock();
			var angularVelocity = body.AngularVelocity;
			Rotation.SmoothDamp( body.Rotation, HoldRot, ref angularVelocity, 0.075f, Time.Delta );
			body.AngularVelocity = angularVelocity;
		}
	}

	bool grabbed;

	public override void OnControl()
	{
		Beaming = Input.Down( "attack1" );

		if ( Input.Pressed( "attack1" ) )
			BroadcastAttack();

		if ( !GrabbedObject.IsValid() && Beaming && !grabbed && TryStartGrab() )
			grabbed = true;

		if ( Beaming && !GrabbedObject.IsValid() && !Grab().isValid )
			grabbed = false;

		if ( Input.Released( "attack1" ) )
		{
			TryEndGrab();
			grabbed = false;
		}

		if ( Input.Pressed( "reload" ) && Input.Down( "run" ) )
			TryUnfreezeAll();

		if ( !GrabbedObject.IsValid() )
			return;

		if ( Input.Pressed( "attack2" ) )
		{
			BroadcastAttack();
			Freeze( GrabbedObject );
			GrabbedObject = null;
			GrabbedBody = null;
			return;
		}

		MoveTargetDistance( Input.MouseWheel.y * TargetDistanceSpeed );
		Owner.CanUse = !Input.Down( "use" );

		if ( Input.Down( "use" ) )
			DoRotate( new Angles( 0.0f, Rotation.LookAt( Owner.AimRay.Forward ).Angles().yaw, 0.0f ),
				Input.MouseDelta * RotateSpeed );

		HoldPos = Owner.AimRay.Position - heldPos * GrabbedObject.WorldRotation + Owner.AimRay.Forward * holdDistance;
		HoldRot = Owner.Controller.EyeAngles * heldRot;

		if ( Input.Down( "run" ) && Input.Down( "use" ) )
		{
			SnapRotation = true;
			var angles = HoldRot.Angles();
			HoldRot = Rotation.From(
				MathF.Round( angles.pitch / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.yaw / RotateSnapAt ) * RotateSnapAt,
				MathF.Round( angles.roll / RotateSnapAt ) * RotateSnapAt
			);
		}
		else
			SnapRotation = false;
	}

	[Rpc.Broadcast]
	private void TryUnfreezeAll()
	{
		// unchanged from your code
		// ...
	}

	// freeze individual bone if GrabbedBody valid, else whole prop
	[Rpc.Broadcast]
	public void Freeze( GameObject gameObject )
	{
		if ( gameObject.IsProxy || !gameObject.IsValid() )
			return;

		if ( GrabbedBody.IsValid() )
		{
			GrabbedBody.MotionEnabled = false;
			gameObject.Tags.Add( "frozen" );
			FreezeEffects();
			return;
		}

		var body = GetBody( gameObject )?.PhysicsBody;
		if ( body.IsValid() )
		{
			body.MotionEnabled = false;
			gameObject.Tags.Add( "frozen" );
			FreezeEffects();
		}
	}

	[Rpc.Broadcast]
	public void UnFreeze( GameObject gameObject )
	{
		if ( gameObject.IsProxy || !gameObject.IsValid() )
			return;

		if ( GrabbedBody.IsValid() )
		{
			GrabbedBody.MotionEnabled = true;
			gameObject.Tags.Remove( "frozen" );
			return;
		}

		var body = GetBody( gameObject )?.PhysicsBody;
		if ( body.IsValid() )
		{
			body.MotionEnabled = true;
			gameObject.Tags.Remove( "frozen" );
		}
	}

	[Rpc.Broadcast]
	private void TryEndGrab()
	{
		var body = ActiveBody;
		if ( body.IsValid() )
			body.Locking = new PhysicsLock();

		if ( !GrabbedObject.IsValid() )
			return;

		if ( GrabbedObject.Root.Components.TryGet<NavMeshAgent>( out var agent ) )
		{
			agent.Velocity = 0;
		}

		GrabbedObject = null;
		GrabbedBody = null;
		lastGrabbed = null;
	}

	private void MoveTargetDistance( float distance )
	{
		holdDistance += distance;
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );
	}

	public void DoRotate( Rotation eye, Vector3 input )
	{
		var localRot = eye;
		localRot *= Rotation.FromAxis( Vector3.Up, input.x * RotateSpeed );
		localRot *= Rotation.FromAxis( Vector3.Right, input.y * RotateSpeed );
		localRot = eye.Inverse * localRot;
		heldRot = localRot * heldRot;
	}

	// Grabbing
	private bool TryStartGrab()
	{
		(var isValid, var tr) = Grab();
		if ( !isValid )
			return false;

		GrabbedObject = tr.GameObject;
		GrabbedBody = tr.Body; // store specific bone or body

		holdDistance = Vector3.DistanceBetween( Owner.AimRay.Position, tr.EndPosition );
		holdDistance = holdDistance.Clamp( MinTargetDistance, MaxTargetDistance );

		heldRot = Owner.Controller.EyeAngles.ToRotation().Inverse * GrabbedObject.WorldRotation;
		heldPos = GrabbedObject.WorldTransform.PointToLocal( tr.EndPosition );

		HoldPos = GrabbedObject.WorldPosition;
		HoldRot = GrabbedObject.WorldRotation;

		GrabbedPos = tr.Body.Transform.PointToLocal( tr.EndPosition );

		UnFreeze( GrabbedObject );
		return true;
	}

	(bool isValid, SceneTraceResult result) Grab()
	{
		var tr = Scene.Trace.Ray( Owner.AimRay, MaxTargetDistance )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "debris", "nocollide" )
			.WithoutTags( "movement", "ignorebullets" )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

		bool valid =
			tr.Hit &&
			(tr.GameObject.Components.Get<Rigidbody>().IsValid() ||
			 tr.GameObject.Components.Get<ModelPhysics>().IsValid() ||
			 tr.GameObject.GetComponent<PropHelper>().IsValid()) &&
			tr.GameObject.IsValid() &&
			tr.Component is not MapCollider &&
			tr.Body.IsValid() &&
			!tr.StartedSolid &&
			!tr.Tags.Contains( "grabbed" );

		return (valid, tr);
	}

	[Rpc.Broadcast]
	private void BroadcastAttack()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}

	public static List<GameObject> GetAllConnectedProps( GameObject gameObject )
	{
		PropHelper propHelper = gameObject.Root.Components.Get<PropHelper>();

		if ( !propHelper.IsValid() )
			return null;

		var result = new List<Joint>();
		var visited = new HashSet<PropHelper>();

		CollectWelds( propHelper, result, visited );

		List<GameObject> returned = new List<GameObject> { gameObject };

		foreach ( Joint joint in result )
		{
			if ( !joint.GameObject.IsValid() || !joint.Body.IsValid() )
				continue;
			GameObject object1 = joint.GameObject.Root;
			GameObject object2 = joint.Body.Root;

			if ( !returned.Contains( object1 ) ) returned.Add( object1 );
			if ( !returned.Contains( object2 ) ) returned.Add( object2 );
		}

		return returned;
	}


	private static void CollectWelds( PropHelper propHelper, List<Joint> result, HashSet<PropHelper> visited )
	{
		if ( visited.Contains( propHelper ) )
			return;

		visited.Add( propHelper );
		result.AddRange( propHelper.Joints );

		foreach ( var joint in propHelper.Joints )
		{
			if ( !joint.GameObject.IsValid() )
				continue;

			GameObject jointObject = joint.GameObject.Root;

			if ( jointObject == propHelper.GameObject )
				jointObject = joint.Body.Root;

			if ( !jointObject.IsValid() )
				return;

			PropHelper propHelper1 = jointObject.Root.Components.GetInParentOrSelf<PropHelper>();

			if ( !propHelper1.IsValid() )
				return;

			CollectWelds( propHelper1, result, visited );
		}
	}


	Vector3 heldPos;
	Rotation heldRot;
	float holdDistance;
}
