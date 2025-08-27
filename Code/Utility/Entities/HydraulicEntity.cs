using Sandbox;
using Sandbox.Services;
using Sandbox.UI;
using Seekers;

public sealed class HydraulicEntity : OwnedEntity
{
	[Property, Sync]
	public float Speed { get; set; }

	[Property]
	public SliderJoint Joint { get; set; }

	[Property, Sync]
	public InputBind ExtendBind { get; set; }

	[Property, Sync]
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

		if ( TargetLength == 0 )
			TargetLength = Vector3.DistanceBetween( Joint.WorldPosition, Joint.Body.WorldPosition );
	}

	protected override void OnFixedUpdate()
	{
		Joint.MinLength = TargetLength;
		Joint.MaxLength = TargetLength;
		base.OnFixedUpdate();
	}

	[Rpc.Owner]
	public void ChangeLength( float value )
	{
		TargetLength += value;
		var min = MinLength == 0 ? 0.01f : MinLength;
		TargetLength = TargetLength.Clamp( min, MaxLength );
	}

	public override void OwnerUpdate()
	{
		if ( ExtendBind.Down() )
			ChangeLength( Speed * Time.Delta );

		if ( DetractBind.Down() )
			ChangeLength( -Speed * Time.Delta );
	}
}
