using Sandbox;
using Sandbox.Citizen;
using Sandbox.VR;
using System;
using System.ComponentModel.DataAnnotations;

public partial class Movement : Component
{
	[Property] public SkinnedModelRenderer BodyModelRenderer { get; set; }

	[Property] public AnimationHelper AnimationHelper { get; set; }

	[Property, Group( "Animator" )] public float RotationAngleLimit { get; set; } = 45.0f;
	[Property, Group( "Animator" )] public float RotationSpeed { get; set; } = 1.0f;
	[Property, Group( "Animator" )] public bool RotationFaceLadders { get; set; } = true;
	float _animRotationSpeed;
	TimeSince timeSinceRotationSpeedUpdate;

	public virtual void Animate()
	{
		if ( !AnimationHelper.IsValid() )
			return;
		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( Velocity );
		AnimationHelper.DuckLevel = (1 - (Height / _startHeight)) * 10;
		AnimationHelper.WithLook( EyeAngles.Forward * 100, 1, 1, 1.0f );
		AnimationHelper.IsGrounded = IsGrounded || IsTouchingLadder;
		AnimationHelper.IsClimbing = IsTouchingLadder;
		//AnimationHelper.IsSwimming = IsSwimming;

		if ( timeSinceRotationSpeedUpdate > 0.1f )
		{
			timeSinceRotationSpeedUpdate = 0;
			AnimationHelper.MoveRotationSpeed = _animRotationSpeed * 5;
			_animRotationSpeed = 0;
		}

		RotateBody();
	}

	public virtual void RotateBody()
	{
		if ( !BodyModelRenderer.IsValid() )
			return;

		if ( IsTouchingLadder && RotationFaceLadders )
		{
			BodyModelRenderer.WorldRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation,
				Rotation.LookAt( LadderNormal * -1 ), Time.Delta * 5.0f );
			return;
		}

		var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

		var velocity = WishVelocity.WithZ( 0 );

		float rotateDifference = BodyModelRenderer.WorldRotation.Distance( targetAngle );

		// We're over the limit - snap it 
		if ( rotateDifference > RotationAngleLimit )
		{
			var delta = 0.999f - (RotationAngleLimit / rotateDifference);
			var newRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation, targetAngle, delta );

			var a = newRotation.Angles();
			var b = BodyModelRenderer.WorldRotation.Angles();

			var yaw = MathX.DeltaDegrees( a.yaw, b.yaw );

			_animRotationSpeed += yaw;
			_animRotationSpeed = _animRotationSpeed.Clamp( -90, 90 );

			BodyModelRenderer.WorldRotation = newRotation;
		}

		if ( velocity.Length > 10 )
		{
			var newRotation = Rotation.Slerp( BodyModelRenderer.WorldRotation, targetAngle,
				Time.Delta * 2.0f * RotationSpeed * velocity.Length.Remap( 0, 100 ) );

			var a = newRotation.Angles();
			var b = BodyModelRenderer.WorldRotation.Angles();

			var yaw = MathX.DeltaDegrees( a.yaw, b.yaw );

			_animRotationSpeed += yaw;
			_animRotationSpeed = _animRotationSpeed.Clamp( -90, 90 );

			BodyModelRenderer.WorldRotation = newRotation;
		}
	}

	public bool Spectating;
	public bool Orbiting;

	public bool BodyVisible;

	public void UpdateBodyVisibility()
	{
		if ( !BodyModelRenderer.IsValid() )
			return;
		if ( IsProxy && !Spectating || Orbiting )
		{
			BodyVisible = true;
			foreach ( var mdlrenderer in BodyModelRenderer.Components.GetAll<ModelRenderer>(
				         FindMode.EverythingInSelfAndChildren ) )
			{
				mdlrenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			}

			return;
		}

		if ( !ThirdPerson )
		{
			foreach ( var mdlrenderer in BodyModelRenderer.Components.GetAll<ModelRenderer>(
				         FindMode.EverythingInSelfAndChildren ) )
			{
				mdlrenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.ShadowsOnly;
			}

			BodyVisible = false;
		}
		else
		{
			foreach ( var mdlrenderer in BodyModelRenderer.Components.GetAll<ModelRenderer>(
				         FindMode.EverythingInSelfAndChildren ) )
			{
				mdlrenderer.RenderType = Sandbox.ModelRenderer.ShadowRenderType.On;
			}

			BodyVisible = true;
		}
	}
}
