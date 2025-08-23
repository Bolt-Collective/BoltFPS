namespace Seekers;

[Spawnable, Library( "weapon_pistol" )]
partial class ProjectileWeapon : BaseWeapon, Component.ICollisionListener
{
	//[Property] public override float Damage { get; set; } = 9f;
	//public RealTimeSince TimeSinceDischarge { get; set; }
	//[Property] public float MinRecoil { get; set; } = 0.5f;
	//[Property] public float MaxRecoil { get; set; } = 1f;

	//public override bool UseProjectile => true;

	//[Property] public RecoilPattern RecoilPattern { get; set; } = new();

	//public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && Input.Pressed( "attack1" );

	//public override void AttackPrimary()
	//{
	//	TimeSincePrimaryAttack = 0;
	//	TimeSinceSecondaryAttack = 0;

	//	AddRecoil( RecoilPattern.GetPoint( LastShot, BulletTime ) );

	//	BroadcastAttackPrimary();

	//	ViewModel?.Set( "b_attack", true );
	//	Ammo--;

	//	ShootEffects();
	//	ShootBullet( 1.5f, Damage, 3.0f );
	//}

	//[Rpc.Broadcast]
	//private void BroadcastAttackPrimary()
	//{
	//	Owner?.Controller?.BodyModelRenderer?.Set( "b_attack", true );

	//	SoundExtensions.AlertingSound( ShootSound?.ResourcePath ?? "", Owner,	 WorldPosition, SoundDistance, 2 );
	//}
}
