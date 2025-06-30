namespace Seekers;

[Spawnable, Library( "weapon_machinegun", Title = "Machine Gun" )]
partial class MachineGun : BaseWeapon
{
	[Property] public bool EjectCasing { get; set; } = true;
	[Property] public override float Damage { get; set; } = 5f;
	[Property] public float MinRecoil { get; set; } = 0.5f;
	[Property] public float MaxRecoil { get; set; } = 1f;

	[Property] public RecoilPattern RecoilPattern { get; set; } = new();

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;
		Ammo--;

		BroadcastAttackPrimary();

		AddRecoil( RecoilPattern.GetPoint( LastShot, BulletTime ) );

		ViewModel?.Set( "b_attack", true );

		//
		// Tell the clients to play the shoot effects
		//
		ShootEffects();

		//
		// Shoot the bullets
		//
		ShootBullet( 1.5f, Damage, 3.0f );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
		var snd = Sound.Play( ShootSound, WorldPosition );
		snd.SpacialBlend = Owner.IsValid() && Owner.IsMe ? 0 : snd.SpacialBlend;
	}

	public override void OnControl()
	{
		base.OnControl();

		var attackHold = !IsReloading && Input.Down( "attack1" ) ? 1.0f : 0.0f;

		BroadcastOnControl( attackHold );

		ViewModel?.Set( "attack_hold", attackHold );
	}

	[Rpc.Broadcast]
	private void BroadcastOnControl( float attackHold )
	{
		Owner?.Renderer?.Set( "attack_hold", attackHold );
	}

	// TODO: Probably should unify these particle methods + make it work for world models

	private GameObject eject = GameObject.GetPrefab( "weapons/common/effects/eject_9mm.prefab" );

	protected override void ShootEffects()
	{
		base.ShootEffects();

		AttachParticleSystem( eject, "eject", 1, LocalWorldModel.GameObject );
	}
}
