using Seekers;

public class ThrusterEntity : Component
{
	[Property]
	public float Force { get; set; }

	[Property]
	public InputBind ForwardBind { get; set; }

	[Property]
	public InputBind BackwardBind { get; set; }

	[Property]
	public Guid Owner { get; set; }

	[Property]
	public float CurrentForce;

	Rigidbody _rigidbody;
	Rigidbody rigidbody
	{
		get
		{
			if ( !_rigidbody.IsValid() )
			{
				_rigidbody = GameObject.Parent.Components.Get<Rigidbody>();
			}
			return _rigidbody;
		}
	}



	float _lastForce;
	protected override void OnUpdate()
	{
		if ( Connection.Local.Id != Owner )
			return;

		float force = 0;

		if (BackwardBind.Down())
			force = -Force;

		if ( ForwardBind.Down() )
			force = Force;

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



		velocity = Vector3.Lerp( velocity, WorldTransform.Down * CurrentForce, 0.4f );

		rigidbody.ApplyForceAt( WorldPosition, velocity * 10000);
	}

	[Rpc.Host]
	public void SetForce(float force)
	{
		CurrentForce = force;
	}

}
