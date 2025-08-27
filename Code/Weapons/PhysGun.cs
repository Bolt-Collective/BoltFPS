using Sandbox.Audio;
using System.Security.Cryptography.X509Certificates;

namespace Seekers;

public partial class PhysGun : BaseWeapon, Component.INetworkListener
{
	[Feature( "Physics" ), Property] public float MinTargetDistance { get; set; } = 0.0f;
	[Feature( "Physics" ), Property] public float MaxTargetDistance { get; set; } = 10000.0f;
	[Feature( "Physics" ), Property] public float LinearFrequency { get; set; } = 20.0f;
	[Feature( "Physics" ), Property] public float LinearDampingRatio { get; set; } = 1.0f;
	[Feature( "Physics" ), Property] public float AngularFrequency { get; set; } = 20.0f;
	[Feature( "Physics" ), Property] public float AngularDampingRatio { get; set; } = 1.0f;
	[Feature( "Physics" ), Property] public float TargetDistanceSpeed { get; set; } = 25.0f;
	[Feature( "Physics" ), Property] public float RotateSpeed { get; set; } = 0.125f;
	[Feature( "Physics" ), Property] public float RotateSnapAt { get; set; } = 45.0f;

	[Sync] public bool Beaming { get; set; }
	[Sync] public Vector3 HoldPos { get; set; }
	[Sync] public Rotation HoldRot { get; set; }
	[Sync] public bool SnapRotation { get; set; }
	[Sync] public GameObject GrabbedObject { get; set; }
	[Sync] public Vector3 GrabbedPos { get; set; }

	GameObject lastGrabbed = null;

	PhysicsBody _heldBody;
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

	Rigidbody GetBody( GameObject gameObject )
	{
		Rigidbody rigidbody = gameObject.Components.Get<Rigidbody>(true);
		return rigidbody;
	}

	protected override void OnEnabled()
	{
		base.OnEnabled();

		GrabbedObject = null;
	}

	protected override void OnPreRender()
	{
		UpdateEffects();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( Owner.IsValid() && Owner.Controller.IsValid() && Owner.Inventory.IsValid())
		{
			Owner.Controller.IgnoreCam = Input.Down( "use" ) && GrabbedObject.IsValid();

			Owner.Inventory.CanChange = !GrabbedObject.IsValid();
		}

		if ( !GrabbedObject.IsValid() )
			return;

		if ( !HeldBody.IsValid() )
		{
			if ( GrabbedObject.Components.TryGet<PropHelper>( out var ph ) && ph.CanFreeze )
				ph.Prop.IsStatic = false;

			return;
		}

		if ( !HeldBody.MotionEnabled )
			return;

		var velocity = HeldBody.Velocity;
		Vector3.SmoothDamp( HeldBody.Position, HoldPos, ref velocity, 0.075f, Time.Delta );
		HeldBody.Velocity = velocity;

		if ( SnapRotation )
		{
			var locking = new PhysicsLock();
			locking.Pitch = true;
			locking.Yaw = true;
			locking.Roll = true;
			HeldBody.Locking = locking;

			float rotateSpeed = 25f;
			HeldBody.Rotation = Rotation.Slerp(
				HeldBody.Rotation,
				HoldRot,
				Time.Delta * rotateSpeed
			);

			if ( HeldBody.Rotation.Distance( HoldRot ) < 0.001f )
				HeldBody.Rotation = HoldRot;

			return;
		}
		else
		{
			HeldBody.Locking = new PhysicsLock();
			var angularVelocity = HeldBody.AngularVelocity;
			Rotation.SmoothDamp( HeldBody.Rotation, HoldRot, ref angularVelocity, 0.075f, Time.Delta );
			HeldBody.AngularVelocity = angularVelocity;
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
			return;
		}

		MoveTargetDistance( Input.MouseWheel.y * TargetDistanceSpeed );

		Owner.CanUse = !Input.Down( "use" );

		if ( Input.Down( "use" ) )
			DoRotate( new Angles( 0.0f, Rotation.LookAt(Owner.AimRay.Forward).Angles().yaw, 0.0f ), Input.MouseDelta * RotateSpeed );

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
		var rootEnt = GrabbedObject;

		if ( !GrabbedObject.IsValid() )
		{
			var tr = Scene.Trace.Ray( Owner.AimRay, MaxTargetDistance )
			.UseHitboxes()
			.WithoutTags( "movement" )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

			if ( !tr.Hit || !tr.GameObject.IsValid() || tr.Component is MapCollider ) return;

			rootEnt = tr.GameObject.Root;
		}

		if ( !rootEnt.IsValid() )
			return;

		var weldContexts = GetAllConnectedProps( rootEnt );

		if ( weldContexts == null )
			return;

		for ( int i = 0; i < weldContexts.Count; i++ )
		{
			ModelPhysics modelPhysics = weldContexts[i]?.GetComponent<ModelPhysics>();
			Rigidbody rigidbody = weldContexts[i]?.GetComponent<Rigidbody>();

			Log.Info( weldContexts[i] );

			if ( modelPhysics.IsValid() )
			{
				foreach(var ragBody in modelPhysics.Bodies)
				{
					if ( ragBody.Component.PhysicsBody.BodyType == PhysicsBodyType.Static )
					{
						ragBody.Component.PhysicsBody.BodyType = PhysicsBodyType.Dynamic;
					}
				}

				continue;
			}

			var body = rigidbody.IsValid() ? weldContexts[i]?.GetComponent<Rigidbody>()?.PhysicsBody : null;


			if ( !body.IsValid() ) continue;

			if ( body.BodyType == PhysicsBodyType.Static )
			{
				body.BodyType = PhysicsBodyType.Dynamic;
			}
		}
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
			{
				jointObject = joint.Body.Root;
			}

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

	private bool TryStartGrab()
	{
		(var isValid, var tr) = Grab();

		if ( !isValid )
			return false;

		var rootEnt = tr.GameObject;
		GrabbedObject = rootEnt;

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
			.WithoutTags("movement")
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Run();

		bool valid =
			tr.Hit &&
			(tr.GameObject.Components.Get<Rigidbody>().IsValid() || tr.GameObject.Components.Get<ModelPhysics>().IsValid() || tr.GameObject.GetComponent<PropHelper>().IsValid()) &&
			tr.GameObject.IsValid() &&
			tr.Component is not MapCollider &&
			!tr.StartedSolid &&
			!tr.Tags.Contains( "grabbed" );

		return (valid, tr);
	}

	[Rpc.Broadcast]
	private void BroadcastAttack()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}

	[Rpc.Broadcast]
	private void TryEndGrab()
	{
		if ( HeldBody.IsValid() )
			HeldBody.Locking = new PhysicsLock();
		GrabbedObject = null;
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

	[Rpc.Broadcast]
	public void Freeze( GameObject gameObject )
	{
		if ( gameObject.IsProxy )
			return;

		if ( !gameObject.IsValid() )
			return;

		if ( gameObject.Components.TryGet<PropHelper>( out var ph ) && !ph.CanFreeze )
			return;

		var body = GetBody( gameObject );

		var renderer = gameObject.GetComponent<ModelRenderer>();

		if (!gameObject.Tags.Contains("frozen"))
			gameObject.Tags.Add( "frozen" );

		if ( body.Components.TryGet<Prop>( out var prop ) )
			prop.IsStatic = true;
		else if ( body.IsValid() )
			body.MotionEnabled = false;
		else
			return;

		if ( renderer.IsValid() && renderer is not SkinnedModelRenderer && body.MotionEnabled == false )
			Scene.NavMesh.GenerateTiles( Scene.PhysicsWorld, renderer.Bounds.Grow( renderer.Bounds.Size.Length ) );

		ResetJoints( gameObject );

		FreezeEffects();
	}

	[Rpc.Broadcast]
	public void UnFreeze( GameObject gameObject )
	{
		if ( gameObject.IsProxy )
			return;

		if ( !gameObject.IsValid() )
			return;

		if ( gameObject.Components.TryGet<PropHelper>( out var ph ) && !ph.CanFreeze )
			return;

		var body = GetBody( gameObject );

		gameObject.Tags.Remove( "frozen" );

		var renderer = gameObject.GetComponent<ModelRenderer>();

		if ( gameObject.Components.TryGet<Prop>( out var prop ) )
			prop.IsStatic = false;
		else if (body.IsValid())
			body.MotionEnabled = true;
		else 
			return;

		if ( renderer.IsValid() && renderer is not SkinnedModelRenderer )
			Scene.NavMesh.GenerateTiles( Scene.PhysicsWorld, renderer.Bounds.Grow( renderer.Bounds.Size.Length ) );

		ResetJoints( gameObject );
	}

	public void ResetJoints(GameObject gameObject)
	{
		foreach ( var jointPoint in gameObject.Components.GetAll<JointPoint>( FindMode.EverythingInChildren ) )
		{
			foreach ( var joint in jointPoint.Components.GetAll<Joint>() )
			{
				joint.Enabled = false;
				joint.Enabled = true;
			}

			foreach ( var joint in jointPoint.otherPoint?.Components.GetAll<Joint>() )
			{
				joint.Enabled = false;
				joint.Enabled = true;
			}
		}
	}
}
