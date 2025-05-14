using Sandbox.Citizen;
using Sandbox.Components;
using Sandbox.UI;
using Sandbox.Utility;
using XMovement;
using static System.Net.Mime.MediaTypeNames;

namespace Seekers;

/// <summary>
/// A common base we can use for weapons so we don't have to implement the logic over and over
/// again. Feel free to not use this and to implement it however you want to.
/// </summary>
[Icon( "sports_martial_arts" )]
public partial class BaseWeapon : Component
{
	[Property] public string Name { get; set; }
	[Property] public WeaponIK LeftIK { get; set; }
	[Property] public CrosshairType CrosshairType { get; set; }

	[Property] public WeaponResource WeaponResource { get; set; }

	public class WeaponIK
	{
		public Action<GameObject, bool, bool> SetActive;
		bool _active = true;

		public bool Active
		{
			get { return _active; }
			set
			{
				_active = value;
				SetActive?.Invoke( GameObject, value, IsLeft );
			}
		}

		[KeyProperty] public GameObject GameObject { get; set; }
		public bool IsLeft { get; set; }
	}

	[Property, Group( "Sounds" )]
	public SoundEvent DeploySound { get; set; } =
		ResourceLibrary.Get<SoundEvent>( "sounds/weapons/switch/switch_3.sound" );

	[Property, Group( "Sounds" )] public SoundEvent ReloadSound { get; set; }
	[Property, Group( "Sounds" )] public SoundEvent ReloadShortSound { get; set; }
	[Property, Group( "Sounds" )] public SoundEvent ShootSound { get; set; }

	[Property, ImageAssetPath] public string Icon { get; set; }
	[Property] public GameObject ViewModelPrefab { get; set; }

	[Property] public GameObject TracerEffect { get; set; }

	private GameObject Tracer
	{
		get
		{
			if ( TracerEffect.IsValid() ) return TracerEffect;

			return GameObject.GetPrefab( $"/weapons/common/effects/tracer.prefab" );
		}
	}

	[Property] public string ParentBone { get; set; } = "hold_r";
	[Property] public Transform BoneOffset { get; set; } = new Transform( 0 );

	[Property]
	public CitizenAnimationHelper.HoldTypes HoldType { get; set; } = CitizenAnimationHelper.HoldTypes.HoldItem;

	[Property] public CitizenAnimationHelper.Hand Handedness { get; set; } = CitizenAnimationHelper.Hand.Right;

	[Property] public float PrimaryRate { get; set; } = 5.0f;
	[Property] public float SecondaryRate { get; set; } = 15.0f;

	[Property] public float ReloadTime { get; set; } = 3.0f;

	[Property] public virtual float Damage { get; set; }

	[Property] public int Ammo { get; set; }
	[Property] public int MaxAmmo { get; set; }
	[Property] public bool ShowAmmo { get; set; } = true;

	[Property] public virtual float Spread { get; set; }
	public virtual float SpreadIncrease { get; set; }

	[Sync] public bool IsSitting { get; set; }
	[Sync] public bool IsReloading { get; set; }

	[Sync] public RealTimeSince TimeSinceReload { get; set; }
	[Sync] public RealTimeSince TimeSinceDeployed { get; set; }
	[Sync] public RealTimeSince TimeSincePrimaryAttack { get; set; }
	[Sync] public RealTimeSince TimeSinceSecondaryAttack { get; set; }

	public ViewModel ViewModel => Owner?.Controller?.Camera?.GetComponentInChildren<ViewModel>( true );
	public SkinnedModelRenderer WorldModel => GameObject?.GetComponentInChildren<SkinnedModelRenderer>( true );

	public SkinnedModelRenderer LocalWorldModel =>
		!Owner.IsValid() ||
		!Owner.Controller.IsValid() ||
		Owner.Controller.BodyVisible
			? WorldModel
			: ViewModel?.Renderer;

	public Pawn Owner => GameObject?.Root?.Components.Get<Pawn>( FindMode.EverythingInSelfAndDescendants );

	public PlayerInventory Inventory =>
		GameObject?.Root?.Components.Get<PlayerInventory>( FindMode.EverythingInSelfAndDescendants );

	public RealTimeSince LastShot { get; set; }
	public Angles Recoil { get; set; }
	public virtual float RecoilTime => 0.05f.Clamp( 0, BulletTime );
	public float BulletTime => 1 / PrimaryRate;

	public bool ForceDisableViewmodel;

	public Transform Attachment( string name ) => LocalWorldModel?.GetAttachment( name ) ?? WorldTransform;


	protected override void OnAwake()
	{
		var obj = Owner?.Controller?.BodyModelRenderer?.GetBoneObject( ParentBone );

		if ( obj is not null )
		{
			GameObject.Parent = obj;
			GameObject.LocalTransform = BoneOffset.WithScale( 1 );
		}

		LeftIK.SetActive = SetIk;
		LeftIK.IsLeft = true;

		LeftIK.Active = true;
	}

	private bool IsNearby( Vector3 position )
	{
		if ( !Scene.Camera.IsValid() ) return false;
		return position.DistanceSquared( Scene.Camera.WorldPosition ) < 4194304f;
	}

	[Rpc.Broadcast]
	protected void DoTracer( Vector3 startPosition, Vector3 endPosition, float distance, int count )
	{
		if ( !IsNearby( startPosition ) && !IsNearby( endPosition ) ) return;

		var origin = count == 0 ? Attachment( "muzzle" ).Position : startPosition;

		var effect =
			Tracer?.Clone( new CloneConfig
			{
				Transform = new Transform().WithPosition( origin ), StartEnabled = true
			} );
		if ( effect.IsValid() && effect.GetComponentInChildren<Tracer>() is { } tracer )
		{
			tracer.EndPoint = endPosition;
		}
	}

	public void CalculateRandomRecoil( (float min, float max) pitchRecoil, (float min, float max) yawRecoil )
	{
		Recoil = new Angles( -MathF.Abs( RandomRecoilValue( pitchRecoil.min, pitchRecoil.max ) ),
			RandomRecoilValue( yawRecoil.min, yawRecoil.max ), 0 );
		LastShot = 0;
	}

	public void AddRecoil( Vector2 recoil )
	{
		Recoil = new Angles( -recoil.y, -recoil.x, 0 );
		LastShot = 0;
	}

	float RandomRecoilValue( float min, float max ) => Game.Random.Next( (int)(min * 10), (int)(max * 10) ) * 0.1f *
	                                                   (Game.Random.Next( 0, 2 ) == 1 ? -1 : 1);

	public void SetIk( GameObject target, bool active, bool left )
	{
		if ( !Owner?.Controller?.AnimationHelper.IsValid() ?? true )
			return;

		if ( left )
		{
			Owner.Controller.AnimationHelper.IkLeftHand = active ? target : null;
		}
		else
		{
			Owner.Controller.AnimationHelper.IkRightHand = active ? target : null;
		}
	}

	protected override void OnEnabled()
	{
		LeftIK.Active = true;

		TimeSinceDeployed = 0;

		BroadcastEnabled();

		if ( IsProxy ) return;

		IsReloading = false;

		if ( !Owner.IsValid() )
			return;

		Owner.Zoom = 1;

		var go = ViewModelPrefab?.Clone( new CloneConfig()
		{
			StartEnabled = false,
			Parent = Owner.Controller.Camera.GameObject,
			Transform = Owner.Controller.WorldTransform
		} );

		go.NetworkSpawn();
	}

	[Rpc.Broadcast]
	private void BroadcastEnabled()
	{
		Owner?.Renderer?.Set( "b_deploy", true );
		Sound.Play( DeploySound, WorldPosition );
	}

	protected override void OnDisabled()
	{
		Owner?.Renderer?.Set( "holdtype", 0 );

		if ( IsProxy ) return;

		ViewModel?.GameObject?.BroadcastDestroy();
	}

	protected override void OnUpdate()
	{
		GameObject.NetworkInterpolation = false;

		Owner?.Renderer?.Set( "holdtype", (int)HoldType );
		Owner?.Renderer?.Set( "holdtype_handedness", (int)Handedness );

		if ( ViewModel.IsValid() )
		{
			ViewModel.GameObject.Enabled = !(Owner?.Controller?.BodyVisible ?? true);
			if ( ForceDisableViewmodel )
				ViewModel.GameObject.Enabled = false;
		}

		if ( WorldModel.IsValid() )
			WorldModel.RenderType = !Owner?.Controller?.BodyVisible ?? false
				? ModelRenderer.ShadowRenderType.ShadowsOnly
				: ModelRenderer.ShadowRenderType.On;

		if ( IsProxy )
			return;

		if ( LastShot < RecoilTime && Owner.IsValid() )
		{
			Owner.Controller.EyeAngles += Recoil * Time.Delta / (RecoilTime);
		}

		Owner?.Controller?.Tags.Set( "viewer",
			Owner.Controller.CameraMode.Equals( PlayerWalkControllerComplex.CameraModes.ThirdPerson ) );

		OnControl();
	}

	public virtual void OnControl()
	{
		if ( TimeSinceDeployed < 0.6f )
			return;

		if ( IsProxy )
			return;

		if ( !IsReloading && CanReload() )
		{
			Reload();
		}

		//
		// Reload could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		bool canPrimaryAttack = CanPrimaryAttack();
		if ( canPrimaryAttack && Ammo > 0 )
		{
			TimeSincePrimaryAttack = 0;
			AttackPrimary();
		}
		else if ( canPrimaryAttack )
		{
			AttackDry();
		}

		//
		// AttackPrimary could have changed our owner
		//
		if ( !Owner.IsValid() )
			return;

		if ( CanSecondaryAttack() )
		{
			TimeSinceSecondaryAttack = 0;
			AttackSecondary();
		}

		FinishReload();
	}

	public virtual void FinishReload()
	{
		if ( IsReloading && TimeSinceReload > ReloadTime )
		{
			OnReloadFinish();
		}
	}

	public virtual void OnReloadFinish()
	{
		Ammo = Math.Clamp( Ammo + (MaxAmmo - 1), 0, MaxAmmo );
		IsReloading = false;
	}

	public virtual void StartReloadEffects()
	{
		ViewModel?.Set( "b_reload", true );
		SoundExtensions.FollowSound( Ammo > 0 ? ReloadShortSound : ReloadSound, GameObject );
	}

	//don't know how spaces keep ending up in this string, but it breaks it so whatever is happening needs to stop
	private LoadedPrefab muzzle = new LoadedPrefab( "weapons/common/effects/muzzle.prefab" );

	protected virtual void ShootEffects()
	{
		AttachParticleSystem( muzzle.Prefab, "muzzle" );

		ViewModel?.Set( "fire", true );
	}

	[Rpc.Broadcast]
	public void AttachParticleSystem( GameObject prefab, string attachment, float time = 1, GameObject parent = null )
	{
		if ( !prefab.IsValid() || !LocalWorldModel.IsValid() )
			return;

		Transform transform = LocalWorldModel?.GetAttachment( attachment ) ?? WorldTransform;

		Particles.MakeParticleSystem( prefab, transform, time, LocalWorldModel?.GameObject );
	}

	[Rpc.Broadcast]
	public void AttachEjectedBullet( GameObject prefab, string attachment, Vector3 force, float time = 1,
		GameObject parent = null )
	{
		Transform transform = LocalWorldModel?.GetAttachment( attachment ) ?? WorldTransform;

		var go = Particles.MakeParticleSystem( prefab, transform, time, parent );

		go?.GetComponent<Rigidbody>()
			?.ApplyForce( transform.Forward * force.x + transform.Right * force.y + transform.Up * force.z );
		go?.GetComponent<Rigidbody>()
			?.ApplyTorque( transform.Forward * force.x + transform.Right * force.y + transform.Up * force.z );
	}

	public virtual bool CanReload()
	{
		if ( Owner != null && Input.Down( "reload" ) ) return true;

		return false;
	}

	public virtual void Reload()
	{
		if ( IsReloading )
			return;

		TimeSinceReload = 0;
		IsReloading = true;

		BroadcastReload();
		StartReloadEffects();
	}

	[Rpc.Broadcast]
	public virtual async void BroadcastReload()
	{
		Owner?.Controller?.BodyModelRenderer?.Set( "b_reload", true );
		LeftIK.Active = false;
		await Task.DelayRealtimeSeconds( ReloadTime );
		LeftIK.Active = true;
	}

	public virtual bool CanPrimaryAttack()
	{
		if ( Owner == null || !Input.Down( "attack1" ) || IsReloading ) return false;

		var rate = PrimaryRate;
		if ( rate <= 0 ) return true;

		return TimeSincePrimaryAttack > (1 / rate);
	}

	public virtual void AttackPrimary()
	{
	}

	public virtual void AttackDry()
	{
		ViewModel?.Set( "b_attack_dry", true );
		BroadcastAttackDry();
	}

	[Rpc.Broadcast]
	private void BroadcastAttackDry()
	{
		Owner?.Renderer?.Set( "b_attack_dry", true );
	}

	public virtual bool CanSecondaryAttack()
	{
		if ( Owner == null || !Input.Down( "attack2" ) || IsReloading ) return false;

		var rate = SecondaryRate;
		if ( rate <= 0 ) return true;

		return TimeSinceSecondaryAttack > (1 / rate);
	}

	public virtual void AttackSecondary()
	{
	}

	/// <summary>
	/// Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	/// hits, like if you're going through layers or ricocheting or something.
	/// </summary>
	public static IEnumerable<SceneTraceResult> TraceBullet( GameObject ignore, Vector3 start, Vector3 end,
		float radius = 2.0f )
	{
		// bool underWater = Trace.TestPoint( start, "water" );

		var trace = Game.ActiveScene.Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement" )
			.IgnoreGameObjectHierarchy( ignore )
			.Size( radius );

		//
		// If we're not underwater then we can hit water
		//
		/*
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );
		*/

		var tr = trace.Run();

		if ( tr.Hit )
			yield return tr;

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	public IEnumerable<SceneTraceResult> TraceMelee( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var trace = Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement" )
			.IgnoreGameObjectHierarchy( GameObject.Root );

		var tr = trace.Run();

		if ( tr.Hit )
		{
			yield return tr;
		}
		else
		{
			trace = trace.Size( radius );

			tr = trace.Run();

			if ( tr.Hit )
			{
				yield return tr;
			}
		}
	}

	/// <summary>
	/// Shoot a single bullet
	/// </summary>
	List<Surface> hitSurfaces = new();

	float shotTime;
	int shots = 0;

	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float force, float damage, float bulletSize,
		float spreadOverride = -1 )
	{
		if ( shotTime != Time.Now )
		{
			shots = 0;
			hitSurfaces = new();
		}

		shotTime = Time.Now;

		shots++;

		var spread = Spread + SpreadIncrease;
		if ( spreadOverride > -1 )
			spread = spreadOverride;

		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		//
		// ShootBullet is coded in a way where we can have bullets pass through shit
		// or bounce off shit, in which case it'll return multiple results
		//
		foreach ( var tr in TraceBullet( GameObject.Root, pos, pos + forward * 5000, bulletSize ) )
		{
			var tagMaterial = "";

			foreach ( var tag in tr.Tags )
			{
				if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
				{
					tagMaterial = tag.Remove( 0, 2 );
					break;
				}
			}

			Surface surface = tagMaterial == "" ? tr.Surface : (Surface.FindByName( tagMaterial ) ?? tr.Surface);

			surface.DoBulletImpact( tr, !hitSurfaces.Contains( surface ) || shots < 3 );
			DoTracer( tr.StartPosition, tr.EndPosition, tr.Distance, count: 1 );

			hitSurfaces.Add( surface );

			if ( !tr.GameObject.IsValid() ) continue;


			var hitboxTags = tr.GetHitboxTags();

			if ( hitboxTags.Contains( HitboxTags.Head ) )
				damage *= 2;

			var calcForce = forward * 250000 * damage;

			OnShootGameobject( tr.GameObject, damage );

			DoDamage( tr.GameObject, damage, calcForce, tr.HitPosition, hitboxTags );
		}
	}

	public void DoDamage( GameObject gameObject, float damage, Vector3 calcForce, Vector3 hitPosition,
		HitboxTags hitboxTags = default )
	{
		if ( gameObject.Components.TryGet<Prop>( out var prop ) )
		{
			KnockBack( gameObject, calcForce );
		}

		if ( gameObject.Components.TryGet<NetworkedProp>( out var netProp ) )
		{
			netProp.Damage( damage );
		}

		if ( gameObject.Root.Components.TryGet<HealthComponent>( out var player,
			    FindMode.EverythingInSelfAndChildren ) )
		{
			if ( !Owner.Team.FriendlyFire && gameObject.Tags.Has( "player" ) )
			{
				var team = player.GameObject.Root.GetComponent<Pawn>()?.Team ?? null;
				if ( team == Owner.Team || Owner.Team.Friends.Contains( team ) )
					return;
			}

			player.TakeDamage( Owner, damage, this, hitPosition, calcForce, hitboxTags );
			Crosshair.Instance.Trigger( player, damage, hitboxTags );
		}
	}

	[Rpc.Host]
	public void KnockBack( GameObject gameObject, Vector3 force )
	{
		gameObject?.GetComponent<Rigidbody>()?.BroadcastApplyForce( force );
	}


	[Rpc.Broadcast]
	public void OnShootGameobject( GameObject gameObject, float damage )
	{
		if ( !Networking.IsHost )
			return;

		Inventory?.OnShootGameObject?.Invoke( gameObject, damage );
	}


	public void ThrowGrenade( GameObject projectile, float force, float distance = 20 )
	{
		var ray = Owner.AimRay;
		SpawnGrenade( Client.Local.GetPawn<Pawn>(), ray.Position, ray.Forward, projectile, force, distance,
			Owner?.Controller?.Controller.Velocity ?? Vector3.Zero );
	}

	[Rpc.Broadcast]
	private void SpawnGrenade( Pawn player, Vector3 pos, Vector3 dir, GameObject projectile, float force,
		float distance, Vector3 playerVelocity )
	{
		if ( !Networking.IsHost )
			return;

		var trace = Scene.Trace.Ray( pos, pos + dir.Normal * distance )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement" )
			.IgnoreGameObjectHierarchy( GameObject.Root )
			.Size( 5 )
			.Run();

		pos = trace.EndPosition;

		var gameobject = projectile.Clone( pos, Rotation.LookAt( dir ) );
		var rigidBody = gameobject.GetComponent<Rigidbody>();
		var grenade = gameobject.GetComponent<BaseGrenade>();
		grenade.Player = player;
		rigidBody.Velocity = dir * force;
		rigidBody.Velocity += playerVelocity;
		gameobject.NetworkSpawn();
	}


	/// <summary>
	/// Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;
		ShootBullet( ray.Position, ray.Forward, force, damage, bulletSize );
	}

	/// <summary>
	/// Shoot a multiple bullets from owners view point
	/// </summary>
	public virtual void ShootBullets( int numBullets, float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;

		for ( int i = 0; i < numBullets; i++ )
		{
			ShootBullet( ray.Position, ray.Forward, force / numBullets, damage, bulletSize,
				Spread * Easing.QuadraticOut( i / (float)numBullets ) );
		}
	}

	public virtual string StatDamage => Damage.ToString();
	public virtual float StatFirerate => PrimaryRate;
	public virtual float StatDPS => MathF.Round( (Damage * MaxAmmo) / ((MaxAmmo / PrimaryRate) + ReloadTime) );

	public class RecoilPattern
	{
		[KeyProperty] public List<Vector2> Points { get; set; } = new();
		[Hide] int currentPoint = 0;

		public Vector2 GetPoint( RealTimeSince lastRecoil, float bulletTime )
		{
			currentPoint = (currentPoint + 1).Clamp( 0, Points.Count - 1 );

			if ( lastRecoil > bulletTime * 1.5f )
				currentPoint = 0;

			return Points[currentPoint];
		}

		public Vector2 Modify = 1;

		[Button]
		void MassModify()
		{
			for ( int i = 0; i < Points.Count; i++ )
			{
				Points[i] *= Modify;
			}
		}
	}
}
