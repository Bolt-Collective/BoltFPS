namespace Seekers;

[Spawnable, Library( "weapon_pistol" )]
partial class Pistol : BaseWeapon, Component.ICollisionListener
{
	[Property] public override float Damage { get; set; } = 9f;
	public RealTimeSince TimeSinceDischarge { get; set; }
	[Property] public float MinRecoil { get; set; } = 0.5f;
	[Property] public float MaxRecoil { get; set; } = 1f;

	[Property] public RecoilPattern RecoilPattern { get; set; } = new();

	public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && Input.Pressed( "attack1" );

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
		AttachEjectedBullet( GameObject.GetPrefab( "weapons/common/effects/eject_9mm.prefab" ), "eject",
			new Vector3( Game.Random.Next( -100, 100 ), 100, 100 ) );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
		var snd = Sound.Play( ShootSound, WorldPosition );
		snd.SpacialBlend = Owner.IsValid() && Owner.IsMe ? 0 : snd.SpacialBlend;;
	}
}
