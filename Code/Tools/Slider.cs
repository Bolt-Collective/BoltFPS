namespace Seekers;

[Library( "tool_slider", Title = "Slider", Description = "Create a sliding joint between 2 objects.", Group = "constraints" )]
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

		point1.LocalRotation = Rotation.LookAt( -selection1.LocalNormal );

		point2.WorldRotation = point1.WorldRotation;
		var sliderJoint = point2.Components.Create<SliderJoint>();
		sliderJoint.Body = point1;
		sliderJoint.EnableCollision = Collision;
		sliderJoint.MinLength = MinLength;
		sliderJoint.MaxLength = MaxLength;

		propHelper1?.Joints.Add( sliderJoint );
		propHelper2?.Joints.Add( sliderJoint );

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Slider", point1, point2 );
		}, prop: point1 );
	}
}

