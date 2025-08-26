using Sandbox;
using Seekers;

public sealed class HealthKitEntity : Component, Component.IPressable
{
	public SkinnedModelRenderer Renderer { get; set; }

	[Sync(SyncFlags.FromHost)]
	RealTimeSince lastLook { get; set; }

	[Property]
	public float Health { get; set; }

	[Property]
	public float OpenTime { get; set; } = 1;

	private bool used = false;

	public bool Press (IPressable.Event e)
	{
		HostHeal( Connection.Local.Id );
		return true;
	}

	RealTimeSince localLook;
	public void Look( IPressable.Event e )
	{
		localLook = 0;
	}

	[Rpc.Host]
	public void SetLook(float value)
	{
		lastLook = value;
	}

	TimeUntil nextLookUpdate;
	protected override void OnFixedUpdate()
	{
		if (!Renderer.IsValid())
			Renderer = GetComponent<SkinnedModelRenderer>();

		Renderer?.Set( "open", lastLook < OpenTime );

		if ( nextLookUpdate > 0 )
			return;

		nextLookUpdate = 0.2f;
		SetLook( localLook );
	}

	[Rpc.Host]
	public void HostHeal(Guid to)
	{
		if ( used )
			return;
		used = true;
		Heal( to );
		GameObject.BroadcastDestroy();
	}

	[Rpc.Broadcast]
	public void Heal(Guid to)
	{
		if ( Connection.Local.Id != to )
			return;

		var pawn = Pawn.Local;

		if ( !pawn.IsValid() || !pawn.HealthComponent.IsValid() )
			return;

		pawn.HealthComponent.AddHealh( Health );
	}
}
