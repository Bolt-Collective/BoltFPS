using Sandbox.Movement;
using Sandbox.Rendering;
using Sandbox.Utility;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Seekers;

[Spawnable, Library( "weapon_sniper" )]
public partial class Sniper : BaseWeapon, Component.ICollisionListener
{
	public bool Scoped;
	[Feature( "Scope" ), Property] public Material ScopeOverlay { get; set; }
	[Feature( "Sounds" ), Property] public SoundEvent ZoomSound { get; set; }
	[Feature( "Sounds" ), Property] public SoundEvent UnzoomSound { get; set; }
	[Feature( "Firing" ), Property] public override float Damage { get; set; } = 50f;
	[Feature( "Firing" ), Property] public float MinRecoil { get; set; } = 0.5f;
	[Feature( "Firing" ), Property] public float MaxRecoil { get; set; } = 1f;
	[Feature( "Firing" ), Property] public float SpreadMult { get; set; } = 0.2f;
	[Feature( "Scope" ), Property] public float Zoom { get; set; } = 4f;
	[Feature( "Scope" ), Property] public float SensMod { get; set; } = 0.25f;
	public RealTimeSince TimeSinceDischarge { get; set; }

	CommandList scopeCommandList;

	private int ZoomLevel { get; set; } = 0;
	[Feature( "Scope" ), Property] private float BlurLerp { get; set; } = 1.0f;

	private Angles LastAngles;

	private Angles AnglesLerp;

	public Vector3 AimPosOverride;
	public Vector3 AimDirectionOverride;

	[Feature( "Scope" ), Property] private float AngleOffsetScale { get; set; } = 0.01f;

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( "attack1" );
	}

	public override void AttackPrimary()
	{
		base.AttackPrimary();

		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		CalculateRandomRecoil( (MinRecoil, MaxRecoil), (MinRecoil / 2, MaxRecoil / 2) );

		AnglesLerp -= new Angles( Recoil.x, Recoil.y, 0 );

		BroadcastAttackPrimary();

		ViewModel?.Set( "b_attack", true );
		Ammo--;

		ShootEffects();
		ShootBullet( 1.5f, Damage, 0.25f );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || !Enabled )
			return;

		if ( !(Owner?.Controller.IsValid() ?? false) )
			return;

		if ( !Scoped && Input.Pressed( "attack2" ) && Owner.IsValid() && !Owner.GrabbedObject.IsValid() )
			Scope();
		if ( Scoped && !scopingIn && (Input.Released( "attack2" ) || Owner.Controller.IsSprinting ||
		                              (!Owner.Controller.IsGrounded &&
		                               Owner.Controller.MoveMode != NormalMovement.MoveModes.NoClip) || IsReloading) )
			UnScope();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy || !GameObject.Enabled || !Enabled )
			return;
	}

	public override void OnCameraMove( Pawn pawn, ref Angles angles )
	{
		if ( !(Owner?.Controller.IsValid() ?? false) )
			return;

		float velocity = Owner.Controller.Velocity.Length / 25.0f;
		float blur = 1.0f / (velocity + 1.0f);
		blur = MathX.Clamp( blur, 0.1f, 1.0f );

		if ( !Owner.Controller.IsGrounded && Owner.Controller.MoveMode != NormalMovement.MoveModes.NoClip )
			blur = 0.1f;

		SpreadIncrease = (Scoped ? 1 - blur : 1) * SpreadMult;

		BlurLerp = blur;

		var delta = angles - LastAngles;


		AnglesLerp = AnglesLerp.LerpTo( delta, Time.Delta * 10.0f );
		LastAngles = angles;

		if ( Scoped && ScopeOverlay is not null )
		{
			if ( scopeCommandList != null )
			{
				Scene?.Camera?.RemoveCommandList( scopeCommandList );
				scopeCommandList = null;
			}

			scopeCommandList = new CommandList( "SniperScope" );
			scopeCommandList?.Attributes.Set( "BlurAmount",
				Easing.EaseOut( Normalize( BlurLerp, 0.5f, 1 ).Clamp( 0, 1 ) ).Clamp( 0.1f, 1f ) );
			scopeCommandList?.Attributes.Set( "Offset",
				new Vector2( AnglesLerp.yaw, -AnglesLerp.pitch ) * AngleOffsetScale );
			scopeCommandList?.Blit( ScopeOverlay );
			Scene?.Camera?.AddCommandList( scopeCommandList, Stage.AfterTransparent, 100 );
		}
	}

	bool scopingIn = false;

	public override void ShootBullet( float force, float damage, float bulletSize )
	{
		var ray = Owner.AimRay;
		if ( Scoped && (ViewModel?.ScopePoint.IsValid() ?? false) )
		{
			Vector2 screenPosA = Scene.Camera.PointToScreenNormal( ViewModel.ScopePoint.WorldPosition );

			// Step 2: unproject same screen position using Camera B
			ray = ViewModel.ScopePoint.CameraComponent.ScreenNormalToRay( screenPosA );
		}
		ShootBullet( ray.Position, ray.Forward, force, damage, bulletSize );
	}

	public async void Scope()
	{
		ViewModel.Set( "ironsights", 1 );
		if(ZoomSound.IsValid())
			SoundExtensions.BroadcastSound( ZoomSound.ResourceName, WorldPosition );

		Scoped = true;
		scopingIn = true;
		await GameTask.DelaySeconds( 0.125f );
		scopingIn = false;

		BlurLerp = 1;

		if ( !Owner.IsValid() )
			return;

		Owner.Zoom = Zoom;
		Owner.SensMod = SensMod;

		if (ScopeOverlay.IsValid())
			ForceDisableViewmodel = true;
	}

	float easeOutCirc( float x )
	{
		return 1 - MathF.Sqrt( 1 - MathF.Pow( x, 2 ) );
	}


	public static float Normalize( float value, float min, float max )
	{
		if ( max == min ) return 0f;
		return (value - min) / (max - min);
	}


	public async void UnScope()
	{
		Scene?.Camera?.RemoveCommandList( scopeCommandList );
		scopeCommandList = null;
		Scoped = false;
		AnglesLerp = new Angles();

		Owner.Zoom = 1;
		Owner.SensMod = 1;

		ForceDisableViewmodel = false;

		await Task.Frame();
		ViewModel?.Set( "ironsights", 0 );

		if ( IsReloading )
			ViewModel?.Set( "b_reload", true );
	}

	[Rpc.Broadcast]
	private void BroadcastAttackPrimary()
	{
		Owner?.Renderer?.Set( "b_attack", true );
	}
}
