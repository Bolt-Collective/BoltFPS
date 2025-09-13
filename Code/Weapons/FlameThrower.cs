namespace Seekers;

[Spawnable, Library( "weapon_pistol" )]
partial class FlameThrower : BaseWeapon, Component.ICollisionListener
{
	public override bool CanReload() => false;
	public override bool UseProjectile => true;

	[Property, Feature( "Sounds" )]
	public SoundPointComponent GasSoundPoint { get; set; }

	[Property, Feature( "Firing" )]
	public float RegenDelay { get; set; } = 1f;

	[Property, Feature( "Firing" )]
	public float RegenRate { get; set; } = 15f;

	[Sync]
	public bool GasOn { get; set; }

	TimeUntil nextRegen;
	protected override void OnUpdate()
	{
		var target = GasOn ? 1f : 0f;

		var vel = 0f;

		GasSoundPoint.Volume = MathX.SmoothDamp( GasSoundPoint.Volume, target, ref vel, 0.1f, Time.Delta );

		base.OnUpdate();

		if ( IsProxy )
			return;
		if ( !Owner.IsValid() )
			return;

		if ( ViewModel.IsValid() )
		{
			ViewModel.Set( "trigger_press", target );
		}


		if ( TimeSincePrimaryAttack > RegenDelay && Ammo < MaxAmmo && nextRegen <= 0)
		{
			Ammo++;
			nextRegen = 1 / RegenRate;
		}

		GasOn = Input.Down( "attack1" ) && Ammo > 0;
	}

	public override void AttackPrimary()
	{
		Ammo--;

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		BroadcastAttackPrimary();

		ShootEffects();
		ShootBullet( 1.5f, Damage, 3.0f );
	}

	protected override void ShootEffects()
	{
		
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{

	}
}
