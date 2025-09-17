namespace Seekers;

[Library( "tool_rope", Title = "Rope", Description = "Creates a rope between two objects" )]
[Group( "constraints" )]
public class Rope : BaseJointTool
{
	[Range( -500, 500 )]
	[Property, Sync]
	public float Slack { get; set; } = 0.0f;

	[Property, Sync]
	public bool Rigid { get; set; } = false;

	public override bool CanConstraintToSelf => true;

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{
		DisconnectTag( target, "rope" );
	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
			return;

		var (point1, point2) = GetJointPoints( selection1, selection2 );
		var rope = CreateRopeBetween( point1, point2, Slack, Rigid );

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects("Undone Rope", point1, point2, rope?.Attachment ?? null, rope.GameObject);
		}, prop: point1 );
	}

	public static VerletRope CreateRopeBetween( GameObject point1, GameObject point2, float slack = 0, bool rigid = false )
	{
		var propHelper1 = point1.Components.Get<PropHelper>(FindMode.EverythingInSelfAndAncestors);
		var propHelper2 = point2.Components.Get<PropHelper>( FindMode.EverythingInSelfAndAncestors );

		var len = point1.WorldPosition.Distance( point2.WorldPosition );
		len = MathF.Max( 1.0f, len + slack );

		if ( point1.Parent != point2.Parent )
		{
			var joint = point1.AddComponent<SpringJoint>();
			joint.Body = point2;
			joint.MinLength = rigid ? len : 0;
			joint.MaxLength = len;
			joint.RestLength = len;
			joint.Frequency = 0;
			joint.Damping = 0;
			joint.EnableCollision = true;

			joint.Tags.Add( "rope" );

			propHelper1?.Joints.Add( joint );
			propHelper2?.Joints.Add( joint );
		}

		return VerletRope.AddRope( point1, point2, len );
	}
}

