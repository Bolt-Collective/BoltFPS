namespace Seekers;

partial class QuickMelee : BasicMelee
{
	[Feature( "Firing" ), Property] public float HitDelay { get; set; } = 0.27f;
	[Feature( "Firing" ), Property] public float SwitchDelay { get; set; } = 0.25f;

	public override bool CanPrimaryAttack() => base.CanPrimaryAttack() && Input.Pressed( "attack1" );

	public override void AttackPrimary()
	{

	}

	protected async override void OnEnabled()
	{
		base.OnEnabled();

		if ( IsProxy )
			return;

		Owner.Inventory.CanChange = false;

		await Task.DelaySeconds( HitDelay );

		MeleeAttack();

		await Task.DelaySeconds( SwitchDelay );

		Owner.Inventory.CanChange = true;

		Owner.Inventory.SetActiveSlot( Owner.Inventory.lastSlot );

		Owner.Inventory.RemoveWeapon( this );
	}

	public override void OnControl()
	{
		Owner.Inventory.CanChange = false;
	}
}
