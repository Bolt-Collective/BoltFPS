namespace Seekers;

[Spawnable, Library( "weapon_grenade" )]
partial class Grenade : BaseWeapon, Component.ICollisionListener
{
	[Property] private GameObject Projectile { get; set; }
	[Property] private float ThrowDelay { get; set; } = 1;
	[Property] private float ThrowForce { get; set; } = 200;
	[Property] private float DropForce { get; set; } = 100;

	public override bool CanReload() => false;

	public override bool CanSecondaryAttack() => base.CanSecondaryAttack() && TimeSincePrimaryAttack > 1/PrimaryRate && Ammo > 0;

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy || !GameObject.Enabled || !Enabled )
			return;

		if(ViewModel.IsValid())
			ViewModel.GameObject.Enabled = Ammo > 0;
	}

	public override async void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		ViewModel?.Renderer?.Set( "b_attack", true );
		BroadcastAttackPrimary();

		await Task.DelaySeconds( ThrowDelay );

		ThrowGrenade(Projectile, ThrowForce);

		Ammo--;
	}

	public override async void AttackSecondary()
	{
		TimeSincePrimaryAttack = 0;
		ViewModel?.Renderer?.Set( "b_attack", true );
		await Task.DelaySeconds( ThrowDelay );

		ThrowGrenade( Projectile, DropForce );

		Ammo--;
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}
}
