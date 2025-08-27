using Sandbox;
using Sandbox.Services;
using Sandbox.VR;
using System;
using static Sandbox.ModelPhysics;

public abstract partial class Movement : Component
{
	[Property, Sync(SyncFlags.FromHost)] public SitEntity CurrentSeat { get; set; }
	[Property] public Vector3 SeatOffset { get; set; } = new Vector3( 0, 0, -15f );

	public void SeatMovement()
	{
		IsGrounded = true;
		Velocity = 0;
		WorldPosition = CurrentSeat.WorldTransform.PointToWorld( CurrentSeat.SeatPosition + SeatOffset );
		if (Input.Pressed("jump") || Input.Pressed("duck") || CurrentSeat.Owner != this)
		{
			WorldPosition = CurrentSeat.WorldTransform.PointToWorld( CurrentSeat.SeatPosition + BodyModelRenderer.WorldTransform.Up * 5);
			CurrentSeat = null;
		}
	}

	[Rpc.Host]
	public void Sit(SitEntity sitEntity)
	{
		if ( CurrentSeat?.Owner.IsValid() ?? false )
			return;

		CurrentSeat = sitEntity;
		sitEntity.Claim( this );
	}
}
