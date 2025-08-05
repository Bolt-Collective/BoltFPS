using Sandbox;
using System;

public abstract partial class Movement : Component, Component.ICollisionListener
{
	[Property]
	public float physicsMass { get; set; } = 200;
	void ICollisionListener.OnCollisionStart( Collision collision )
	{
		return;
		// not really needed
		if ( !collision.Other.Body.IsValid() )
			return;

		var center = BoundingBox.Center + WorldPosition;
		var collisionNormal = (center - collision.Contact.Point).Normal;

		collision.Other.Body.ApplyForceAt( collision.Contact.Point, Velocity.Length * collisionNormal * 10000);
	}

	private void SimulateWeight()
	{
		if ( !GroundObject.IsValid() )
			return;

		PhysicsBody body = null;

		if ( GroundObject.Root.Components.TryGet<Rigidbody>( out var rb ) )
			body = rb.PhysicsBody;

		if ( GroundObject.Root.Components.TryGet<ModelPhysics>( out var mp ) )
			body = mp.PhysicsGroup.GetBody( 0 );

		if ( body.IsValid() )
			body.ApplyForceAt( WorldPosition, Scene.PhysicsWorld.Gravity * physicsMass * Time.Delta * 100 );
	}
}
