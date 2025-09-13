using Sandbox.Citizen;
using Sandbox.Events;
using Sandbox.Utility;

namespace Seekers;

/// <summary>
///     A common base we can use for weapons so we don't have to implement the logic over and over
///     again. Feel free to not use this and to implement it however you want to.
/// </summary>
public record BulletHitEvent( Vector3 position ) : IGameEvent;

[Icon( "sports_martial_arts" )]
public partial class BaseWeapon : Component
{
	public bool ForceDisableViewmodel;

	/// <summary>
	///     Shoot a single bullet
	/// </summary>
	private List<Surface> hitSurfaces = new();

	//don't know how spaces keep ending up in this string, but it breaks it so whatever is happening needs to stop
	private readonly GameObject muzzle = GameObject.GetPrefab( "weapons/common/effects/muzzle.prefab" );
	private int shots;

	private float shotTime;
	[Feature( "General" )] [Property] public string Name { get; set; }
	[Feature( "General" )] [Property] public ItemResource ItemResource { get; set; }

	[Feature( "General" )]
	[Property]
	[ImageAssetPath]
	public string Icon { get; set; }

	[Feature( "Animation" )] [Property] public WeaponIK LeftIK { get; set; }

	[Feature( "Animation" )]
	[Property]
	public CitizenAnimationHelper.HoldTypes HoldType { get; set; }
		= CitizenAnimationHelper.HoldTypes.HoldItem;

	[Feature( "Animation" )]
	[Property]
	public CitizenAnimationHelper.Hand Handedness { get; set; }
		= CitizenAnimationHelper.Hand.Right;

	[Feature( "Animation" )] [Property] public string ParentBone { get; set; } = "hold_r";
	[Feature( "Animation" )] [Property] public Transform BoneOffset { get; set; } = new(0);

	[Feature( "Models" )] [Property] public GameObject ViewModelPrefab { get; set; }
	[Feature( "Models" )] [Property] public GameObject TracerEffect { get; set; }

	[Feature( "Sounds" )]
	[Property]
	public SoundEvent DeploySound { get; set; } =
		ResourceLibrary.Get<SoundEvent>( "sounds/weapons/switch/switch_3.sound" );

	[Feature( "Sounds" )] [Property] public SoundEvent ReloadSound { get; set; }
	[Feature( "Sounds" )] [Property] public SoundEvent ReloadShortSound { get; set; }
	[Feature( "Sounds" )] [Property] public SoundEvent ShootSound { get; set; }

	[Feature( "Firing" )] [Property] public float PrimaryRate { get; set; } = 5.0f;
	[Feature( "Firing" )] [Property] public float SecondaryRate { get; set; } = 15.0f;
	[Feature( "Firing" )] [Property] public virtual float Damage { get; set; }
	[Feature( "Firing" )] [Property] public virtual float Spread { get; set; }
	[Feature( "Firing" )] [Property] public virtual float SpreadIncrease { get; set; }

	[Property, ShowIf( "UseProjectile", true ), Group( "Projectile" ), Feature( "Firing" )]
	public GameObject Projectile { get; set; }

	[Property, ShowIf( "UseProjectile", true ), Group( "Projectile" ), Feature( "Firing" )]
	public float ProjectileSpeed { get; set; }

	[Feature( "Ammo" )] [Property] public int Ammo { get; set; }
	[Feature( "Ammo" )] [Property] public int MaxAmmo { get; set; }
	[Feature( "Ammo" )] [Property] public bool Chamber { get; set; } = true;
	[Feature( "Ammo" )] [Property] public bool ShowAmmo { get; set; } = true;
	[Feature( "Ammo" )] [Property] public float ReloadTime { get; set; } = 3.0f;

	[Feature( "UI" )] [Property] public CrosshairType CrosshairType { get; set; }

	[Feature( "Weapon Visuals" )][Property] public GameObject MuzzleOverride { get; set; }


	private GameObject Tracer
	{
		get
		{
			if ( TracerEffect.IsValid() )
			{
				return TracerEffect;
			}

			return GameObject.GetPrefab( "/weapons/common/effects/tracer.prefab" );
		}
	}

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

	public virtual bool WantsHideHud => false;

	public PlayerInventory Inventory =>
		GameObject?.Root?.Components.Get<PlayerInventory>( FindMode.EverythingInSelfAndDescendants );

	public RealTimeSince LastShot { get; set; }
	public Angles Recoil { get; set; }
	public virtual float RecoilTime => 0.05f.Clamp( 0, BulletTime );
	public float BulletTime => 1 / PrimaryRate;

	public virtual bool UseProjectile => false;

	[ConVar( ConVarFlags.Saved )] public static bool bolt_tracers { get; set; } = true;

	public virtual string StatDamage => Damage.ToString();
	public virtual float StatFirerate => PrimaryRate;
	public virtual float StatDPS => MathF.Round( Damage * MaxAmmo / (MaxAmmo / PrimaryRate + ReloadTime) );

	public Transform Attachment( string name )
	{
		if ( LocalWorldModel == ViewModel?.Renderer )
		{
			return ViewModel.GetAttachment( name );
		}

		if ( name == "muzzle" && MuzzleOverride.IsValid() )
			return MuzzleOverride.WorldTransform;

		return LocalWorldModel?.GetAttachment( name ) ?? WorldTransform;
	}

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

	public static bool IsNearby( Vector3 position )
	{
		if ( !Game.ActiveScene.Camera.IsValid() )
		{
			return false;
		}

		return position.DistanceSquared( Game.ActiveScene.Camera.WorldPosition ) < 4194304f;
	}

	[Rpc.Broadcast]
	protected void DoTracer( Vector3 startPosition, Vector3 endPosition, float distance, bool muzzle )
	{
		if ( !bolt_tracers )
		{
			return;
		}

		if ( !IsNearby( startPosition ) && !IsNearby( endPosition ) )
		{
			return;
		}

		var attachment = LocalWorldModel.GetAttachment( "muzzle" );

		var origin = LocalWorldModel.GetAttachment( "muzzle" ).GetValueOrDefault();

		var effect =
			Tracer?.Clone( new CloneConfig
			{
				Transform = new Transform().WithPosition( origin.Position ), StartEnabled = true
			} );
		if ( effect.IsValid() && effect.GetComponentInChildren<BeamEffect>() is { } tracer )
		{
			tracer.TargetPosition = endPosition;
		}
	}

	protected void CalculateRandomRecoil( (float min, float max) pitchRecoil, (float min, float max) yawRecoil )
	{
		Recoil = new Angles( -MathF.Abs( RandomRecoilValue( pitchRecoil.min, pitchRecoil.max ) ),
			RandomRecoilValue( yawRecoil.min, yawRecoil.max ), 0 );
		LastShot = 0;
	}

	protected void AddRecoil( Vector2 recoil )
	{
		Recoil = new Angles( -recoil.y, -recoil.x, 0 );
		LastShot = 0;
	}

	private float RandomRecoilValue( float min, float max )
	{
		return Game.Random.Next( (int)(min * 10), (int)(max * 10) ) * 0.1f *
		       (Game.Random.Next( 0, 2 ) == 1 ? -1 : 1);
	}

	public void SetIk( GameObject target, bool active, bool left )
	{
		if ( !Owner?.Controller?.AnimationHelper.IsValid() ?? true )
		{
			return;
		}

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
		ResetBodyGroups();

		if ( MergeModel &&
		     GameObject.Components.TryGet<SkinnedModelRenderer>( out var skinnedModel,
			     FindMode.EnabledInSelfAndChildren ) )
		{
			SkinMergeModel( skinnedModel );
		}

		LeftIK.Active = true;

		TimeSinceDeployed = 0;

		BroadcastEnabled();

		if ( IsProxy )
		{
			return;
		}

		IsReloading = false;

		if ( !Owner.IsValid() )
		{
			return;
		}

		Owner.Zoom = 1;

		var go = ViewModelPrefab?.Clone( new CloneConfig
		{
			StartEnabled = false,
			Parent = Owner.Controller.Camera.GameObject,
			Transform = Owner.Controller.WorldTransform
		} );

		go.NetworkSpawn();
	}

	[Rpc.Broadcast]
	private void ResetBodyGroups( bool useRemoves = true )
	{
		if ( !Owner.IsValid() )
			return;

		if ( !GameObject.IsValid() )
			return;

		if ( GameObject.Root.Components.TryGet<PlayerDresser>( out var dresser, FindMode.EnabledInSelfAndChildren ) )
		{
			dresser.DisableClothingGroups( useRemoves ? RemoveGroups : null,
				useRemoves ? RemoveClothingCategories : null );
		}
	}


	private void SkinMergeModel( SkinnedModelRenderer skinnedModel )
	{
		if ( !(Owner?.Controller?.BodyModelRenderer.IsValid() ?? false) )
		{
			return;
		}

		var clothingData = Network.Owner.GetUserData( "avatar" );
		var container = new ClothingContainer();
		container.Deserialize( clothingData );

		var femaleModel = Model.Load( "models/citizen_human/citizen_human_female.vmdl" );

		if ( FemaleReplacement != null && Owner.Controller.BodyModelRenderer.Model == femaleModel )
		{
			skinnedModel.Model = FemaleReplacement;
		}

		skinnedModel.MaterialGroup = Owner.Controller.BodyModelRenderer.MaterialGroup;

		skinnedModel.BoneMergeTarget = Owner.Controller.BodyModelRenderer;
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	private void BroadcastEnabled()
	{
		var owner = Owner;
		if ( !owner.IsValid() )
			return;

		var renderer = owner?.Renderer;

		if ( !renderer.IsValid() )
			return;

		renderer?.Set( "b_deploy", true );

		SoundExtensions.BroadcastSound( DeploySound, WorldPosition,
			spacialBlend: Owner.IsValid() && Owner.IsMe ? 0 : 1 );
	}

	protected override void OnDisabled()
	{
		ResetBodyGroups( false );

		Owner?.Renderer?.Set( "holdtype", 0 );

		if ( IsProxy )
		{
			return;
		}

		ViewModel?.GameObject?.BroadcastDestroy();
	}

	protected override void OnUpdate()
	{
		GameObject.NetworkInterpolation = false;

		if ( !Owner.IsValid() )
		{
			return;
		}

		if ( !Owner.Controller.IsValid() )
		{
			return;
		}

		if ( !Owner.Inventory.IsValid() )
		{
			return;
		}

		Owner?.Renderer?.Set( "holdtype", (int)HoldType );
		Owner?.Renderer?.Set( "holdtype_handedness", (int)Handedness );

		if ( ViewModel.IsValid() )
		{
			ViewModel.GameObject.Enabled = !(Owner?.Controller?.BodyVisible ?? true);
			if ( ForceDisableViewmodel )
			{
				ViewModel.GameObject.Enabled = false;
			}
		}

		if ( WorldModel.IsValid() )
		{
			WorldModel.RenderType = !Owner?.Controller?.BodyVisible ?? false
				? ModelRenderer.ShadowRenderType.ShadowsOnly
				: ModelRenderer.ShadowRenderType.On;
		}

		if (DisableInFP != null)
		{
			foreach ( var model in DisableInFP )
			{
				model.Enabled = Owner?.Controller?.BodyVisible ?? false;
			}
		}
		

		if ( IsProxy )
		{
			return;
		}

		if ( LastShot < RecoilTime && Owner.IsValid() )
		{
			Owner.Controller.EyeAngles += Recoil * Time.Delta / RecoilTime;
		}

		Owner.Controller.Tags.Set( "viewer",
			Owner.Controller.ThirdPerson );

		Owner.Controller.IgnoreMove = false;
		Owner.Controller.IgnoreCam = false;

		Owner.Inventory.CanChange = true;

		Owner.CanUse = true;

		OnControl();
	}

	public virtual void OnControl()
	{
		if ( TimeSinceDeployed < 0.6f )
		{
			return;
		}

		if ( IsProxy )
		{
			return;
		}

		if ( !IsReloading && CanReload() )
		{
			Reload();
		}

		//
		// Reload could have changed our owner
		//
		if ( !Owner.IsValid() )
		{
			return;
		}

		var canPrimaryAttack = CanPrimaryAttack();
		if ( canPrimaryAttack && Ammo > 0 )
		{
			TimeSincePrimaryAttack = 0;
			AttackPrimary();
		}
		else if ( canPrimaryAttack && Input.Pressed( "attack1" ) )
		{
			AttackDry();
		}

		//
		// AttackPrimary could have changed our owner
		//
		if ( !Owner.IsValid() )
		{
			return;
		}

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
		var add = Chamber ? MaxAmmo - 1 : MaxAmmo;
		Ammo = Math.Clamp( Ammo + add, 0, MaxAmmo );
		IsReloading = false;
	}

	public virtual void StartReloadEffects()
	{
		ViewModel?.Set( "b_reload", true );
		SoundExtensions.FollowSound( Ammo > 0 ? ReloadShortSound : ReloadSound, GameObject );
	}

	protected virtual void ShootEffects()
	{
		AttachParticleSystem( muzzle, "muzzle" );

		ViewModel?.Set( "fire", true );
	}

	[Rpc.Broadcast]
	public void AttachParticleSystem( GameObject prefab, string attachment, float time = 1, GameObject parent = null )
	{
		if ( !prefab.IsValid() || !LocalWorldModel.IsValid() )
		{
			return;
		}

		var transform = LocalWorldModel?.GetAttachment( attachment ) ?? WorldTransform;

		Particles.MakeParticleSystem( prefab, transform, time, LocalWorldModel?.GameObject );
	}

	[Rpc.Broadcast]
	public void AttachEjectedBullet( GameObject prefab, string attachment, Vector3 force, float time = 1,
		GameObject parent = null )
	{
		var transform = LocalWorldModel?.GetAttachment( attachment ) ?? WorldTransform;

		var go = Particles.MakeParticleSystem( prefab, transform, time, parent );

		go?.GetComponent<Rigidbody>()
			?.ApplyForce( transform.Forward * force.x + transform.Right * force.y + transform.Up * force.z );
		go?.GetComponent<Rigidbody>()
			?.ApplyTorque( transform.Forward * force.x + transform.Right * force.y + transform.Up * force.z );
	}

	public virtual bool CanReload()
	{
		if ( Owner != null && Input.Down( "reload" ) )
		{
			return true;
		}

		return false;
	}

	public virtual void Reload()
	{
		if ( IsReloading )
		{
			return;
		}

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
		if ( Owner == null || !Input.Down( "attack1" ) || IsReloading )
		{
			return false;
		}

		var rate = PrimaryRate;
		if ( rate <= 0 )
		{
			return true;
		}

		return TimeSincePrimaryAttack > 1 / rate;
	}

	public virtual void AttackPrimary()
	{
		SoundExtensions.BroadcastSound( ShootSound, WorldPosition,
			ShootSound.Volume.FixedValue, spacialBlend: Owner.IsValid() && Owner.IsMe ? 0 : 1 );
	}

	public virtual void AttackDry()
	{
		ViewModel?.Set( "b_attack_dry", true );
		SoundExtensions.BroadcastSound( "gun_dryfire", WorldPosition,
			spacialBlend: Owner.IsValid() && Owner.IsMe ? 0 : 1 );

		Owner?.Renderer?.SetParamNet( "b_attack_dry", true );
	}

	public virtual bool CanSecondaryAttack()
	{
		if ( Owner == null || !Input.Down( "attack2" ) || IsReloading )
		{
			return false;
		}

		var rate = SecondaryRate;
		if ( rate <= 0 )
		{
			return true;
		}

		return TimeSinceSecondaryAttack > 1 / rate;
	}

	public virtual void AttackSecondary()
	{
	}

	/// <summary>
	///     Does a trace from start to end, does bullet impact effects. Coded as an IEnumerable so you can return multiple
	///     hits, like if you're going through layers or ricocheting or something.
	/// </summary>
	public static IEnumerable<SceneTraceResult> TraceBullet( GameObject ignore, Vector3 start, Vector3 end,
		float radius = 2.0f )
	{
		// bool underWater = Trace.TestPoint( start, "water" );

		var trace = Game.ActiveScene.Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement", "ignorebullets" )
			.IgnoreGameObjectHierarchy( ignore )
			.Size( radius );

		var triggerHitboxTrace = Game.ActiveScene.Trace.Ray( start, end )
			.HitTriggersOnly()
			.WithTag( "triggerhitbox" );

		//
		// If we're not underwater then we can hit water
		//
		/*
		if ( !underWater )
			trace = trace.WithAnyTags( "water" );
		*/

		var tr = trace.Run();
		var ttr = triggerHitboxTrace.Run();

		if ( ttr.Hit && !tr.Hit )
		{
			yield return ttr;
		}

		else if ( ttr.Hit && ttr.Distance < tr.Distance )
		{
			yield return ttr;
		}

		else if ( tr.Hit )
		{
			yield return tr;
		}

		//
		// Another trace, bullet going through thin material, penetrating water surface?
		//
	}

	public IEnumerable<SceneTraceResult> TraceMelee( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var trace = Scene.Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "player", "npc", "glass" )
			.WithoutTags( "playercontroller", "debris", "movement", "ignorebullets" )
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

	public virtual void ShootBullet( Vector3 pos, Vector3 dir, float force, float damage, float bulletSize,
		float spreadOverride = -1 )
	{
		if ( shotTime != Time.Now )
		{
			shots = 0;
			hitSurfaces = new List<Surface>();
		}

		shotTime = Time.Now;

		shots++;

		var spread = Spread + SpreadIncrease;
		if ( spreadOverride > -1 )
		{
			spread = spreadOverride;
		}

		var forward = dir;
		forward += (Vector3.Random + Vector3.Random + Vector3.Random + Vector3.Random) * spread * 0.25f;
		forward = forward.Normal;

		if ( UseProjectile )
		{
			var projectile = Projectile.Clone().GetComponent<BaseProjectile>();

			var attachment = Attachment( "muzzle" );

			projectile.WorldPosition = attachment.Position;

			projectile.WorldRotation = Rotation.LookAt( forward );

			projectile.Speed = Owner.Controller.Velocity + forward * ProjectileSpeed;

			projectile.Damage = damage;

			projectile.Origin = GameObject;

			projectile.OriginLocalPos = WorldTransform.PointToLocal( attachment.Position );

			projectile.GameObject.NetworkSpawn();
			return;
		}

			//
			// ShootBullet is coded in a way where we can have bullets pass through shit
			// or bounce off shit, in which case it'll return multiple results
			//
		foreach ( var trace in TraceBullet( GameObject.Root, pos, pos + forward * 5000, bulletSize ) )
		{
			var tr = trace;

			if ( Owner?.Controller?.ThirdPerson ?? false )
			{
				var headCheck = TraceBullet( GameObject.Root, Owner.Controller.Head.WorldPosition, tr.HitPosition );
				var sceneTraceResults = headCheck.ToList();
				if ( sceneTraceResults.Any() )
				{
					tr = sceneTraceResults.Last();
				}
			}

			var tagMaterial = "";

			if ( tr.Tags.Any() )
			{
				foreach ( var tag in tr.Tags )
				{
					if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
					{
						tagMaterial = tag.Remove( 0, 2 );
						break;
					}
				}
			}

			var surface = tagMaterial == ""
				? tr.Surface
				: Surface.FindByName( tagMaterial ) ?? tr.Surface;

			surface.DoBulletImpact( tr, !hitSurfaces.Contains( surface ) || shots < 3 );
			DoTracer( tr.StartPosition, tr.EndPosition, tr.Distance, true );

			hitSurfaces.Add( surface );

			if ( !tr.GameObject.IsValid() )
			{
				continue;
			}

			var hitboxTags = tr.GetHitboxTags();

			var calcForce = forward * 25000 * damage;

			DoDamage( tr.GameObject, damage, calcForce, tr.HitPosition, hitboxTags, Owner, this );
		}
	}

	public static void DoDamage( GameObject gameObject, float damage, Vector3 calcForce, Vector3 hitPosition,
		HitboxTags hitboxTags = default, Pawn owner = null, Component inflictor = null, Team ownerTeam = null,
		Component attacker = null )
	{
		if ( attacker == null )
			attacker = owner;

		Game.ActiveScene.Dispatch( new BulletHitEvent( hitPosition ) );

		if ( gameObject.Components.TryGet<PropHelper>( out var prop ) )
		{
			prop.AddDamage( damage );
		}

		if ( gameObject.Components.TryGet<Rigidbody>( out var rb ) )
		{
			KnockBack( gameObject, calcForce );
		}

		if ( !ownerTeam.IsValid() && owner.IsValid() )
		{
			ownerTeam = owner.Team;
		}

		if ( !ownerTeam.IsValid() )
		{
			return;
		}


		if ( gameObject.Root.Components.TryGet<HealthComponent>( out var player,
			    FindMode.EverythingInSelfAndChildren ) )
		{
			if ( !ownerTeam.FriendlyFire && gameObject.Tags.Has( "player" ) )
			{
				var team = player.GameObject.Root.GetComponent<Pawn>()?.Team ?? null;
				if ( team == ownerTeam || ownerTeam.Friends.Contains( team ) )
				{
					return;
				}
			}

			player.TakeDamage( attacker, damage, inflictor, hitPosition, calcForce, hitboxTags );
			if ( owner.IsValid() )
			{
				Crosshair.Instance.Trigger( player, damage, hitboxTags );
			}
		}
	}

	[Rpc.Host]
	public static void KnockBack( GameObject gameObject, Vector3 force )
	{
		gameObject?.GetComponent<Rigidbody>()?.BroadcastApplyForce( force );
	}

	public void ThrowGrenade( GameObject projectile, float force, float distance = 20 )
	{
		var ray = Owner.AimRay;
		SpawnGrenade( Client.Local.GetPawn<Pawn>(), ray.Position, ray.Forward, projectile, force, distance,
			Owner?.Controller?.Velocity ?? Vector3.Zero );
	}

	[Rpc.Broadcast]
	private void SpawnGrenade( Pawn player, Vector3 pos, Vector3 dir, GameObject projectile, float force,
		float distance, Vector3 playerVelocity )
	{
		if ( !Networking.IsHost )
		{
			return;
		}

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
	///     Shoot a single bullet from owners view point
	/// </summary>
	public virtual void ShootBullet( float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;
		ShootBullet( ray.Position, ray.Forward, force, damage, bulletSize );
	}

	/// <summary>
	///     Shoot a multiple bullets from owners view point
	/// </summary>
	public virtual void ShootBullets( int numBullets, float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;

		for ( var i = 0; i < numBullets; i++ )
		{
			ShootBullet( ray.Position, ray.Forward, force / numBullets, damage, bulletSize,
				Spread * Easing.QuadraticOut( i / (float)numBullets ) );
		}
	}

	/// <summary>
	/// Called when setting up the camera - use this to apply effects on the camera based on this carriable
	/// </summary>
	/// <param name="pawn"></param>
	/// <param name="camera"></param>
	public virtual void OnCameraSetup( Pawn pawn, Sandbox.CameraComponent camera )
	{
	}

	/// <summary>
	/// Can directly influence the player's eye angles here
	/// </summary>
	/// <param name="pawn"></param>
	/// <param name="angles"></param>
	public virtual void OnCameraMove( Pawn pawn, ref Angles angles )
	{
	}


	public class WeaponIK
	{
		private bool _active = true;
		public Action<GameObject, bool, bool> SetActive;

		public bool Active
		{
			get => _active;
			set
			{
				_active = value;
				SetActive?.Invoke( GameObject, value, IsLeft );
			}
		}

		[KeyProperty] public GameObject GameObject { get; set; }
		public bool IsLeft { get; set; }
	}

	public class RecoilPattern
	{
		[Hide] private int currentPoint;

		public Vector2 Modify = 1;
		[KeyProperty] public List<Vector2> Points { get; set; } = new();

		public Vector2 GetPoint( RealTimeSince lastRecoil, float bulletTime )
		{
			currentPoint = (currentPoint + 1).Clamp( 0, Points.Count - 1 );

			if ( lastRecoil > bulletTime * 1.5f )
			{
				currentPoint = 0;
			}

			return Points[currentPoint];
		}

		[Button]
		private void MassModify()
		{
			for ( var i = 0; i < Points.Count; i++ )
			{
				Points[i] *= Modify;
			}
		}
	}
}
