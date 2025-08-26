using Seekers;

public class WheelEntity : OwnedEntity
{
	[Property]
	public float Torque { get; set; } = 600;

	[Property]
	public InputBind ForwardBind { get; set; }

	[Property]
	public InputBind BackwardBind { get; set; }

	[Property]
	public float CurrentForce;

	Rigidbody _rigidbody;
	Rigidbody rigidbody
	{
		get
		{
			if ( !_rigidbody.IsValid() )
			{
				_rigidbody = GameObject.Components.Get<Rigidbody>();
			}
			return _rigidbody;
		}
	}

	float _lastForce;
	public override void OwnerUpdate()
	{
		float force = 0;

		if (BackwardBind.Down())
			force = -Torque;

		if ( ForwardBind.Down() )
			force = Torque;

		if ( force != _lastForce )
			SetForce( force );

		_lastForce = force;
	}

	Vector3 velocity;
	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;
		if ( CurrentForce == 0 )
			return;

		velocity = Vector3.Lerp( velocity, WorldTransform.Left * CurrentForce, 0.4f );
		rigidbody.ApplyTorque( velocity * 100000 );
		rigidbody.ApplyForce( velocity.Length * Vector3.Down * 2000 );
	}

	[Rpc.Host]
	public void SetForce(float force)
	{
		CurrentForce = force;
	}

}
