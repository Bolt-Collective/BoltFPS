namespace Seekers;

public static partial class GameObjectExtensions
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

	public static void CopyPropertiesTo( this Component src, Component dst )
	{
		var json = src.Serialize().AsObject();
		json.Remove( "__guid" );
		dst.DeserializeImmediately( json );
	}

	public static void DestroyAsync( this GameObject self, float seconds = 1.0f )
	{
		var component = self.Components.Create<TimedDestroyComponent>();
		component.Time = seconds;
	}

	[Rpc.Broadcast]
	public static void BroadcastDestroy( this GameObject self )
	{
		self?.Destroy();
	}

	[Rpc.Broadcast]
	public static void SetParamNet( this SkinnedModelRenderer self, string param, bool active )
	{
		self?.Set( param, active );
	}
}
