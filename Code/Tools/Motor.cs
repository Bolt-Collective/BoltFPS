namespace Seekers;

[Library( "tool_motor", Title = "Motor", Description = "Creates a rotational motor between two objects" )]
[Group( "constraints" )]
public class Motor : BaseJointTool
{
	[Property]
	public InputBind Forwards { get; set; } = new( "uparrow", true );

	[Property]
	public InputBind Backwards { get; set; } = new( "downarrow", true );

	[Property, Range( -1000, 1000 ), Sync]
	public float Speed { get; set; } = 250;

	[Property, Range( -180, 180 ), Sync]
	public float MinRotation { get; set; }

	[Property, Range( -180, 180 ), Sync]
	public float MaxRotation { get; set; }
	[Sync] public int currentAxisRotation { get; set; }
	static List<Angles> hingeAxisRotations = new List<Angles>
	{
		new Angles(90,0,0),
		new Angles(0,90,0),
		new Angles(90,90,0)
	};

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{

	}

	[Sync]
	public BroadcastBind ForwardSync { get; set; }

	[Sync]
	public BroadcastBind BackwardSync { get; set; }

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy )
			return;

		ForwardSync = Forwards.GetBroadcast();
		BackwardSync = Backwards.GetBroadcast();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		var trace = Parent.BasicTraceTool();

		Gizmo.Draw.IgnoreDepth = true;

		if ( !selected.Active )
		{
			Gizmo.Draw.Color = Color.Green;
			var normal = (Rotation.LookAt( trace.Normal ) * hingeAxisRotations[currentAxisRotation] * new Angles( -90, 0, 0 )).Forward;
			Gizmo.Draw.Line( trace.HitPosition, trace.HitPosition + normal * 5 );
			return;
		}

		if ( MinRotation == 0 && MaxRotation == 0 )
			return;
			

		Gizmo.Draw.Color = Color.Blue;
		var minNormal = (Rotation.LookAt( trace.Normal ) * hingeAxisRotations[currentAxisRotation] * new Angles(0,MinRotation - 90,0)).Forward;
		Gizmo.Draw.Line( trace.HitPosition, trace.HitPosition + minNormal * 2.5f );

		Gizmo.Draw.Color = Color.Red;
		var maxNormal = (Rotation.LookAt( trace.Normal ) * hingeAxisRotations[currentAxisRotation] * new Angles( 0, MaxRotation - 90, 0 )).Forward;
		Gizmo.Draw.Line( trace.HitPosition, trace.HitPosition + maxNormal * 5 );
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( !Input.Pressed( "reload" ) )
			return false;

		currentAxisRotation++;
		if ( currentAxisRotation >= hingeAxisRotations.Count() )
			currentAxisRotation = 0;
		return false;
	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
			return;

		PropHelper propHelper1 = selection1.GameObject.Root.Components.Get<PropHelper>();
		PropHelper propHelper2 = selection2.GameObject.Root.Components.Get<PropHelper>();

		(GameObject point1, GameObject point2) = GetJointPoints( selection1, selection2 );

		var hingeJoint = point2.Components.Create<HingeJoint>();
		hingeJoint.Body = point1;

		hingeJoint.Motor = HingeJoint.MotorMode.TargetVelocity;

		hingeJoint.MinAngle = MinRotation;
		hingeJoint.MaxAngle = MaxRotation;

		point1.WorldRotation = Rotation.LookAt( selection1.WorldNormal ) * hingeAxisRotations[currentAxisRotation];
		point2.WorldRotation = Rotation.LookAt( -selection2.WorldNormal ) * hingeAxisRotations[currentAxisRotation];

		propHelper1?.Joints.Add( hingeJoint );
		propHelper2?.Joints.Add( hingeJoint );

		var motorEntity = point1.Components.Create<MotorEntity>();
		motorEntity.ForwardBind = new InputBind( ForwardSync );
		motorEntity.BackwardBind = new InputBind( BackwardSync );
		motorEntity.Speed = Speed;
		motorEntity.HingeJoint = hingeJoint;
		motorEntity.EntityOwner = Network.OwnerId;

		point1.Network.AssignOwnership( Connection.Host );
		point1.NetworkSpawn();

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Hinge", point1, point2 );
		}, prop: point1 );
	}

	public static HingeJoint HingeObjects( GameObject body1, GameObject body2, Vector3 pos1, Vector3 pos2, Vector3 normal, Angles rotation )
	{
		var selection1 = new SelectionPoint( body1, pos1, body1.WorldTransform.WithPosition( 0 ).PointToLocal( normal ) );
		var selection2 = new SelectionPoint( body2, pos2, body2.WorldTransform.WithPosition( 0 ).PointToLocal( -normal ) );

		PropHelper propHelper1 = body1.Root.Components.Get<PropHelper>();
		PropHelper propHelper2 = body2.Root.Components.Get<PropHelper>();

		(GameObject point1, GameObject point2) = GetJointPoints( selection1, selection2 );

		var hingeJoint = point2.Components.Create<HingeJoint>();
		hingeJoint.Body = point1;

		point1.WorldRotation = Rotation.LookAt( selection1.WorldNormal ) * rotation;
		point2.WorldRotation = Rotation.LookAt( -selection2.WorldNormal ) * rotation;

		propHelper1?.Joints.Add( hingeJoint );
		propHelper2?.Joints.Add( hingeJoint );

		return hingeJoint;
	}
}
