using Sandbox;
using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;
using ShrimplePawns;
using static Sandbox.PhysicsContact;
using Pawn = Seekers.Pawn;

public partial class NormalMovement : Movement
{
	[Property] public float StandingHeight { get; set; } = 64f;

	[Property] public float CrouchHeight { get; set; } = 38f;

	[Property] public float SlideHeight { get; set; } = 20f;

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

	[Property, Group( "Movement Variables" )]
	public bool CanNoClip { get; set; }

	[Property, Group( "Movement Variables" ), ShowIf( "CanNoClip", true )]
	[ConVar( "noclip_speed", ConVarFlags.Saved, Help = "Default noclip speed" )]
	public static float NoClipSpeed { get; set; } = 300;

	[Property, Group( "Movement Variables" ), ShowIf( "CanNoClip", true )]
	[ConVar( "noclip_sprintspeed", ConVarFlags.Saved, Help = "Default noclip shift speed" )]
	public static float NoClipSprintSpeed { get; set; } = 600;

	[Property, Group( "Movement Variables" ), ShowIf( "CanNoClip", true )]
	[ConVar( "noclip_walkspeed", ConVarFlags.Saved, Help = "Default alt (walk) speed, for slower movements" )]
	public static float NoClipWalkSpeed { get; set; } = 100;


	[Property, Sync] public MoveModes MoveMode { get; set; }

	public bool EnableSprinting = true;

	public bool EnableCrouching = true;

	public enum MoveModes
	{
		Walk,
		Crouch,
		NoClip
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		SnapToGround = MoveMode != MoveModes.NoClip;

		if ( CanNoClip && Input.Pressed( "Noclip" ) )
		{
			ToggleNoclip();
		}

		switch ( MoveMode )
		{
			case MoveModes.Walk:
				Walk();
				break;
			case MoveModes.Crouch:
				Crouch();
				break;
			case MoveModes.NoClip:
				NoClip();
				break;
		}
	}

	[ConCmd( "noclip", ConVarFlags.Admin )]
	public static void ToggleNoclip()
	{
		var controller = Pawn.Local?.Controller;

		if ( !controller.IsValid() )
			return;

		if ( controller.CanNoClip )
		{
			if ( controller.MoveMode == MoveModes.NoClip )
				controller.MoveMode = MoveModes.Walk;
			else
				controller.MoveMode = MoveModes.NoClip;
		}
	}


	[Property] public bool CanSetHeight = true;

	private void SetHeight( float height )
	{
		if ( !CanSetHeight )
			return;

		Height = height;
	}

	private float heightVelocity;

	public bool IsRunning => MaxSpeed == RunSpeed;
	public bool IsSprinting => MaxSpeed == SprintSpeed;
	public bool IsCrouching => MoveMode == MoveModes.Crouch;

	public override void WalkMove()
	{
		if ( MoveMode != MoveModes.NoClip )
		{
			base.WalkMove();
			return;
		}

		var rot = EyeAngles.ToRotation();

		var wishDirection = Input.AnalogMove.Normal * rot;

		if ( Input.Down( "Jump" ) )
			wishDirection += Vector3.Up;

		if ( Input.Down( "Duck" ) )
			wishDirection -= Vector3.Up;

		Velocity = wishDirection * MaxSpeed;
	}

	public override (Vector3 pos, Vector3 velocity) MovePos()
	{
		if ( MoveMode != MoveModes.NoClip )
			return base.MovePos();

		var newPosition = WorldPosition + Velocity * Time.Delta;

		return (newPosition, Velocity);
	}

	private void Walk()
	{
		if ( Input.Pressed( "Duck" ) && EnableCrouching )
		{
			MoveMode = MoveModes.Crouch;

			if ( !IsGrounded )
			{
				WorldPosition += Vector3.Up * (StandingHeight - CrouchHeight);
				Height = CrouchHeight;
			}

			Crouch();
			return;
		}

		SetHeight( MathX.SmoothDamp( Height, StandingHeight, ref heightVelocity, 0.1f, Time.Delta ) );

		MaxSpeed = RunSpeed;

		if ( Input.Down( "Run" ) && EnableSprinting )
			MaxSpeed = SprintSpeed;

		if ( Input.Down( "Walk" ) )
			MaxSpeed = WalkSpeed;
	}

	private void Crouch()
	{
		if ( (!Input.Down( "Duck" ) && !Input.Down( "Slide" ) && StandCheck()) || !EnableCrouching )
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

		SetHeight( MathX.SmoothDamp( Height, CrouchHeight, ref heightVelocity, 0.1f, Time.Delta ) );

		MaxSpeed = CrouchSpeed;
	}

	private void NoClip()
	{
		SetHeight( MathX.SmoothDamp( Height, StandingHeight, ref heightVelocity, 0.1f, Time.Delta ) );

		OnGroundVelocity = 0;

		MaxSpeed = NoClipSpeed;

		if ( Input.Down( "Run" ) && EnableSprinting )
			MaxSpeed = NoClipSprintSpeed;

		if ( Input.Down( "Walk" ) )
			MaxSpeed = NoClipWalkSpeed;
	}

	public bool StandCheck()
	{
		var previousHeight = Height;
		Height = StandingHeight;

		if ( !IsGrounded )
			WorldPosition -= Vector3.Up * (StandingHeight - CrouchHeight);

		var result = !IsStuck();

		if ( !IsGrounded )
			WorldPosition += Vector3.Up * (StandingHeight - CrouchHeight);

		Height = previousHeight;

		return result;
	}

	public override bool IsStuck()
	{
		if ( MoveMode == MoveModes.NoClip )
			return false;
		return base.IsStuck();
	}

	public override bool TryUnstuck( Vector3 velocity )
	{
		if ( MoveMode == MoveModes.NoClip )
			return false;
		return base.TryUnstuck( velocity );
	}
}
