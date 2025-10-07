using Sandbox.Diagnostics;

namespace Seekers;

public class PlayerInventory : Component, IPlayerEvent
{
	[RequireComponent] public Pawn Owner { get; set; }
	[Sync, Property] public NetList<BaseWeapon> Weapons { get; set; } = new();
	[Sync, Property] public BaseWeapon ActiveWeapon { get; set; }
	[Property] public bool InfiniteAmmo { get; set; }

	[Property] public Dictionary<string, int> Ammo { get; set; } = new();

	public Action<GameObject, float> OnShootGameObject;

	public bool CanChange = true;

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
		if ( ActiveWeapon == null )
			Owner?.Controller?.BodyModelRenderer?.Set( "holdtype", 0 );
		if ( IsProxy )
			return;

		for ( int i = 0; i < 9; i++ )
		{
			if ( !Input.Pressed( $"slot{i + 1}" ) )
				continue;

			if ( currentSlot == i )
			{
				SetActiveSlot( -1 );
				continue;
			}

			SetActiveSlot( i );
			break;
		}

		if ( Input.MouseWheel != 0 ) SwitchActiveSlot( (int)-Input.MouseWheel.y );
	}

	[Rpc.Owner]
	public void Pickup( string prefabName, bool equip = true )
	{
		if ( string.IsNullOrEmpty( prefabName ) )
			return;

		var prefabObject = GameObject.GetPrefab( prefabName );
		if ( prefabObject == null )
			return;

		Pickup( prefabObject, equip );
	}

	public virtual void Pickup( GameObject prefabObject, bool equip = true )
	{
		if ( !Owner.Renderer.IsValid() )
			return;

		Owner.Renderer.GameObject.Enabled = true;

		var prefab = prefabObject.Clone( global::Transform.Zero, Owner.Renderer.GameObject, false );
		prefab.NetworkSpawn( false, Network.Owner );

		var weapon = prefab.Components.Get<BaseWeapon>( true );
		Assert.NotNull( weapon );

		Weapons.Add( weapon );

		if ( equip )
			SetActiveSlot( Weapons.Count - 1 );

		IPlayerEvent.PostToGameObject( Owner.GameObject, e => e.OnWeaponAdded( weapon ) );
	}

	public virtual int GetReserveAmmo( string ammoType = "" )
	{
		if ( Ammo == null )
			Ammo = new();

		if (InfiniteAmmo)
			return int.MaxValue;

		if ( !Ammo.ContainsKey( ammoType ) )
			return 0;

		return Ammo[ ammoType ];
	}

	public void TakeReserve(string ammoType, int amount)
	{
		if ( Ammo == null )
			Ammo = new();

		if ( !Ammo.ContainsKey( ammoType ) )
			return;

		Ammo[ ammoType ] -= amount;
	}

	public void GiveReserve( string ammoType, int amount )
	{
		if ( Ammo == null )
			Ammo = new();

		if ( !Ammo.ContainsKey( ammoType ) )
		{
			Ammo.Add( ammoType, amount );
			return;
		}

		Ammo[ammoType] += amount;
	}


	public void RemoveWeapon( BaseWeapon weapon )
	{
		Weapons?.Remove( weapon );
		weapon?.GameObject?.BroadcastDestroy();
		SetActiveSlot( lastSlot.Clamp( 0, Weapons.Count() - 1 ) );
	}

	[ConCmd( "give" )]
	public static void GiveWeapon( string name, string group = null )
	{
		FindItemByName( name )?.GiveWeapon();
	}

	public static ItemResource FindItemByName( string name )
	{
		foreach ( var res in ResourceLibrary.GetAll<ItemResource>() )
		{
			if ( res.ResourceName.Equals( name, StringComparison.OrdinalIgnoreCase ) )
				return res;
		}

		return null;
	}

	public static void GiveWeapon( GameObject Weapon, string group = null )
	{
		Pawn.Local?.Inventory?.Pickup( Weapon );
	}

	public int lastSlot;
	public int currentSlot;

	public void SetActiveSlot( int i )
	{
		if ( !CanChange )
			return;

		lastSlot = currentSlot;
		currentSlot = i;

		var weapon = GetSlot( i );
		if ( ActiveWeapon != null && ActiveWeapon == weapon )
			return;

		if ( ActiveWeapon.IsValid() )
			ActiveWeapon.GameObject.Enabled = false;

		ActiveWeapon = null;

		if ( weapon == null )
			return;

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

	public void GiveTemporaryWeapon(ItemResource weapon, float duration = 5f)
	{
		GiveTemporaryWeapon( weapon.ResourcePath, duration );
	}

	public async void GiveTemporaryWeapon( string prefabName, float duration = 5f )
	{
		var prevSlot = GetActiveSlot();

		var prefabObject = GameObject.GetPrefab( prefabName );
		if ( !prefabObject.IsValid() )
			return;

		var prefab = prefabObject.Clone( global::Transform.Zero, Owner.Renderer.GameObject, false );
		prefab.NetworkSpawn( false, Network.Owner );

		if ( !prefab.Components.TryGet<BaseWeapon>( out var weapon ) )
			return;

		Weapons.Add( weapon );
		SetActiveSlot( Weapons.Count - 1 );

		IPlayerEvent.PostToGameObject( Owner.GameObject, e => e.OnWeaponAdded( weapon ) );

		await Task.DelaySeconds( duration );

		if ( weapon.IsValid() )
		{
			if ( ActiveWeapon == weapon )
				SetActiveSlot( prevSlot );

			RemoveWeapon( weapon );
		}
	}
}
