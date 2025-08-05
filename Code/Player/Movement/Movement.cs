using Sandbox;
using Sandbox.VR;
using System;

public abstract partial class Movement : Component
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
	public float Friction { get; set; } = 6f;

	[Group( "Movement Variables" )]
	[Property]
	public float JumpPower { get; set; } = 268.3281572999747f;

	[Group( "Movement Variables" )]
	[Property]
	public float Gravity { get; set; } = 600f;

	public Vector3 wishDirection;

	private Vector3 lastVel;

	private static bool AutoBH;

	[ConCmd( "toggle.bhop" )]
	public static void ToggleBH()
	{
		AutoBH = !AutoBH;
	}

	protected override void OnStart()
	{
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

	Vector3 newVel;

	protected override void OnFixedUpdate()
	{
		lastVel = newVel;
		newVel = Velocity;

		Collider.Start = new Vector3( 0, 0, Radius );
		Collider.End = new Vector3( 0, 0, Height - Radius );

		if ( IsProxy )
			return;

		SimulateWeight();
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

	public virtual void UpdateCamera()
	{
		var camera = Scene.Camera;
		if ( !camera.IsValid() )
			return;

		camera.FieldOfView = Preferences.FieldOfView;

		EyeAngles += Input.AnalogLook;
		EyeAngles = EyeAngles.WithPitch( EyeAngles.pitch.Clamp( -89f, 89f ) );

		IsBobbing = wishDirection.Length > 0.1f;

		if ( IsBobbing )
		{
			_cameraBobTime += Time.Delta * CameraBobFrequency;
			var offsetY = MathF.Sin( _cameraBobTime ) * CameraBobAmplitude;
			_cameraBobOffset = Vector3.Up * offsetY;
		}
		else
		{
			_cameraBobOffset = Vector3.Lerp( _cameraBobOffset, Vector3.Zero, Time.Delta * 10f );
		}

		var targetTransform = new Transform( CameraPosition() + _cameraBobOffset, CameraRotation() );

		targetTransform.Position = targetTransform.PointToWorld( CameraPosOffset );
		targetTransform.Rotation *= CameraRotOffset;

		camera.WorldTransform = targetTransform;
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
		AddSpeed( MaxAirSpeed, MaxAccelM );
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
