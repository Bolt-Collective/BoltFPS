namespace Seekers;

partial class Revolver : BaseWeapon, Component.ICollisionListener
{
	public RealTimeSince TimeSinceDischarge { get; set; }
	[Feature( "Ammo" ), Property] public float LoadTime { get; set; } = 1f;
	[Feature( "Firing" ), Property] public RecoilPattern RecoilPattern { get; set; } = new();

	public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && Input.Pressed( "attack1" );

	public override void OnControl()
	{
		base.OnControl();

		if ( IsProxy )
			return;

		if (IsReloading && Input.Pressed( "attack1" ))
		{
			reloadCancled = true;
		}
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		AddRecoil( RecoilPattern.GetPoint( LastShot, BulletTime ) );

		BroadcastAttackPrimary();

		ViewModel?.Set( "b_attack", true );
		Ammo--;

		ShootEffects();
		ShootBullet( 1.5f, Damage, 3.0f );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Controller?.BodyModelRenderer?.Set( "b_attack", true );
	}

	public override void StartReloadEffects()
	{
		ViewModel?.Set( "reloading", true );
	}

	bool reloadCancled;
	public override void OnReloadFinish()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		if ( Ammo < MaxAmmo && !reloadCancled )
		{
			if ( Ammo > 0 )
			{
				//SoundExtensions.FollowSound( LoadSound, GameObject );
				ViewModel?.Set( "reload", true );
			}
			Ammo++;
			TimeSinceReload = ReloadTime - LoadTime;
			IsReloading = true;
		}
		else
			FinishReloadSequence();

		reloadCancled = false;
	}

	protected virtual void FinishReloadSequence()
	{
		IsReloading = false;
		ViewModel?.Set( "reloading", false );
	}
}
