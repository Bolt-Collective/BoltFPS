namespace Seekers;

[Spawnable, Library( "weapon_shotgun" )]
partial class Shotgun : BaseWeapon
{
	[Property] public float MinRecoil { get; set; } = 2f;
	[Property] public float MaxRecoil { get; set; } = 3f;
	[Property] public override float Damage { get; set; } = 4f;
	[Property] public override float Spread { get; set; } = 0.1f;
	[Property] public int Bullets { get; set; } = 27;
	[Property] public SoundEvent LoadSound { get; set; }

	public override bool CanPrimaryAttack()
	{
		if ( Owner == null || !Input.Pressed( "attack1" ) ) return false;

		if ( IsReloading )
			FinishReload();

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public override void AttackPrimary()
	{
		base.AttackPrimary();

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;
		Ammo--;
		FinishReloadSequence();

		CalculateRandomRecoil( (MinRecoil, MaxRecoil), (MinRecoil / 2, MaxRecoil / 2) );

		BroadcastAttackPrimary();

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();

		//
		// Shoot the bullets
		//
		ShootBullets( Bullets, 10.0f, Damage, 3.0f );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}

	// TODO: Probably should unify these particle methods + make it work for world models

	GameObject _eject = GameObject.GetPrefab( "weapons/common/effects/eject_shotgun.prefab" );

	protected override void ShootEffects()
	{
		base.ShootEffects();
		AttachParticleSystem( _eject, "eject", 1, LocalWorldModel.GameObject );
		ViewModel?.Set( "b_attack", true );
	}

	protected virtual void DoubleShootEffects()
	{
		Particles.CreateParticleSystem( GameObject.GetPrefab( "weapons/common/effects/muzzle.prefab" ),
			Attachment( "muzzle" ) );

		ViewModel?.Set( "fire_double", true );
	}

	[Rpc.Broadcast]
	public override async void BroadcastReload()
	{
		Owner?.Controller?.BodyModelRenderer?.Set( "b_reloading", true );
		LeftIK.Active = false;
		await Task.DelayRealtimeSeconds( ReloadTime );
		LeftIK.Active = true;
		Owner?.Controller?.BodyModelRenderer?.Set( "b_reloading", false );
	}

	public override void StartReloadEffects()
	{
		ViewModel?.Set( "b_reloading", true );
		ViewModel?.Set( "speed_reload", (1 / ReloadTime) * 1.7f );
		SoundExtensions.FollowSound( ReloadSound, GameObject );

		if ( Ammo <= 0 )
			ViewModel?.Set( "b_reloading_first_shell", true );
	}

	public override void OnReloadFinish()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( Ammo < MaxAmmo )
		{
			if ( Ammo > 0 )
			{
				SoundExtensions.FollowSound( LoadSound, GameObject );
				ViewModel?.Set( "b_reloading_shell", true );
			}

			Ammo++;
			TimeSinceReload = 0;
			IsReloading = true;
		}
		else
			FinishReloadSequence();
	}

	protected virtual void FinishReloadSequence()
	{
		IsReloading = false;
		ViewModel?.Set( "b_reloading", false );
		ViewModel?.Set( "reload_finished", true );
	}

	public override string StatDamage => $"{Damage}x10";
	public override float StatDPS => MathF.Round( (Damage * 10 * MaxAmmo) / ((MaxAmmo / PrimaryRate) + ReloadTime) );
}
