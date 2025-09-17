using System.Diagnostics;

namespace Seekers;

[Library( "tool_snap", Title = "Snap", Description = "Snap anouther object to anouther" )]
[Group( "constraints" )]
public class Snap : BaseJointTool
{
	[Property, Sync]
	public bool weld { get; set; }

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{
		if ( target.IsProxy )
			return;

		if ( !target.Components.TryGet( out PropHelper propHelper ) )
			return;

		foreach ( var joint in propHelper.Joints )
		{
			if ( joint.IsValid() )
			{
				joint.Destroy();
			}
		}

		propHelper.Joints.Clear();
	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
			return;

		var selectionDirection = selection1.GameObject.WorldTransform.PointToWorld( selected.LocalNormal );
		var selectionPoint = selection1.GameObject.WorldTransform.PointToWorld( selected.LocalPosition );

		selection1.GameObject.WorldPosition += selection2.WorldPosition - selectionPoint;

		if ( !weld )
			return;

		PropHelper propHelper1 = selection1.GameObject.Root.Components.Get<PropHelper>();
		PropHelper propHelper2 = selection2.GameObject.Root.Components.Get<PropHelper>();

		(GameObject point1, GameObject point2) = GetJointPoints( selection1, selection2 );

		var fixedJoint = point2.Components.Create<FixedJoint>();
		fixedJoint.Body = point1;
		fixedJoint.LinearDamping = 0;
		fixedJoint.LinearFrequency = 0;
		fixedJoint.AngularDamping = 0;
		fixedJoint.AngularFrequency = 0;

		propHelper1?.Joints.Add( fixedJoint );
		propHelper2?.Joints.Add( fixedJoint );

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Weld", point1, point2 );
		}, prop: point1 );

		Weld.WeldObjects( selection1.GameObject, selection2.GameObject, selection2.WorldPosition );
	}
}
