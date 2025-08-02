namespace Seekers;
public class Weld : BaseJointTool
{

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{

	}

	[Rpc.Broadcast]
	public override void Join( GameObject body1, Vector3 pos1, GameObject body2, Vector3 pos2 )
	{
		if ( body1.IsProxy || body2.IsProxy )
			return;

		PropHelper propHelper1 = body1.Components.Get<PropHelper>();
		PropHelper propHelper2 = body2.Components.Get<PropHelper>();

		(GameObject point1, GameObject point2) = GetLocalPoints( body1, pos1, body2, pos2 );

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
			point1.BroadcastDestroy();
			point2.BroadcastDestroy();
			return $"Undone Weld";
		}, prop: point1 );
	}
}
