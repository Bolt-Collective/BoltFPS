using Sandbox;
using Sandbox.Services;
using Sandbox.VR;
using System;
using static Sandbox.ModelPhysics;

public abstract partial class Movement : Component
{
	[Property, Sync] public SitEntity CurrentSeat { get; set; }
	[Property] public Vector3 SeatOffset { get; set; } = new Vector3( 0, 0, -15f );

	public void SeatMovement()
	{
		WorldPosition = CurrentSeat.WorldTransform.PointToWorld( CurrentSeat.SeatPosition + SeatOffset );
		if (Input.Pressed("jump") || Input.Pressed("duck"))
		{
			WorldPosition = CurrentSeat.WorldTransform.PointToWorld( CurrentSeat.SeatPosition + BodyModelRenderer.WorldTransform.Up * 5);
			CurrentSeat = null;
		}
	}

	[Rpc.Owner]
	public void Sit(SitEntity sitEntity)
	{
		CurrentSeat = sitEntity;
	}
}
