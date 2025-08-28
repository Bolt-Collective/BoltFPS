using Sandbox;
using Sandbox.UI;
using Seekers;

public sealed class HoverballEntity : OwnedEntity
{
	[Property, Sync] public float Strength { get; set; } = 1;

	[Property, Sync] public float Speed { get; set; } = 10;

	[Property]
	public InputBind UpBind { get; set; }

	[Property]
	public InputBind DownBind { get; set; }

	[Property, Sync]
	public GameObject ConnectedObject { get; set; }

	[Property, Sync]
	public Vector3 LocalPos { get; set; }

	[Property, Sync]
	public float HeightOffset { get; set; }


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

	float targetHeight;

	protected override void OnStart()
	{
		if (ConnectedObject.IsValid())
		{
			targetHeight = ConnectedObject.WorldPosition.z;
			return;
		}
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

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( !ConnectedObject.IsValid() )
			return;

		if ( !Components.TryGet<ModelRenderer>( out var modelRenderer ) )
			return;

		modelRenderer.SceneObject.Position = ConnectedObject.WorldTransform.PointToWorld( LocalPos );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( !GameObject.Parent.IsValid() )
			return;

		if ( !rigidbody.IsValid() )
			return;

		var height = targetHeight;

		if ( ConnectedObject.IsValid() )
			height += HeightOffset;

		var prevVelocity = rigidbody.Velocity;

		rigidbody.SmoothMove( WorldPosition.WithZ(height), 1f.LerpTo( 0.01f, Strength ), Time.Delta );

		rigidbody.Velocity = prevVelocity.WithZ(rigidbody.Velocity.z);
	}


}
