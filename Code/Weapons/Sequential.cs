namespace Seekers;

[Spawnable, Library( "weapon_shotgun" )]
partial class SequentialGun : BaseWeapon
{
	[Feature( "Firing" ), Property] public float MinRecoil { get; set; } = 2f;
	[Feature( "Firing" ), Property] public float MaxRecoil { get; set; } = 3f;
	[Feature( "Firing" ), Property] public float LastBulletRate { get; set; } = 1f;
	[Feature( "Firing" ), Property] public override float Damage { get; set; } = 4f;
	[Feature( "Firing" ), Property] public override float Spread { get; set; } = 0.1f;
	[Feature( "Firing" ), Property] public int Bullets { get; set; } = 27;
	[Feature( "Ammo" ), Property] public int ReloadCost { get; set; } = 0;
	[Feature( "Sounds" ), Property] public SoundEvent LoadSound { get; set; }
	[Feature( "Ammo" ), Property] public bool FirstShell { get; set; } = true;
	[Feature( "Ammo" ), Property] public float ReloadTransitionTime { get; set; } = 0.67f;

	bool stopReload = false;
	bool emptyReload = false;

	public override bool CanPrimaryAttack()
	{
		if ( Owner == null || !Input.Pressed( "attack1" ) ) return false;

		if ( IsReloading )
		{
			if (Ammo > 0)
			{
				stopReload = true;
				FinishReload();
			}
			return false;
		}

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public override void AttackPrimary()
	{
		base.AttackPrimary();

		Ammo--;

		TimeSincePrimaryAttack = Ammo <= 0 ? 1 / (PrimaryRate - LastBulletRate) : 0;
		TimeSinceSecondaryAttack = 0;
		

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
		ShootBullets( Bullets, 10.0f, Damage, 0.25f );
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
		AttachParticleSystem( _eject, "eject", 1, LocalWorldModel?.GameObject );
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
		ViewModel?.Set( "speed_reload", (1 / ReloadTime) * 1.7f );
		SoundExtensions.FollowSound( ReloadSound, GameObject );

		if ( Ammo <= 0 && FirstShell )
			ViewModel?.Set( "b_reloading_first_shell", true );

	}

	public override void Reload()
	{
		ViewModel?.Set( "b_reloading", true );

		Ammo -= ReloadCost;
		Ammo = Ammo.Clamp( 0, int.MaxValue );

		BroadcastReload();
		StartReloadEffects();

		if ( IsReloading )
		{
			return;
		}

		TimeSinceReload = -ReloadTransitionTime;
		IsReloading = true;
	}

	public override void OnReloadFinish()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( Ammo < MaxAmmo && !stopReload && GetReserveAmmo() > 0)
		{
			if ( Ammo > 0 || !FirstShell )
			{
				if ( LoadSound.IsValid() )
					SoundExtensions.FollowSound( LoadSound, GameObject );
				ViewModel?.Set( "b_reloading_shell", true );
			}

			Ammo++;
			TakeReserveAmmo( 1 );
			TimeSinceReload = 0;
			IsReloading = true;
		}
		else
			FinishReloadSequence();
	}

	protected virtual void FinishReloadSequence()
	{
		stopReload = false;
		IsReloading = false;
		ViewModel?.Set( "b_reloading", false );
		ViewModel?.Set( "reload_finished", true );
	}

	public override string StatDamage => $"{Damage}x10";
	public override float StatDPS => MathF.Round( (Damage * 10 * MaxAmmo) / ((MaxAmmo / PrimaryRate) + ReloadTime) );
}
