using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Seekers;

[Spawnable, Library( "weapon_sniper" )]
partial class Sniper : BaseWeapon, Component.ICollisionListener
{
	public bool Scoped;
	[Property] public Material ScopeOverlay { get; set; }
	[Property] public SoundEvent ZoomSound { get; set; }
	[Property] public SoundEvent UnzoomSound { get; set; }
	[Property] public override float Damage { get; set; } = 50f;
	[Property] public float MinRecoil { get; set; } = 0.5f;
	[Property] public float MaxRecoil { get; set; } = 1f;
	[Property] public float SpreadMult { get; set; } = 0.2f;
	public RealTimeSince TimeSinceDischarge { get; set; }

	IDisposable renderHook;

	private int ZoomLevel { get; set; } = 0;
	[Property] private float BlurLerp { get; set; } = 1.0f;

	private Angles LastAngles;

	private Angles AnglesLerp;

	[Property] private float AngleOffsetScale { get; set; } = 0.01f;

	public override bool CanPrimaryAttack()
	{
		return base.CanPrimaryAttack() && Input.Pressed( "attack1" );
	}

	public override void AttackPrimary()
	{
		TimeSincePrimaryAttack = 0;
		TimeSinceSecondaryAttack = 0;

		CalculateRandomRecoil( (MinRecoil, MaxRecoil), (MinRecoil / 2, MaxRecoil / 2) );

		AnglesLerp -= Recoil;

		BroadcastAttackPrimary();

		ViewModel?.Set( "b_attack", true );
		Ammo--;

		ShootEffects();
		ShootBullet( 1.5f, Damage, 3.0f );
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy || !Enabled )
			return;

		if ( !Scoped && Input.Pressed( "attack2" ) && Owner.IsValid() && !Owner.GrabbedObject.IsValid() )
			Scope();
		if ( Scoped && !scopingIn && (Input.Released( "attack2" ) || Owner.Controller.IsRunning ||
		                              !Owner.Controller.Controller.IsOnGround || IsReloading) )
			UnScope();
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy || !GameObject.Enabled || !Enabled )
			return;

		if ( !(Owner?.Controller.IsValid() ?? false) )
			return;

		float velocity = Owner.Controller.Controller.Velocity.Length / 25.0f;
		float blur = 1.0f / (velocity + 1.0f);
		blur = MathX.Clamp( blur, 0.1f, 1.0f );

		if ( !Owner.Controller.Controller.IsOnGround )
			blur = 0.1f;

		SpreadIncrease = (Scoped ? 1 - blur : 1) * SpreadMult;

		BlurLerp = blur;

		var angles = Owner.Controller.EyeAngles;
		var delta = angles - LastAngles;

		AnglesLerp = AnglesLerp.LerpTo( delta, Time.Delta * 10.0f );
		LastAngles = angles;
	}

	bool scopingIn = false;

	public async void Scope()
	{
		ViewModel.Set( "ironsights", 1 );
		SoundExtensions.BroadcastSound( ZoomSound.ResourceName, WorldPosition );

		Scoped = true;
		scopingIn = true;
		await Task.DelaySeconds( 0.1f );
		scopingIn = false;
		renderHook?.Dispose();
		renderHook = null;

		if ( ScopeOverlay is not null )
			renderHook = Owner.Controller.Camera.AddHookAfterTransparent( "Scope", 100, RenderEffect );

		BlurLerp = 1;

		Owner.Zoom = 4;

		ForceDisableViewmodel = true;
	}

	public void RenderEffect( SceneCamera camera )
	{
		RenderAttributes attrs = new RenderAttributes();

		attrs.Set( "BlurAmount", easeOutCirc( Normalize( BlurLerp, 0.5f, 1 ).Clamp( 0, 1 ) ).Clamp( 0.1f, 1f ) );
		attrs.Set( "Offset", new Vector2( AnglesLerp.yaw, -AnglesLerp.pitch ) * AngleOffsetScale );

		Graphics.Blit( ScopeOverlay, attrs );
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
		renderHook?.Dispose();
		Scoped = false;
		AnglesLerp = new Angles();

		Owner.Zoom = 1;

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
		var snd = Sound.Play( ShootSound, WorldPosition );
		snd.SpacialBlend = Owner.IsValid() && Owner.IsMe ? 0 : snd.SpacialBlend;;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		renderHook?.Dispose();
	}
}
