public class BalloonGravity : Component
{
	Rigidbody _rigidbody;
	Rigidbody rigidbody
	{
		get
		{
			if ( !_rigidbody.IsValid() )
			{
				_rigidbody = Components.Get<Rigidbody>();
			}
			return _rigidbody;
		}
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		rigidbody.Gravity = false;

		rigidbody.ApplyForce( Scene.PhysicsWorld.Gravity * -0.2f * Time.Delta * rigidbody.Mass * 100);

		rigidbody.SmoothRotate( Angles.Zero, 0.1f, Time.Delta );
	}
}
