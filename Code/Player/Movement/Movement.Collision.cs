using Sandbox;
using Sandbox.VR;
using Seekers;
using System;

public abstract partial class Movement : Component, ISceneCollisionEvents
{

	void ISceneCollisionEvents.OnCollisionStart( Collision collision )
	{
		if ( !Networking.IsHost )
			return;
		if ( collision.Self.GameObject != ShadowCollider.GameObject && collision.Other.GameObject != ShadowCollider.GameObject )
			return;

		float objectMass = collision.Self.GameObject == ShadowCollider.GameObject ? collision.Other.Body.Mass : collision.Self.Body.Mass;
		var objectVelocity = collision.Self.GameObject == ShadowCollider.GameObject ? collision.Other.Body.Velocity : collision.Self.Body.Velocity;
		var normal = collision.Contact.Normal;
		
		var speed = collision.Contact.NormalSpeed;

		var toPlayerNormal = collision.Contact.Point - WorldPosition.WithZ( collision.Contact.Point.z );

		if ( Vector3.GetAngle( normal, toPlayerNormal ) <= 90 )
		{
			normal = -normal;
		}
		
		var velocityDot = normal.Dot( objectVelocity );

		//DebugOverlay.Line(WorldPosition, WorldPosition)

		Slow( normal * objectMass * 0.0005f );

		//if ( velocityDot < 0)
		//	Punch( normal * velocityDot * objectMass * 0.01f );

		
	}

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
		var healthComponent = shadowCollider.Components.Create<HealthComponent>();
		healthComponent.LinkedHealth = GetComponent<HealthComponent>();

		shadowCollider.Tags.Add( "movement" );

		return shadowCollider;
	}

	public void SetCollisionBox()
	{
		if ( !Networking.IsHost )
			return;

		ShadowCollider.Enabled = !CurrentSeat.IsValid();

		ShadowCollider.WorldPosition = WorldPosition;
		ShadowCollider.WorldRotation = WorldRotation;

		var velocity = Velocity.LerpTo( WishVelocity, 0.2f );
		var speed = velocity.Length;
		var direction = velocity.Normal;

		if ( speed <= 0.001f )
			direction = Vector3.Zero;

		var baseScale = new Vector3( Radius * 2.1f, Radius * 2.1f, Height );

		var stretchAmount = speed * 0.05f;
		var stretch = direction * stretchAmount;

		var scale = baseScale + new Vector3(
			MathF.Abs( stretch.x ),
			MathF.Abs( stretch.y ),
			0
		);

		ShadowCollider.Scale = scale;

		var centerOffset = new Vector3(
			direction.x * (stretchAmount / 2),
			direction.y * (stretchAmount / 2),
			(Height / 2f) + 2
		);

		ShadowCollider.Center = centerOffset;
	}

	

	protected override void OnDestroy()
	{
		ShadowCollider?.GameObject?.Destroy();
	}
}
