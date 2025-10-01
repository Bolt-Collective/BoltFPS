namespace Seekers;

[Spawnable, Library( "weapon_pistol" )]
partial class Pistol : BaseWeapon, Component.ICollisionListener
{
	[Feature( "Firing" ), Property] public override float Damage { get; set; } = 9f;
	public RealTimeSince TimeSinceDischarge { get; set; }
	[Feature( "Firing" ), Property] public float MinRecoil { get; set; } = 0.5f;
	[Feature( "Firing" ), Property] public float MaxRecoil { get; set; } = 1f;

	[Feature( "Firing" ), Property] public RecoilPattern RecoilPattern { get; set; }

	public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && Input.Pressed( "attack1" );

	public override void AttackPrimary()
	{
		base.AttackPrimary();

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		AddRecoil( RecoilPattern.GetPoint( LastShot, BulletTime ) );

		BroadcastAttackPrimary();

		ViewModel?.Set( "b_attack", true );
		Ammo--;

		ShootEffects();
		ShootBullet( 1.5f, Damage, 0.25f );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}
}
