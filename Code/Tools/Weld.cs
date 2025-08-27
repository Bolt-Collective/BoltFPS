namespace Seekers;

[Library( "tool_weld", Title = "Weld", Description = "Weld two objects together" )]
[Group( "constraints" )]
public class Weld : BaseJointTool
{

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{

	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if (!selected.Active)
			return false;

		if (!Input.Pressed("attack2"))
			return false;

		var selectionDirection = selected.GameObject.WorldTransform.PointToWorld( selected.LocalNormal );
		var selectionPoint = selected.GameObject.WorldTransform.PointToWorld( selected.LocalPosition );

		selected.GameObject.WorldPosition += trace.EndPosition - selectionPoint;

		selected.GameObject.WorldTransform = selected.GameObject.WorldTransform.RotateAround( trace.HitPosition, Rotation.FromToRotation( selectionDirection, -trace.Normal ) );

		selected.Active = false;

		return true;
	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
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
	}

	public static FixedJoint WeldObjects(GameObject body1, GameObject body2, Vector3 pos1, Vector3 pos2 = default)
	{
		if (pos2 == default)
			pos2 = body2.WorldTransform.PointToLocal( body1.WorldTransform.PointToWorld( pos1 ) );

		var selection1 = new SelectionPoint(body1, pos1, Vector3.Zero);
		var selection2 = new SelectionPoint(body2, pos2, Vector3.Zero);

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

		return fixedJoint;
	}
}
