using Sandbox.Diagnostics;

namespace Seekers;

public sealed class PlayerInventory : Component, IPlayerEvent
{
	[RequireComponent] public Pawn Owner { get; set; }
	[Sync, Property] public NetList<BaseWeapon> Weapons { get; set; } = new();
	[Sync] public BaseWeapon ActiveWeapon { get; set; }

	public Action<GameObject, float> OnShootGameObject;

	[Rpc.Broadcast]
	public void ResetWeapons()
	{
		if ( IsProxy ) return;

		foreach ( var weapon in Weapons )
			weapon.DestroyGameObject();

		Weapons = new();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( Input.Pressed( "slot1" ) ) SetActiveSlot( 0 );
		if ( Input.Pressed( "slot2" ) ) SetActiveSlot( 1 );
		if ( Input.Pressed( "slot3" ) ) SetActiveSlot( 2 );
		if ( Input.Pressed( "slot4" ) ) SetActiveSlot( 3 );
		if ( Input.Pressed( "slot5" ) ) SetActiveSlot( 4 );
		if ( Input.Pressed( "slot6" ) ) SetActiveSlot( 5 );
		if ( Input.Pressed( "slot7" ) ) SetActiveSlot( 6 );
		if ( Input.Pressed( "slot8" ) ) SetActiveSlot( 7 );
		if ( Input.Pressed( "slot9" ) ) SetActiveSlot( 8 );

		if ( Input.MouseWheel != 0 ) SwitchActiveSlot( (int)-Input.MouseWheel.y );
	}

	[Rpc.Broadcast]
	public void Pickup( string prefabName, bool equip = true )
	{
		if ( IsProxy )
			return;

		if ( !Owner.Renderer.IsValid() )
			return;

		Owner.Renderer.GameObject.Enabled = true;

		var prefab = GameObject.Clone( prefabName, global::Transform.Zero, Owner.Renderer.GameObject, false );
		prefab?.NetworkSpawn( false, Network.Owner );

		var weapon = prefab?.Components.Get<BaseWeapon>( true );
		//Assert.NotNull( weapon );

		Weapons.Add( weapon );

		if ( equip )
			SetActiveSlot( Weapons.Count - 1 );

		IPlayerEvent.PostToGameObject( Owner.GameObject, e => e.OnWeaponAdded( weapon ) );
		ILocalPlayerEvent.Post( e => e.OnWeaponAdded( weapon ) );
	}

	public void RemoveWeapon( BaseWeapon weapon )
	{
		Weapons?.Remove( weapon );
		weapon?.GameObject?.BroadcastDestroy();
		SetActiveSlot( lastSlot.Clamp( 0, Weapons.Count() - 1 ) );
	}

	[Rpc.Broadcast]
	public void Pickup( GameObject prefabObject )
	{
		if ( IsProxy )
			return;

		Owner.Renderer.GameObject.Enabled = true;

		var prefab = prefabObject.Clone( global::Transform.Zero, Owner.Renderer.GameObject, false );
		prefab.NetworkSpawn( false, Network.Owner );

		var weapon = prefab.Components.Get<BaseWeapon>( true );
		Assert.NotNull( weapon );

		Weapons.Add( weapon );

		SetActiveSlot( Weapons.Count - 1 );

		IPlayerEvent.PostToGameObject( Owner.GameObject, e => e.OnWeaponAdded( weapon ) );
		ILocalPlayerEvent.Post( e => e.OnWeaponAdded( weapon ) );
	}

	[ConCmd( "give" )]
	public static void GiveWeapon( string name, string group = null )
	{
		Pawn.Local?.Inventory?.Pickup( $"weapons/{group ?? name}/w_{name}.prefab" );
	}

	public static void GiveWeapon( GameObject Weapon, string group = null )
	{
		Pawn.Local?.Inventory?.Pickup( Weapon );
	}

	public int lastSlot;
	int currentSlot;

	public void SetActiveSlot( int i )
	{
		lastSlot = currentSlot;
		currentSlot = i;

		var weapon = GetSlot( i );
		if ( ActiveWeapon == weapon )
			return;

		if ( weapon == null )
			return;

		if ( ActiveWeapon.IsValid() )
			ActiveWeapon.GameObject.Enabled = false;

		ActiveWeapon = weapon;

		if ( ActiveWeapon.IsValid() )
			ActiveWeapon.GameObject.Enabled = true;
	}

	public BaseWeapon GetSlot( int i )
	{
		if ( Weapons.Count <= i ) return null;
		if ( i < 0 ) return null;

		return Weapons[i];
	}

	public int GetActiveSlot()
	{
		var aw = ActiveWeapon;
		var count = Weapons?.Count;

		for ( int i = 0; i < count; i++ )
		{
			if ( Weapons[i] == aw )
				return i;
		}

		return -1;
	}

	public void SwitchActiveSlot( int idelta )
	{
		var count = Weapons.Count;
		if ( count == 0 ) return;

		var slot = GetActiveSlot();
		var nextSlot = slot + idelta;

		while ( nextSlot < 0 ) nextSlot += count;
		while ( nextSlot >= count ) nextSlot -= count;

		SetActiveSlot( nextSlot );
	}

	[Rpc.Broadcast]
	void IPlayerEvent.OnSpawned()
	{
		if ( IsProxy ) return;
		Pickup( "prefabs/weapons/fists/w_fists.prefab" );
		SetActiveSlot( 0 );
	}

	[Rpc.Broadcast]
	void IPlayerEvent.OnDied()
	{
		if ( IsProxy ) return;

		if ( Weapons.Count <= 0 )
			return;

		foreach ( var weapon in Weapons )
			weapon.DestroyGameObject();
	}
}
