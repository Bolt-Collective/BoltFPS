using Sandbox;
using System;
using System.ComponentModel.DataAnnotations;
using static Sandbox.PhysicsContact;

public partial class NormalMovement : Movement
{
	[Property]
	public float StandingHeight { get; set; } = 64f;

	[Property]
	public float CrouchHeight { get; set; } = 38f;

	[Property]
	public float SlideHeight { get; set; } = 20f;

	[Property, Group( "Movement Variables" )]
	public float RunSpeed { get; set; }

	[Property, Group( "Movement Variables" )]
	public float SprintSpeed { get; set; }

	[Property, Group( "Movement Variables" )]
	public float WalkSpeed { get; set; }

	[Property, Group( "Movement Variables" )]
	public float CrouchSpeed { get; set; }

	[Property, Group( "Movement Variables" )]
	public float SlideFriction { get; set; } = 2f;

	[Property, Group( "Movement Variables" )]
	public float SlideBoost { get; set; } = 500f;



	[Property, Sync]
	public MoveModes MoveMode { get; set; }

	public bool EnableRunning = true;

	public enum MoveModes
	{
		Walk, 
		Crouch,
		Slide
	}

	protected override void OnUpdate()
	{
		switch(MoveMode)
		{
			case MoveModes.Walk:
				Walk();
				break;
			case MoveModes.Crouch:
				Crouch();
				break;
			case MoveModes.Slide:
				Slide(); 
				break;
		}

		base.OnUpdate();
	}

	private float heightVelocity;

	private void Walk()
	{
		if ( MaxSpeed == SprintSpeed && IsGrounded && ( Input.Pressed("Slide") || Input.Pressed("Duck") ) )
		{
			MoveMode = MoveModes.Slide;
			AddSpeed( SlideBoost, 1000 );
			Slide();
			return;
		}

		if (Input.Pressed("Duck") || Input.Pressed("Slide") )
		{
			MoveMode = MoveModes.Crouch;

			if (!IsGrounded)
			{
				WorldPosition += Vector3.Up * (StandingHeight - CrouchHeight);
				Height = CrouchHeight;
			}
			
			Crouch();
			return;
		}

		Height = MathX.SmoothDamp(Height, StandingHeight, ref heightVelocity, 0.1f, Time.Delta);

		MaxSpeed = RunSpeed;

		if ( Input.Down( "Run" ) && EnableRunning)
			MaxSpeed = SprintSpeed;

		if ( Input.Down( "Walk" ) )
			MaxSpeed = WalkSpeed;
	}

	private void Slide()
	{
		Height = MathX.SmoothDamp( Height, SlideHeight, ref heightVelocity, 0.1f, Time.Delta );
		if ( Velocity.Length < WalkSpeed + 10 )
		{
			ExitSlide();
			return;
		}
		MaxSpeed = WalkSpeed;
	}

	private void ExitSlide()
	{
		if ( StandCheck() && !Input.Down("Slide") && !Input.Down("Duck") )
		{
			MoveMode = MoveModes.Walk;

			if ( !IsGrounded )
			{
				WorldPosition -= Vector3.Up * (StandingHeight - CrouchHeight);
				Height = StandingHeight;
			}

			Walk();
			return;
		}

		MoveMode = MoveModes.Crouch;
		Crouch();
	}

	Vector3 previousWish;

	public override void GroundVelocity()
	{
		if ( MoveMode != MoveModes.Slide )
		{
			base.GroundVelocity();
			return;
		}

		ApplyFriction( SlideFriction );

		var wish = wishDirection;

		if(wishDirection.Length < 0.5f)
		{
			wish = previousWish;
		}

		var currentSpeed = Vector3.Dot( Velocity, wishDirection.Normal * 2);
		var addSpeed = (MaxSpeed - currentSpeed).Clamp( 0, MaxAccelM * MaxSpeed * Time.Delta );

		previousWish = wishDirection;

		Velocity += addSpeed * wishDirection;
	}

	private void Crouch()
	{
		if ( !Input.Down( "Duck" ) && !Input.Down("Slide") && StandCheck() )
		{
			MoveMode = MoveModes.Walk;

			if ( !IsGrounded )
			{
				WorldPosition -= Vector3.Up * (StandingHeight - CrouchHeight);
				Height = StandingHeight;
			}

			Walk();
			return;
		}

		Height = MathX.SmoothDamp( Height, CrouchHeight, ref heightVelocity, 0.1f, Time.Delta );

		MaxSpeed = CrouchSpeed;
		
	}

	public bool StandCheck()
	{
		var previousHeight = Height;
		Height = StandingHeight;

		if(!IsGrounded)
			WorldPosition -= Vector3.Up * (StandingHeight - CrouchHeight);

		var result = !IsStuck();

		if(!IsGrounded)
			WorldPosition += Vector3.Up * (StandingHeight - CrouchHeight);

		Height = previousHeight;

		return result;
	}

	public override void Animate()
	{
		if (MoveMode != MoveModes.Slide)
		{
			BodyModelRenderer.Set( "skid", 0 );
			BodyModelRenderer.Set( "skid_x", 0 );
			BodyModelRenderer.Set( "skid_y", 0 );
			base.Animate();
			return;
		}

		var dir = Velocity;
		var forward = WorldRotation.Forward.Dot( dir );

		AnimationHelper.WithVelocity( 0 );
		AnimationHelper.WithWishVelocity( 0 );

		BodyModelRenderer.Set( "skid_x", -forward );

		BodyModelRenderer.Set( "skid", 1 );

	}
}
