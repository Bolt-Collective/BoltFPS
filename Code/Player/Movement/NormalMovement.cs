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

	public bool EnableSprinting = true;

	public enum MoveModes
	{
		Walk, 
		Crouch
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
		}

		base.OnUpdate();
	}

	private float heightVelocity;

	public bool IsRunning => MaxSpeed == RunSpeed;
	public bool IsSprinting => MaxSpeed == SprintSpeed;
	public bool IsCrouching => MoveMode == MoveModes.Crouch;

	private void Walk()
	{
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

		if ( Input.Down( "Run" ) && EnableSprinting)
			MaxSpeed = SprintSpeed;

		if ( Input.Down( "Walk" ) )
			MaxSpeed = WalkSpeed;
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
}
