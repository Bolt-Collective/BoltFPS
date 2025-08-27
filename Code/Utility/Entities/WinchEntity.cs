using Sandbox;
using Sandbox.Services;
using Sandbox.UI;
using Seekers;

public sealed class WinchEntity : OwnedEntity
{
	[Property, Sync]
	public float Speed { get; set; }

	[Property]
	public SpringJoint Joint { get; set; }

	[Property]
	public VerletRope Rope { get; set; }

	[Property]
	public InputBind ExtendBind { get; set; }

	[Property]
	public InputBind DetractBind { get; set; }

	[Property, Sync]
	public float TargetLength { get; set; }

	[Property, Sync]
	public float MinLength { get; set; }

	[Property, Sync]
	public float MaxLength { get; set; }

	protected override void OnStart()
	{
		base.OnStart();

		if ( IsProxy )
			return;

		var min = MinLength == 0 ? 5f : MinLength; 
		var max = MaxLength == 0 ? 5f : MaxLength;

		if ( TargetLength == 0 )
			TargetLength = Vector3.DistanceBetween( Joint.WorldPosition, Joint.Body.WorldPosition ).Clamp( min, max );
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		Rope.SegmentLength = TargetLength / Rope.SegmentCount;

		if ( IsProxy )
			return;

		Joint.MaxLength = TargetLength;
		Joint.RestLength = TargetLength;
	}

	[Rpc.Owner]
	public void ChangeLength( float value )
	{
		TargetLength += value;
		var min = MinLength == 0 ? 0.01f : MinLength;
		var max = MaxLength == 0 ? 0.01f : MaxLength;
		TargetLength = TargetLength.Clamp( min, max );
	}

	public override void OwnerUpdate()
	{
		if ( ExtendBind.Down() )
			ChangeLength( Speed * Time.Delta );

		if ( DetractBind.Down() )
			ChangeLength( -Speed * Time.Delta );
	}
}
