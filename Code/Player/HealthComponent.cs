using Sandbox.Events;

namespace Seekers;

/// <summary>
/// A health component for any kind of GameObject.
/// </summary>
public partial class HealthComponent : Component, IRespawnable
{

	[Property] public HealthComponent LinkedHealth { get; set; }

	[Button]
	async void Kill()
	{
		await Task.DelaySeconds( 1 );
		TakeDamage( this, 1000 );
	}

	/// <summary>
	/// Are we in god mode?
	/// </summary>
	[Property, Sync( SyncFlags.FromHost )]
	public bool IsGodMode { get; set; } = false;

	/// <summary>
	/// An action (mainly for ActionGraphs) to respond to when a GameObject's health changes.
	/// </summary>
	[Property]
	public Action<float, float> OnHealthChanged { get; set; }

	[Property] public Action<DamageInfo> OnKilled { get; set; }

	/// <summary>
	/// How long has it been since life state changed?
	/// </summary>
	public TimeSince TimeSinceLifeStateChanged { get; private set; } = 1f;

	/// <summary>
	/// A list of all Respawnable things on this GameObject
	/// </summary>

	protected IEnumerable<IDamageListener> DamageListeners => GetComponents<IDamageListener>();

	/// <summary>
	/// What's our health?
	/// </summary>
	[Property, Sync( SyncFlags.FromHost ), Change( nameof(OnHealthPropertyChanged) )]
	public float Health { get; set; } = 100f;

	[Property, Group( "Setup" )] public float MaxHealth { get; set; } = 100f;

	/// <summary>
	/// What's our life state?
	/// </summary>
	[Group( "Life State" ), Sync( SyncFlags.FromHost ), Change( nameof(OnStatePropertyChanged) )]
	public LifeState State { get; private set; }

	/// <summary>
	/// Called when <see cref="Health"/> is changed across the network.
	/// </summary>
	/// <param name="oldValue"></param>
	/// <param name="newValue"></param>
	protected void OnHealthPropertyChanged( float oldValue, float newValue )
	{
		OnHealthChanged?.Invoke( oldValue, newValue );
	}

	protected void OnStatePropertyChanged( LifeState oldValue, LifeState newValue )
	{
		TimeSinceLifeStateChanged = 0f;
	}

	protected override void OnStart()
	{
		Health = MaxHealth;
	}

	[Rpc.Broadcast]
	public void TakeDamage( Component attacker, float damage, Component inflictor = null, Vector3 position = default,
		Vector3 force = default, HitboxTags hitbox = default, DamageFlags flags = DamageFlags.None,
		float armorDamage = 0f, bool external = false )
	{
		if(LinkedHealth.IsValid())
		{
			LinkedHealth.TakeDamage( attacker, damage, inflictor, position, force, hitbox, flags, armorDamage, external );
			return;
		}
		if ( !Networking.IsHost )
			return;

		var damageInfo = new DamageInfo( attacker, damage, inflictor, position, force, hitbox, flags, armorDamage,
			external );

		damageInfo = WithThisAsVictim( damageInfo );
		damageInfo = ModifyDamage( damageInfo );

		BroadcastDamage( damageInfo );

		if ( IsGodMode ) return;

		Health = Math.Max( 0f, Health - damageInfo.Damage );

		if ( Health > 0f || State != LifeState.Alive ) return;

		Health = 0f;
		State = LifeState.Dead;

		Kill( damageInfo );
	}

	private DamageInfo WithThisAsVictim( DamageInfo damageInfo )
	{
		var extraFlags = DamageFlags.None;
		var hitbox = damageInfo.Hitbox;

		if ( damageInfo.WasExplosion || damageInfo.WasMelee ) hitbox = HitboxTags.UpperBody;
		if ( damageInfo.WasFallDamage ) hitbox = HitboxTags.Leg;

		return damageInfo with { Victim = this, Hitbox = hitbox, Flags = damageInfo.Flags | extraFlags };
	}

	private DamageInfo ModifyDamage( DamageInfo damageInfo )
	{
		damageInfo = ModifyDamage<ModifyDamageGivenEvent>( damageInfo.Attacker?.GameObject?.Root, damageInfo );
		damageInfo = ModifyDamage<ModifyDamageTakenEvent>( damageInfo.Victim?.GameObject?.Root, damageInfo );
		damageInfo = ModifyDamage<ModifyDamageGlobalEvent>( Scene, damageInfo );

		return damageInfo;
	}

	private static DamageInfo ModifyDamage<T>( GameObject root, DamageInfo damageInfo )
		where T : ModifyDamageEvent, new()
	{
		if ( root is null )
		{
			return damageInfo;
		}

		var modifyEvent = new T { DamageInfo = damageInfo };

		root?.Dispatch( modifyEvent );

		return modifyEvent.DamageInfo;
	}

	private void BroadcastDamage( DamageInfo damageInfo )
	{
		BroadcastDamage( damageInfo.Damage, damageInfo.Position, damageInfo.Force,
			damageInfo.Attacker, damageInfo.Inflictor,
			damageInfo.Hitbox, damageInfo.Flags );
	}

	private void Kill( DamageInfo damageInfo )
	{
		BroadcastKill( damageInfo.Damage, damageInfo.Position, damageInfo.Force,
			damageInfo.Attacker, damageInfo.External, damageInfo.Inflictor,
			damageInfo.Hitbox, damageInfo.Flags );
	}

	public static Pawn GetPlayerFromComponent( Component component )
	{
		if ( component is Pawn player ) return player;
		if ( !component.IsValid() ) return null;
		return !component.GameObject.IsValid() ? null : component.GameObject.Root.GetComponentInChildren<Pawn>();
	}

	[Rpc.Broadcast]
	private void BroadcastDamage( float damage, Vector3 position, Vector3 force, Component attacker,
		Component inflictor = default, HitboxTags hitbox = default, DamageFlags flags = default )
	{
		var damageInfo = new DamageInfo( attacker, damage, inflictor, position, force, hitbox, flags )
		{
			Victim = GetPlayerFromComponent( this )
		};

		GameObject.Root.Dispatch( new DamageTakenEvent( damageInfo ) );

		Scene.Dispatch( new DamageTakenGlobalEvent( damageInfo ) );

		if ( damageInfo.Attacker.IsValid() )
		{
			damageInfo.Attacker.GameObject.Root.Dispatch( new DamageGivenEvent( damageInfo ) );
		}

		DamageListeners.ToList().ForEach( x => x.OnDamaged( damageInfo ) );
	}

	[Rpc.Broadcast]
	private void BroadcastKill( float damage, Vector3 position, Vector3 force, Component attacker,
		bool external = false,
		Component inflictor = default, HitboxTags hitbox = default, DamageFlags flags = default )
	{
		var damageInfo =
			new DamageInfo( attacker, damage, inflictor, position, force, hitbox, flags, External: external )
			{
				Victim = this
			};

		AddKill( attacker.Network.OwnerId );


		//listen for this instead
		//if ( attacker.IsValid() && attacker?.Network.OwnerId != Network.OwnerId && inflictor != default && attacker.GameObject.Root.Components.TryGet<PropHuntPawn>( out var pawn ) && attacker.Network.OwnerId == Connection.Local.Id )
		//	SoundExtensions.FollowSound( SoundExtensions.RandomSoundFromFolder( pawn?.KillFolder ), attacker?.GameObject, attacker?.Network.OwnerId.ToString(), "LocalTaunt" );

		Scene.Dispatch( new KillEvent( damageInfo ) );

		OnKilled?.Invoke( damageInfo );

		damageInfo.DisplayFeed();
	}

	[Rpc.Broadcast]
	public void AddKill(Guid player)
	{
		if ( Connection.Local.Id != player || !Client.Local.IsValid())
			return;

		Client.Local.Kills++;
	}

	[Rpc.Broadcast]
	public void AddDeath( Guid player )
	{
		if ( Connection.Local.Id != player || !Client.Local.IsValid() )
			return;

		Client.Local.Deaths++;
	}
}

/// <summary>
/// The component's life state.
/// </summary>
public enum LifeState
{
	Alive,
	Dead
}

/// <summary>
/// A respawnable object.
/// </summary>
public interface IRespawnable
{
	public void OnRespawn() { }
	public void OnKill( DamageInfo damageInfo ) { }
}

public interface IDamageListener
{
	public void OnDamaged( DamageInfo damageInfo ) { }
}
