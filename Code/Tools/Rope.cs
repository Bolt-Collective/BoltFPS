namespace Seekers;
public class Rope : BaseJointTool
{
	[Range( -500, 500 )]
	[Property, Sync]
	public float Slack { get; set; } = 0.0f;

	[Property, Sync]
	public bool Rigid { get; set; } = false;

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

		var len = point1.WorldPosition.Distance( point2.WorldPosition );
		len = MathF.Max( 1.0f, len + Slack );

		if ( point1.Parent != point2.Parent )
		{
			var joint = point1.AddComponent<SpringJoint>();
			joint.Body = point2;
			joint.MinLength = Rigid ? len : 0;
			joint.MaxLength = len;
			joint.RestLength = len;
			joint.Frequency = 0;
			joint.Damping = 0;
			joint.EnableCollision = true;
			propHelper1?.Joints.Add( joint );
			propHelper2?.Joints.Add( joint );
		}
		
		var rope = VerletRope.AddRope( point1, point2, len );

		UndoSystem.Add( creator: Network.Owner.SteamId, callback: () =>
		{
			point1.BroadcastDestroy();
			point2.BroadcastDestroy();
			rope.Attachment.BroadcastDestroy();
			rope.GameObject.BroadcastDestroy();
			return $"Undone Rope";
		}, prop: point1 );
	}
}
