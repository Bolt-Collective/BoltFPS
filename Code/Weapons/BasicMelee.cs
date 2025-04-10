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

			if ( tr.GameObject.Components.TryGet<Rigidbody>( out var prop ) )
			{
				prop.BroadcastApplyForce( forward * 80 * 100 );
			}
			else if ( tr.GameObject.Root.Components.TryGet<Pawn>( out var player ) )
			{
				player.TakeDamage( 25 );
			}
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
