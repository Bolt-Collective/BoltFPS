using Sandbox;
using Seekers;

public sealed class SitEntity : Component, Component.IPressable
{
	public bool Press (IPressable.Event e)
	{
		var pawn = Pawn.Local;
		if ( !pawn.IsValid() || !pawn.Controller.IsValid() )
			return false;

		pawn.Controller.Sit( this );

		return true;
	}

	[Rpc.Host]
	public void Claim(Movement owner)
	{
		Owner = owner;
	}

	[Sync(SyncFlags.FromHost)]
	public Movement Owner { get; set; }

	[Property]
	public Vector3 SeatPosition { get; set; }

	[Property]
	public Angles SeatRotation { get; set; }

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected )
			return;

		var dir = Vector3.Forward * SeatRotation;

		Gizmo.Draw.SolidSphere( SeatPosition, 1 );
		Gizmo.Draw.Arrow( SeatPosition, SeatPosition + dir * 10, 2, 1 );
	}
}
