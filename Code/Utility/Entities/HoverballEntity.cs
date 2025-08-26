using Sandbox;
using Sandbox.UI;
using Seekers;

public sealed class HoverballEntity : OwnedEntity
{
	[Property] public float Strength { get; set; } = 1;

	[Property, Sync] public float Speed { get; set; } = 10;

	[Property]
	public InputBind UpBind { get; set; }

	[Property]
	public InputBind DownBind { get; set; }

	[Property]
	public Rotation TargetRotation { get; set; }


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

	float targetHeight;

	protected override void OnStart()
	{
		targetHeight = GameObject.WorldPosition.z;
	}

	float _lastHeight;
	public override void OwnerUpdate()
	{
		if ( UpBind.Down() )
			targetHeight += Speed * Time.Delta;

		if ( DownBind.Down() )
			targetHeight -= Speed * Time.Delta;

		if (targetHeight != _lastHeight)
			SetHeight( targetHeight );

		_lastHeight = targetHeight;
	}

	[Rpc.Host]
	public void SetHeight(float value) => targetHeight = value;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( !GameObject.Parent.IsValid() )
			return;

		if ( !rigidbody.IsValid() )
			return;

		var prevVelocity = rigidbody.Velocity;

		var childWorldPos = GameObject.WorldPosition;

		var zOffset = targetHeight - childWorldPos.z;

		var desiredParentPos = rigidbody.WorldPosition + Vector3.Up * zOffset;

		var desiredTransform = new Transform( desiredParentPos, TargetRotation.Angles().WithYaw(rigidbody.WorldRotation.Yaw()));

		rigidbody.SmoothMove( desiredTransform, 10f.LerpTo( 0.01f, Strength ), Time.Delta );

		rigidbody.Velocity = prevVelocity.WithZ( rigidbody.Velocity.z );
	}


}
