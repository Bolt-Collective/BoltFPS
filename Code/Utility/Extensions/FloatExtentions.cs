namespace Seekers;

public static partial class FloatExtensions
{
	/// <summary>
	/// Take damage. Only the host can call this.
	/// </summary>
	/// <param name="go"></param>
	/// <param name="damageInfo"></param>
	public static void TakeDamage( this GameObject go, DamageInfo damageInfo )
	{
		foreach ( var damageable in go.Root.GetComponents<HealthComponent>() )
		{
			damageable.TakeDamage( damageInfo.Attacker, damageInfo.Damage, damageInfo.Inflictor, damageInfo.Position, damageInfo.Force, damageInfo.Hitbox, damageInfo.Flags, damageInfo.ArmorDamage, damageInfo.External );
		}
	}

	public static float MoveTowards( float current, float target, float maxDelta )
	{
		return current += MathX.Clamp( target - current, -maxDelta, maxDelta );
	}

	public static float MoveTowardsAngle( float current, float target, float maxDelta )
	{
		float deltaAngle = MathX.DeltaDegrees( current, target );
		if ( -maxDelta < deltaAngle && deltaAngle < maxDelta )
			current = target;
		target = current + deltaAngle;
		return MoveTowards(current, target, maxDelta );
	}
}
