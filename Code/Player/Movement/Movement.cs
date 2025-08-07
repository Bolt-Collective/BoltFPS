using Sandbox;
using Sandbox.VR;
using Seekers;
using System;

public abstract partial class Movement : Component, IScenePhysicsEvents
{
	[Group( "Movement Variables" )]
	[Property]
	public float MaxSpeed { get; set; } = 320f;

	[Group( "Movement Variables" )]
	[Property]
	public float MaxAirSpeed { get; set; } = 30f;

	[Group( "Movement Variables" )]
	[Property]
	public float MaxAccelM { get; set; } = 10f;

	[Group( "Movement Variables" )]
	[Property]
	public float MaxAirAccelM { get; set; } = 20f;

	[Group( "Movement Variables" )]
	[Property]
	public float Friction { get; set; } = 6f;

	[Group( "Movement Variables" )]
	[Property]
	public float JumpPower { get; set; } = 268.3281572999747f;

	[Group( "Movement Variables" )]
	[Property]
	public float Gravity { get; set; } = 600f;

	[Property, Group( "Camera Variables" )]
	public bool ThirdPerson { get; set; } = false;

	[Property, Group( "Camera Variables" )]
	public Vector3 ThirdPersonCameraOffset { get; set; } = new Vector3( -180, -20, 0 );

	[Property, Group( "Camera Variables" )]
	public GameObject Head { get; set; }

	[Property, Group( "Camera Variables" )]
	public CameraComponent Camera { get; set; }

	[Property, Group( "Camera Variables" )]
	public ScreenShaker ScreenShaker { get; set; }

	public Vector3 wishDirection;

	private Vector3 lastVel;

	private static bool AutoBH;

	[ConCmd( "toggle.bhop" )]
	public static void ToggleBH()
	{
		AutoBH = !AutoBH;
	}

	private float _startHeight;

	protected override void OnStart()
	{
		Camera.Enabled = false;
		Camera.FieldOfView = Preferences.FieldOfView;

		_startHeight = Height;
	}

	public bool OverrideVelocity;

	public virtual void PreMove()
	{
	}

	[Sync] public Vector3 WishVelocity { get; set; }

	public void BuildWishVelocity()
	{
		var rot = EyeAngles.WithPitch( 0f ).ToRotation();

		var wishDirection = Input.AnalogMove.Normal * rot;
		wishDirection = wishDirection.WithZ( 0 );

		WishVelocity = wishDirection * MaxSpeed;
	}

	protected override void OnUpdate()
	{

		Animate();

		UpdateBodyVisibility();

		Camera.Enabled = !IsProxy;

		if ( IsProxy )
			return;

		BuildWishVelocity();

		LadderLatchCheck();

		UpdateCamera();

		CheckLadder();

		if ( IsTouchingLadder && !OverrideVelocity )
		{
			LadderMove();
			return;
		}

		GetWishDirection();

		if ( !OverrideVelocity )
			WalkMove();

		PreMove();

		Move();
	}

	protected override void OnFixedUpdate()
	{
		SetCollisionBox();
	}

	public Action OnJump;


	public void WalkMove()
	{
		ApplyHalfGravity();

		if ( IsGrounded )
			GroundVelocity();
		else
			AirVelocity();

		if ( (Input.Pressed( "Jump" ) || (AutoBH && Input.Down( "Jump" ))) && IsGrounded )
		{
			OnJump?.Invoke();
			LaunchUpwards( JumpPower );
		}

		ApplyHalfGravity();
	}

	public void ApplyHalfGravity()
	{
		if ( IsGrounded )
			return;

		Velocity += Vector3.Down * Gravity / 2 * Time.Delta;
	}

	public Vector3 forwardDirection => Scene.Camera?.WorldTransform.Forward.WithZ( 0 ).Normal ?? default;
	public Vector3 rightDirection => Scene.Camera?.WorldTransform.Right.WithZ( 0 ).Normal ?? default;

	private float _cameraBobTime;
	public float CameraBobFrequency = 8f;
	public float CameraBobAmplitude = 2f;
	public bool IsBobbing;

	private Vector3 _cameraBobOffset;

	[Sync] public Angles EyeAngles { get; set; }

	public Vector3 CameraPosOffset;
	public Angles CameraRotOffset;

	public bool IgnoreMove { get; set; }

	public bool IgnoreCam { get; set; }

	public float AimSensitivityScale = 1;

	public virtual void UpdateCamera()
	{
		if ( !Camera.IsValid() )
			return;

		if ( !IgnoreCam )
			EyeAngles += Input.AnalogLook * AimSensitivityScale;

		EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -89f, 89f ) );

		var targetTransform = new Transform( CameraPosition(), CameraRotation() );

		Head.WorldTransform = targetTransform;

		if (ThirdPerson)
		{
			var trace = Scene.Trace.Ray( Head.WorldPosition, Head.WorldTransform.PointToWorld( ThirdPersonCameraOffset ) ).IgnoreGameObjectHierarchy( GameObject ). Radius(5).Run();
			Camera.WorldPosition = trace.EndPosition;
		}
		else
			Camera.LocalPosition = 0;
	}

	public void LookAt( Vector3 worldTarget )
	{
		var worldDirection = (worldTarget - Head.WorldPosition).Normal;

		EyeAngles = Rotation.LookAt( worldDirection );

		EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -89f, 89f ) );
	}

	public virtual Vector3 CameraPosition()
	{
		return WorldPosition + Vector3.Up * (Height - 2);
	}

	public virtual Rotation CameraRotation()
	{
		return EyeAngles;
	}

	public virtual void GetWishDirection()
	{
		if ( IgnoreMove )
		{
			wishDirection = 0;
			return;
		}

		var dir = Input.AnalogMove.Normal;

		wishDirection = dir.x * forwardDirection + dir.y * -rightDirection;
	}

	public virtual void GroundVelocity()
	{
		ApplyFriction( Friction );

		AddSpeed( MaxSpeed, MaxAccelM );
	}

	public void AddSpeed( float maxSpeed, float maxAccel )
	{
		var currentSpeed = Vector3.Dot( Velocity, wishDirection );
		var addSpeed = (maxSpeed - currentSpeed).Clamp( 0, maxAccel * maxSpeed * Time.Delta );

		Velocity += addSpeed * wishDirection;
	}

	public virtual void AirVelocity()
	{
		AddSpeed( MaxAirSpeed, MaxAirAccelM );
	}

	public void ApplyFriction( float friction )
	{
		var speed = Velocity.Length;

		float newspeed = 0f;

		float drop = speed * friction * Time.Delta;


		newspeed = speed - drop;
		if ( newspeed < 0 )
		{
			newspeed = 0;
		}

		if ( speed > 0 )
			newspeed /= speed;

		Velocity *= newspeed;
	}
}
