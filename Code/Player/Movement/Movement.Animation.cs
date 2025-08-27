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

		if (CurrentSeat.IsValid())
		{
			AnimationHelper.Sitting = AnimationHelper.SittingStyle.Chair;
		}
		else
			AnimationHelper.Sitting = AnimationHelper.SittingStyle.None;

		AnimationHelper.WithWishVelocity( WishVelocity );
		AnimationHelper.WithVelocity( Velocity );
		AnimationHelper.DuckLevel = (1 - (Height / _startHeight)) * 2;
		var dir = ClampDirection( EyeAngles.Forward, BodyModelRenderer.WorldTransform.Forward, 80 );
		AnimationHelper.WithLook( dir * 100, 1, 1, 1.0f );
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

		if (CurrentSeat.IsValid())
		{
			BodyModelRenderer.WorldPosition = CurrentSeat.WorldTransform.PointToWorld( CurrentSeat.SeatPosition + SeatOffset);
			BodyModelRenderer.WorldRotation = CurrentSeat.WorldTransform.RotationToWorld( CurrentSeat.SeatRotation );
			return;
		}
		else
		{
			BodyModelRenderer.LocalPosition = 0;
		}

		if ( IsTouchingLadder && RotationFaceLadders )
		{
			BodyModelRenderer.WorldRotation = Rotation.Lerp( BodyModelRenderer.WorldRotation,
				Rotation.LookAt( LadderNormal * -1 ), Time.Delta * 5.0f );
			return;
		}

		var targetAngle = new Angles( 0, EyeAngles.yaw, 0 ).ToRotation();

		var velocity = WishVelocity.WithZ( 0 );

		float rotateDifference = BodyModelRenderer.WorldRotation.Distance( targetAngle );

		BodyModelRenderer.WorldRotation = Angles.Zero.WithYaw( BodyModelRenderer.WorldRotation.Yaw() );

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

			BodyModelRenderer.WorldRotation = Angles.Zero.WithYaw( newRotation.Yaw() );
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

			BodyModelRenderer.WorldRotation = Angles.Zero.WithYaw( newRotation.Yaw() );
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

	public static Vector3 ClampDirection( Vector3 direction, Vector3 target, float maxAngleDeg )
	{
		direction = direction.Normal;
		target = target.Normal;

		float angle = Vector3.GetAngle( direction, target );

		if ( angle <= maxAngleDeg )
			return direction;

		float t = maxAngleDeg / angle;
		return Vector3.Slerp( target, direction, t ).Normal;
	}
}
