namespace Seekers;
public class Weld : BaseJointTool
{

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

		var fixedJoint = point2.Components.Create<FixedJoint>();
		fixedJoint.Body = point1;
		fixedJoint.LinearDamping = 0;
		fixedJoint.LinearFrequency = 0;
		fixedJoint.AngularDamping = 0;
		fixedJoint.AngularFrequency = 0;

		propHelper1?.Joints.Add( fixedJoint );
		propHelper2?.Joints.Add( fixedJoint );

		UndoSystem.Add( creator: Network.Owner.SteamId, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Weld", point1, point2 );
		}, prop: point1 );
	}
}
