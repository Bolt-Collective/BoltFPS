namespace Seekers;

[Spawnable, Library( "weapon_stim" )]
partial class Stim : BaseWeapon, Component.ICollisionListener
{
	[Feature( "Firing" ), Property] private float Heal { get; set; }
	[Feature( "Firing" ), Property] private float Delay { get; set; } = 0.2f;

	public override bool CanReload() => false;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy || !GameObject.Enabled || !Enabled )
			return;

		if ( ViewModel.IsValid() )
			ViewModel.GameObject.Enabled = Ammo > 0;
	}

	public override async void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		ViewModel?.Set( "b_attack", true );

		await Task.DelaySeconds( Delay );

		BroadcastAttackPrimary();

		if ( Owner?.HealthComponent.IsValid() ?? false )
			Owner.HealthComponent.Health = (Owner.HealthComponent.Health + Heal).Clamp(-float.MaxValue, Owner.HealthComponent.MaxHealth);

		Ammo--;
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}
}
