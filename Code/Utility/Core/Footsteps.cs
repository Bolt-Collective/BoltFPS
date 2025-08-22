namespace Seekers;

public class Footsteps : Component
{
	[Property, ShowIf( nameof(ObjectBased), false )]
	public Pawn Player { get; set; }

	[Property, ShowIf( nameof(ObjectBased), true )]
	public GameObject Object { get; set; }

	/// <summary>
	/// Draw debug overlay on footsteps
	/// </summary>
	[Property]
	public bool DebugFootsteps { get; set; } = false;

	/// <summary>
	/// Listen to scene model events for footsteps if true
	/// </summary>
	[Property]
	public bool ObjectBased { get; set; } = false;

	/// <summary>
	/// Play prop-like footstep impacts on the ground
	/// </summary>
	[Property]
	public bool ImpactyFootsteps { get; set; } = false;

	[Property, Group( "Frequency" ), ShowIf( nameof(ObjectBased), false )]
	public float CrouchingStepFrequency { get; set; } = 0.5f;

	[Property, Group( "Frequency" ), ShowIf( nameof(ObjectBased), false )]
	public float SprintingStepFrequency { get; set; } = 0.28f;

	[Property, Group( "Frequency" ), ShowIf( nameof(ObjectBased), false )]
	public float WalkingStepFrequency { get; set; } = 0.39f;

	[Property, Group( "Footstep Volume" ), ShowIf( nameof(ObjectBased), false )]
	public float CrouchingStepVolume { get; set; } = 0.25f;

	[Property, Group( "Footstep Volume" ), ShowIf( nameof(ObjectBased), false )]
	public float WalkingStepVolume { get; set; } = 0.4f;

	// New: independent sprinting volume control
	[Property, Group( "Footstep Volume" ), ShowIf( nameof(ObjectBased), false )]
	public float SprintingStepVolume { get; set; } = 0.55f;

	[Property, Group( "Object Footsteps" ), ShowIf( nameof(ObjectBased), true )]
	public float ObjectStepFrequency { get; set; } = 0.4f;

	[Property, Group( "Object Footsteps" ), ShowIf( nameof(ObjectBased), true )]
	public float ObjectStepVolume { get; set; } = 0.5f;

	// New: object speed gating and scaling
	[Property, Group( "Object Footsteps" ), ShowIf( nameof(ObjectBased), true )]
	public float ObjectMinSpeed { get; set; } = 30.0f;

	[Property, Group( "Object Footsteps" ), ShowIf( nameof(ObjectBased), true )]
	public float ObjectMaxSpeed { get; set; } = 400.0f;

	// New: pitch variation controls
	[Property, Group( "Pitch" )] public float MinPitch { get; set; } = 0.95f;

	[Property, Group( "Pitch" )] public float MaxPitch { get; set; } = 1.05f;

	[Property, Group( "Pitch" ), ShowIf( nameof(ObjectBased), false )]
	public float CrouchPitchMultiplier { get; set; } = 0.97f;

	[Property, Group( "Pitch" ), ShowIf( nameof(ObjectBased), false )]
	public float SprintPitchMultiplier { get; set; } = 1.03f;

	// New: landing sounds
	[Property, Group( "Landing" ), ShowIf( nameof(ObjectBased), false )]
	public bool EnableLandingSounds { get; set; } = true;

	[Property, Group( "Landing" ), ShowIf( nameof(ObjectBased), false )]
	public float LandingMinSpeed { get; set; } = 150.0f;

	[Property, Group( "Landing" ), ShowIf( nameof(ObjectBased), false )]
	public float LandingHardSpeed { get; set; } = 300.0f;

	[Property, Group( "Landing" ), ShowIf( nameof(ObjectBased), false )]
	public float LandingCooldown { get; set; } = 0.1f;

	TimeSince _timeSinceStep;
	bool leftFoot = true;

	// Track landing
	bool _wasGrounded = false;
	TimeSince _timeSinceLanding;

	private float GetStepFrequency()
	{
		if ( !Player.IsValid() ) return ObjectStepFrequency;
		if ( Player.Controller.IsCrouching ) return CrouchingStepFrequency;
		if ( Player.Controller.IsSprinting ) return SprintingStepFrequency;
		return WalkingStepFrequency;
	}

	private float GetStepVolume( float velocityFactor = 1.0f )
	{
		if ( !Player.IsValid() ) return ObjectStepVolume;
		if ( Player.Controller.IsCrouching ) return CrouchingStepVolume;
		if ( Player.Controller.IsSprinting ) return SprintingStepVolume * velocityFactor;
		return WalkingStepVolume * velocityFactor;
	}

	private void HandleFootsteps( Vector3 position, Surface surface, GameObject gameObject,
		float volumeMultiplier = 1.0f,
		bool isLocalPlayer = false )
	{
		if ( surface == null || !surface.IsValid() )
			return;

		var tagMaterial = "";

		foreach ( var tag in gameObject.Tags )
		{
			if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
			{
				tagMaterial = tag.Remove( 0, 2 );
				break;
			}
		}

		Surface groundSurface = null;
		try
		{
			groundSurface = tagMaterial == ""
				? surface.ReplaceSurface()
				: (Surface.FindByName( tagMaterial ) ?? surface.ReplaceSurface());
		}
		catch
		{
			groundSurface = surface;
		}

		if ( groundSurface == null || !groundSurface.IsValid() )
		{
			if ( DebugFootsteps )
			{
				DebugOverlay.Text( position, "Missing surface or sound collection",
					size: 14, flags: TextFlag.LeftTop, duration: 10, overlay: true );
			}

			return;
		}

		leftFoot = !leftFoot;
		_timeSinceStep = 0;

		var sound = leftFoot ? groundSurface.SoundCollection.FootLeft : groundSurface.SoundCollection.FootRight;

		if ( ImpactyFootsteps )
		{
			sound = leftFoot
				? groundSurface.SoundCollection.ImpactHard
				: groundSurface.SoundCollection.ImpactSoft;
		}

		if ( sound is null )
		{
			if ( DebugFootsteps )
			{
				DebugOverlay.Sphere( new Sphere( position, GetStepVolume() * volumeMultiplier ),
					duration: 10, color: Color.Orange, overlay: true );
			}

			return;
		}

		float finalVolume = GetStepVolume() * volumeMultiplier;
		// Randomized pitch with state multiplier
		float pitchMul = 1.0f;
		if ( Player != null )
		{
			pitchMul = Player.Controller.IsCrouching
				? CrouchPitchMultiplier
				: (Player.Controller.IsSprinting ? SprintPitchMultiplier : 1.0f);
		}

		float pitch = Game.Random.Float( MinPitch, MaxPitch ) * pitchMul;
		SoundExtensions.BroadcastSound( sound, position, finalVolume, pitch, spacialBlend: isLocalPlayer ? 1 : 0 );

		if ( DebugFootsteps )
		{
			DebugOverlay.Sphere( new Sphere( position, finalVolume ), duration: 10, overlay: true );
			DebugOverlay.Text( position, $"{sound.ResourceName} p:{pitch:0.00}", size: 14, flags: TextFlag.LeftTop,
				duration: 10, overlay: true );
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( ObjectBased )
		{
			ObjectFootsteps();
		}
		else
		{
			PlayerControllerFootsteps();
		}
	}

	private void TryLandingSound()
	{
		if ( !EnableLandingSounds || IsProxy || !Player.IsValid() ) return;
		if ( _timeSinceLanding < LandingCooldown ) return;

		var tr = Scene.Trace
			.Ray( Player.WorldPosition + Vector3.Up * 20, Player.WorldPosition + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit || tr.Surface == null ) return;

		float verticalSpeed = -Player.Controller.Velocity.z; // positive when falling
		if ( verticalSpeed < LandingMinSpeed ) return;

		Surface groundSurface = null;
		try { groundSurface = tr.Surface.ReplaceSurface(); }
		catch { groundSurface = tr.Surface; }

		if ( groundSurface == null || !groundSurface.IsValid() ) return;

		var impactSound = groundSurface.SoundCollection.FootLand;

		if ( impactSound is null ) return;

		float norm = verticalSpeed.Remap( LandingMinSpeed, LandingHardSpeed * 1.5f, 0.4f, 1.0f ).Clamp( 0.0f, 1.0f );
		float pitch = Game.Random.Float( MinPitch, MaxPitch ) * (verticalSpeed >= LandingHardSpeed ? 0.98f : 1.02f);
		SoundExtensions.BroadcastSound( impactSound, tr.HitPosition, GetStepVolume() * norm, pitch,
			spacialBlend: Player.IsMe ? 1 : 0 );

		if ( DebugFootsteps )
		{
			DebugOverlay.Text( tr.HitPosition, $"Landing v:{verticalSpeed:0} vol:{norm:0.00}", size: 14,
				flags: TextFlag.LeftTop, duration: 5, overlay: true );
		}

		_timeSinceLanding = 0;
	}

	private void PlayerControllerFootsteps()
	{

		if ( !Player.IsValid() ) return;

		bool grounded = Player.Controller.IsGrounded;
		if ( grounded && !_wasGrounded )
		{
			TryLandingSound();
		}

		_wasGrounded = grounded;

		if ( !grounded )
			return;

		if ( _timeSinceStep < GetStepFrequency() )
			return;

		if ( Player.Controller.Velocity.Length < 1.0f )
			return;

		float velocityFactor = Player.Controller.WishVelocity.Length.Remap( 0, 400, 0, 1 );
		HandleFootsteps( Player.WorldPosition, Player.Controller.GroundSurface, Player.Controller.GroundObject,
			velocityFactor, Player.IsMe );
	}

	private void ObjectFootsteps()
	{
		if ( !Object.IsValid() ) return;

		// Determine object speed if possible
		float speed = 0.0f;
		var rb = Object.Components.Get<Rigidbody>();
		if ( rb.IsValid() )
		{
			speed = rb.Velocity.Length;
		}

		// Gate by speed so stationary objects don't tap
		if ( speed < ObjectMinSpeed )
			return;

		// Speed-normalized frequency: faster objects step more often
		float speedNorm = speed.Remap( ObjectMinSpeed, ObjectMaxSpeed, 0.0f, 1.0f ).Clamp( 0.0f, 1.0f );
		float effectiveFreq = ObjectStepFrequency * (1.0f - 0.5f * speedNorm); // up to 50% faster when fast
		if ( _timeSinceStep < effectiveFreq )
			return;

		var tr = Scene.Trace
			.Ray( Object.WorldPosition + Vector3.Up * 20, Object.WorldPosition + Vector3.Up * -20 )
			.Run();

		if ( !tr.Hit || !tr.Surface.IsValid() )
			return;

		// Volume scales gently with speed
		float volMul = speedNorm.LerpInverse( 0.0f, 1.0f );
		HandleFootsteps( tr.HitPosition, tr.Surface, tr.GameObject, 0.75f + 0.5f * volMul, false );
	}
}
