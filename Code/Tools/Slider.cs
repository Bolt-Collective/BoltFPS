using Sandbox;

namespace Seekers;

[Library( "tool_slider", Title = "Slider", Description = "Create a sliding joint between 2 objects." )]
[Group( "constraints" )]
public class Slider : BaseJointTool
{
	[Property, Sync]
	public bool Collision { get; set; } = true;

	[Property, Range( -500, 500 ), Sync]
	public float MinLength { get; set; }

	[Property, Range( -500, 500 ), Sync]
	public float MaxLength { get; set; }

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{

	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
			return;

		PropHelper propHelper1 = selection1.GameObject.Root.Components.Get<PropHelper>();
		PropHelper propHelper2 = selection2.GameObject.Root.Components.Get<PropHelper>();


		(GameObject point1, GameObject point2) = GetJointPoints( selection1, selection2 );

		var dir = (point1.WorldPosition - point2.WorldPosition).Normal;

		point1.WorldRotation = Rotation.LookAt( dir );
		point2.WorldRotation = Rotation.LookAt( dir );

		var sliderJoint = point2.Components.Create<SliderJoint>();
		sliderJoint.Body = point1;
		sliderJoint.EnableCollision = Collision;
		sliderJoint.MinLength = MinLength;
		sliderJoint.MaxLength = MaxLength;

		propHelper1?.Joints.Add( sliderJoint );
		propHelper2?.Joints.Add( sliderJoint );

		var linePoint1 = new GameObject( parent: point1.Parent );
		linePoint1.LocalPosition = point1.LocalPosition;
		linePoint1.LocalRotation = point1.LocalRotation;

		var linePoint2 = new GameObject( parent: point2.Parent );
		linePoint2.LocalPosition = point2.LocalPosition;
		linePoint2.LocalRotation = point2.LocalRotation;

		var lineRenderer = linePoint1.Components.Create<LineRenderer>();
		lineRenderer.Width = 1f;
		lineRenderer.Color = Color.White;
		lineRenderer.Lighting = true;
		lineRenderer.CastShadows = true;
		lineRenderer.Face = SceneLineObject.FaceMode.Cylinder;
		lineRenderer.Points = [linePoint1, linePoint2];

		linePoint2.Network.AssignOwnership( Connection.Host );
		linePoint2.NetworkSpawn();
		linePoint1.Network.AssignOwnership( Connection.Host );
		linePoint1.NetworkSpawn();

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Slider", point1, point2, linePoint1, linePoint2 );
		}, prop: point1 );
	}
}

