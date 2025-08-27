using Seekers;

public sealed class MotorEntity : OwnedEntity
{
	[Property]
	public InputBind ForwardBind { get; set; }

	[Property]
	public InputBind BackwardBind { get; set; }

	[Property]
	public HingeJoint HingeJoint { get; set; }

	[Property, Sync]
	public float Speed { get; set; }

	public float targetVelocity;
	float velocity;
	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();
		if ( IsProxy )
			return;

		velocity = velocity.LerpTo(targetVelocity, 0.4f );

		HingeJoint.TargetVelocity = velocity;
		HingeJoint.MaxTorque = float.MaxValue;
	}

	[Rpc.Owner]
	public void SetSpeed( float speed )
	{
		targetVelocity = speed;
	}


	float _lastSpeed;
	public override void OwnerUpdate()
	{
		float speed = 0;

		if ( BackwardBind.Down() )
			speed = -Speed;

		if ( ForwardBind.Down() )
			speed = Speed;

		if ( speed != _lastSpeed )
			SetSpeed( speed );

		_lastSpeed = speed;
	}
}
