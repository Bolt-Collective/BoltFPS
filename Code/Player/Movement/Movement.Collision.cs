using Sandbox;
using Sandbox.VR;
using Seekers;
using System;

public abstract partial class Movement : Component
{
	private BoxCollider _shadowCollider;
	public BoxCollider ShadowCollider
	{ 
		get
		{
			if(!_shadowCollider.IsValid())
			{
				_shadowCollider = CreateShadowCollider();
			}
			return _shadowCollider;
		}
	}

	public BoxCollider CreateShadowCollider()
	{
		if ( !Networking.IsHost )
			return null ;

		var shadowCollider = new GameObject( GameObject.Name.Split( "-" )[0] + "- COLLIDER" ).AddComponent<BoxCollider>();
		shadowCollider.WorldPosition = WorldPosition;

		shadowCollider.Tags.Add( "movement" );

		return shadowCollider;
	}

	public void SetCollisionBox()
	{
		if ( !Networking.IsHost )
			return;

		ShadowCollider.WorldPosition = WorldPosition;
		ShadowCollider.WorldRotation = WorldRotation;

		var velocity = Velocity.LerpTo( WishVelocity, 0.2f );
		var speed = velocity.Length;
		var direction = velocity.Normal;

		if ( speed <= 0.001f )
			direction = Vector3.Zero;

		var baseScale = new Vector3( Radius * 2f, Radius * 2f, Height );

		var stretchAmount = speed * 0.05f;
		var stretch = direction * stretchAmount;

		var scale = baseScale + new Vector3(
			MathF.Abs( stretch.x ),
			MathF.Abs( stretch.y ),
			MathF.Abs( stretch.z )
		);

		ShadowCollider.Scale = scale;

		var centerOffset = new Vector3(
			direction.x * (stretchAmount / 2),
			direction.y * (stretchAmount / 2),
			(Height / 2f) + 2 + direction.z * (stretchAmount / 2)
		);

		ShadowCollider.Center = centerOffset;
	}

	protected override void OnDestroy()
	{
		ShadowCollider?.GameObject?.Destroy();
	}
}
