namespace Seekers;

[Spawnable, Library( "weapon_crowbar" )]
partial class BasicMelee : BaseWeapon
{

	[Property] public float Range { get; set; } = 80;
	[Property] public float Radius { get; set; } = 5;
	public override bool CanReload()
	{
		return false;
	}

	[Rpc.Broadcast]
	private void BroadcastAttack()
	{
		Owner?.Renderer?.Set( "b_attack", true );
		Sound.Play( ShootSound, WorldPosition );
	}

	public override void AttackPrimary()
	{
		BroadcastAttack();
		if ( MeleeAttack() )
		{
			OnMeleeHit();
		}
		else
		{
			OnMeleeMiss();
		}
	}

	private bool MeleeAttack()
	{
		var ray = Owner.AimRay;

		var forward = ray.Forward;
		forward = forward.Normal;

		bool hit = false;

		foreach ( var tr in TraceMelee( ray.Position, ray.Position + forward * Range, Radius ) )
		{
			tr.Surface.DoBulletImpact( tr );

			hit = true;

			if ( !tr.GameObject.IsValid() ) continue;

			var hitboxTags = tr.GetHitboxTags();

			var damage = Damage;

			if ( hitboxTags.Contains( HitboxTags.Head ) )
				damage *= 2;

			var calcForce = forward * 250000 * damage;

			DoDamage( tr.GameObject, damage, calcForce, tr.HitPosition, hitboxTags );
		}

		return hit;
	}

	private void OnMeleeMiss()
	{
		ViewModel?.Renderer?.Set( "b_attack_has_hit", false );
		ViewModel?.Renderer?.Set( "b_attack", true );
	}

	private void OnMeleeHit()
	{
		ViewModel?.Renderer?.Set( "b_attack_has_hit", true );
		ViewModel?.Renderer?.Set( "b_attack", true );
	}
}
