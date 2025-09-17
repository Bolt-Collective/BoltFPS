namespace Seekers;

[Library( "tool_winch", Title = "Winch", Description = "Creates a winch between two objects" )]
[Group( "constraints" )]
public class Winch : BaseJointTool
{
	[Property]
	public InputBind Extend { get; set; } = new( "uparrow", true );

	[Property]
	public InputBind Detract { get; set; } = new( "downarrow", true );

	[Range( 0, 500 )]
	[Property, Sync]
	public float Speed { get; set; } = 250f;

	[Range( 0, 1000 )]
	[Property, Sync]
	public float MaxLength { get; set; } = 250f;

	[Sync]
	public BroadcastBind ExtendSync { get; set; }

	[Sync]
	public BroadcastBind DetractSync { get; set; }

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( IsProxy )
			return;

		ExtendSync = Extend.GetBroadcast();
		DetractSync = Detract.GetBroadcast();
	}

	[Rpc.Broadcast]
	public override void Disconnect( GameObject target )
	{
		if ( target.IsProxy )
			return;

		if ( !target.Components.TryGet( out PropHelper propHelper ) )
			return;

		foreach ( var joint in new List<Joint>( propHelper.Joints ) )
		{
			if ( joint.IsValid() && joint.Tags.Contains( "winch" ) )
			{
				propHelper.Joints.Remove( joint );
				joint.Destroy();
			}
		}

	}

	[Rpc.Broadcast]
	public override void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		if ( selection1.GameObject.IsProxy || selection2.GameObject.IsProxy )
			return;

		var (point1, point2) = GetJointPoints( selection1, selection2 );
		var rope = CreateRopeBetween( point1, point2, MaxLength );

		var winchEntity = point1.Components.Create<WinchEntity>();
		winchEntity.ExtendBind = new InputBind( ExtendSync );
		winchEntity.DetractBind = new InputBind( DetractSync );
		winchEntity.MaxLength = MaxLength;
		winchEntity.Speed = Speed;
		winchEntity.Joint = rope.joint;
		winchEntity.Rope = rope.rope;
		winchEntity.EntityOwner = Network.OwnerId;

		var propHelper1 = point1.Components.Get<PropHelper>( FindMode.EverythingInSelfAndAncestors );
		var propHelper2 = point2.Components.Get<PropHelper>( FindMode.EverythingInSelfAndAncestors );

		propHelper1?.Joints.Add( rope.joint );
		propHelper2?.Joints.Add( rope.joint );

		rope.joint.Tags.Add( "winch" );

		point1.Network.AssignOwnership( Connection.Host );
		point1.NetworkSpawn();

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects("Undone Winch", point1, point2, rope.rope?.Attachment ?? null, rope.rope.GameObject);
		}, prop: point1 );
	}

	public static (SpringJoint joint, VerletRope rope) CreateRopeBetween( GameObject point1, GameObject point2, float maxLength = 0 )
	{
		var propHelper1 = point1.Components.Get<PropHelper>(FindMode.EverythingInSelfAndAncestors);
		var propHelper2 = point2.Components.Get<PropHelper>( FindMode.EverythingInSelfAndAncestors );

		var joint = point1.AddComponent<SpringJoint>();
		joint.Body = point2;
		joint.MaxLength = maxLength;
		joint.RestLength = maxLength;
		joint.Frequency = 0;
		joint.Damping = 0;
		joint.EnableCollision = true;

		propHelper1?.Joints.Add( joint );
		propHelper2?.Joints.Add( joint );

		return (joint,VerletRope.AddRope( point1, point2, maxLength ));
	}
}

