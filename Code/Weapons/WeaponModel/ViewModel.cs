using ShrimplePawns;

namespace Seekers;

[Icon( "pan_tool" )]
public class ViewModel : Component
{
	[Property, ToggleGroup( "SwingAndBob" )]
	public bool SwingAndBob { get; set; } = true;
	[Property, Group( "AnimGraph" ), ShowIf("SwingAndBob", false)] public bool SprintHold { get; set; }

	[Property, Group( "SwingAndBob" )] public float SwingInfluence { get; set; } = 0.05f;
	[Property, Group( "SwingAndBob" )] public float ReturnSpeed { get; set; } = 5.0f;
	[Property, Group( "SwingAndBob" )] public float MaxOffsetLength { get; set; } = 10.0f;
	[Property, Group( "SwingAndBob" )] public float BobCycleTime { get; set; } = 7;
	[Property, Group( "SwingAndBob" )] public Vector3 BobDirection { get; set; } = new(0.0f, 1.0f, 0.5f);
	[Property, Group( "SwingAndBob" )] public float InertiaDamping { get; set; } = 20.0f;

	[RequireComponent] public SkinnedModelRenderer Renderer { get; set; }

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }

	[Property] public Vector3 Offset { get; set; }

	[Property] public Dictionary<string, string> AnimParamTranslate { get; set; }

	public string GetAnim( string name )
	{
		if ( AnimParamTranslate != null && AnimParamTranslate.ContainsKey( name ) )
			return AnimParamTranslate[name];
		return name;
	}

	private Vector3 swingOffset;

	private float lastPitch;
	private float lastYaw;
	private float bobAnim;
	private float bobSpeed;

	private bool activated = false;

	protected override void OnEnabled()
	{
		Renderer?.Set( GetAnim( "b_deploy" ), true );
	}

	protected override void OnPreRender()
	{
		if ( IsProxy )
			return;

		var pawn = Pawn.Local;
		if ( !pawn.IsValid() )
			return;

		if ( !Renderer.IsValid() )
			return;

		var inPos = pawn.Controller.Camera.WorldPosition;
		var inRot = pawn.Controller.Camera.WorldRotation;

		if ( !activated )
		{
			lastPitch = MathX.Clamp( inRot.Pitch(), -89, 89 );
			lastYaw = inRot.Yaw();

			YawInertia = 0;
			PitchInertia = 0;

			activated = true;
		}

		LocalPosition = Offset;
		WorldRotation = inRot;

		/*
		 * This causes fucked up shit
		if ( Renderer.TryGetBoneTransformLocal( "camera", out var bone ) )
		{
			pawn.Controller.Camera.LocalPosition += bone.Position;
			pawn.Controller.Camera.LocalRotation *= bone.Rotation;
		}
		*/

		var newPitch = WorldRotation.Pitch();
		var newYaw = WorldRotation.Yaw();

		bool pitchInRange = newPitch < 89f && newPitch > -89f;

		var pitchDelta = pitchInRange ? Angles.NormalizeAngle( newPitch - lastPitch ) : 0;
		var yawDelta = pitchInRange ? Angles.NormalizeAngle( lastYaw - newYaw ) : 0;

		PitchInertia += pitchDelta;
		YawInertia += yawDelta;

		if ( SwingAndBob )
		{
			DoSwingAndBob( newPitch, pitchDelta, yawDelta );
		}
		else
		{
			var velocity = pawn.Controller.IsGrounded
				? GetPercentageBetween( pawn.Controller.Velocity.Length, 0, pawn.Controller.WalkSpeed )
					.Clamp( 0, 1 )
				: 0;
			Animate(
				YawInertia,
				PitchInertia,
				velocity,
				pawn.Controller.IsRunning && Renderer.GetFloat( "attack_hold" ) <= 0 && pawn.Controller.wishDirection.Length >= 0.1f && SprintHold,
				pawn.Controller.IsGrounded,
				pawn.Inventory.ActiveWeapon.Ammo <= 0
			);
		}

		if ( pitchInRange )
		{
			lastPitch = newPitch;
			lastYaw = newYaw;
		}

		YawInertia = YawInertia.LerpTo( 0, Time.Delta * InertiaDamping );
		PitchInertia = PitchInertia.LerpTo( 0, Time.Delta * InertiaDamping );
	}

	[Rpc.Broadcast]
	public void Set( string name, bool value )
	{
		Renderer?.Set( GetAnim( name ), value );
	}

	[Rpc.Broadcast]
	public void Set( string name, float value )
	{
		Renderer?.Set( GetAnim( name ), value );
	}

	[Rpc.Broadcast]
	public void Animate( float yawInertia, float pitchInertia, float velocity, bool sprint, bool grounded, bool empty )
	{
		Renderer.Set( GetAnim( "aim_yaw_inertia" ), yawInertia );
		Renderer.Set( GetAnim( "aim_pitch_inertia" ), pitchInertia );
		Renderer.Set( GetAnim( "move_bob" ), velocity );
		Renderer.Set( GetAnim( "b_sprint" ), sprint );
		Renderer.Set( GetAnim( "b_grounded" ), grounded );
		Renderer.Set( GetAnim( "b_empty" ), empty );
	}

	public float GetPercentageBetween( float value, float min, float max )
	{
		return (value - min) / (max - min);
	}

	private void DoSwingAndBob( float newPitch, float pitchDelta, float yawDelta )
	{
		var player = Pawn.Local;
		var playerVelocity = player.Controller.Velocity;

		var verticalDelta = playerVelocity.z * Time.Delta;
		var viewDown = Rotation.FromPitch( newPitch < 89f && newPitch > -89f ? newPitch : lastPitch ).Up * -1.0f;

		verticalDelta *= 1.0f - MathF.Abs( viewDown.Cross( Vector3.Down ).y );

		if ( float.IsNaN( verticalDelta ) )
			return;

		pitchDelta -= verticalDelta * 1.0f;

		if ( float.IsNaN( pitchDelta ) )
			return;

		var speed = playerVelocity.WithZ( 0 ).Length;
		speed = speed > 10.0 ? speed : 0.0f;
		bobSpeed = bobSpeed.LerpTo( speed, Time.Delta * InertiaDamping );

		var offset = CalcSwingOffset( pitchDelta, yawDelta );
		offset += CalcBobbingOffset( bobSpeed );

		WorldPosition += WorldRotation * offset + (player.Controller.Camera.WorldTransform.PointToWorld( Offset ) -
		                                           player.Controller.Camera.WorldPosition);
	}

	protected Vector3 CalcSwingOffset( float pitchDelta, float yawDelta )
	{
		var swingVelocity = new Vector3( 0, yawDelta, pitchDelta );

		swingOffset -= swingOffset * ReturnSpeed * Time.Delta;
		swingOffset += swingVelocity * SwingInfluence;

		if ( swingOffset.Length > MaxOffsetLength )
			swingOffset = swingOffset.Normal * MaxOffsetLength;

		return swingOffset;
	}

	protected Vector3 CalcBobbingOffset( float speed )
	{
		bobAnim += Time.Delta * BobCycleTime;

		var twoPI = MathF.PI * 2.0f;

		if ( bobAnim > twoPI )
			bobAnim -= twoPI;

		var offset = BobDirection * (speed * 0.005f) * MathF.Cos( bobAnim );
		offset = offset.WithZ( -MathF.Abs( offset.z ) );

		return offset;
	}
}
