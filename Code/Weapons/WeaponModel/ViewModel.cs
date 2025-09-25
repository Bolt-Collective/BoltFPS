using Sandbox;
using ShrimplePawns;
using System.Net.Mail;

namespace Seekers;

[Icon( "pan_tool" )]
public class ViewModel : Component
{

	[Property]
	public SwayTypes SwayType { get; set; }

	public bool graphSway => SwayType == SwayTypes.Graph;

	public bool swingAndBob => SwayType == SwayTypes.SwingAndBob;

	public bool procedualSway => SwayType == SwayTypes.ProcedualSway;

	[Property]
	public List<SkinnedModelRenderer> SyncedRenderers { get; set; }

	[Property, Group( "AnimGraph" )]
	public bool SprintHold { get; set; }

	[Property, Group( "SwingAndBob" ), ShowIf("swingAndBob", true)] public float SwingInfluence { get; set; } = 0.05f;
	[Property, Group( "SwingAndBob" ), ShowIf( "swingAndBob", true )] public float ReturnSpeed { get; set; } = 5.0f;
	[Property, Group( "SwingAndBob" ), ShowIf( "swingAndBob", true )] public float MaxOffsetLength { get; set; } = 10.0f;
	[Property, Group( "SwingAndBob" ), ShowIf( "swingAndBob", true )] public float BobCycleTime { get; set; } = 7;
	[Property, Group( "SwingAndBob" ), ShowIf( "swingAndBob", true )] public Vector3 BobDirection { get; set; } = new(0.0f, 1.0f, 0.5f);
	[Property, Group( "SwingAndBob" ), ShowIf( "swingAndBob", true )] public float InertiaDamping { get; set; } = 20.0f;

	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public Angles YawInertiaAngle { get; set; } = new Angles(0, 30, 20);
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public Angles PitchInertiaAngle { get; set; } = new Angles( 30, 0, 0 );
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public Curve JumpCurve { get; set; } = Curve.Ease;	
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public float JumpTime { get; set; } = 0.5f;
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public float JumpDownTime { get; set; } = 0.25f;
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public Vector3 JumpOffset { get; set; } = new Vector3( 0, 0, -1 );
	[Property, Group( "ProcedualSway" ), ShowIf( "procedualSway", true )] public GameObject RotateAround { get; set; }


	[Property, ToggleGroup("ProcedualAim")] public bool procedualAim { get; set; }

	[Property, Group( "ProcedualAim" )] public float AimTime { get; set; } = 2;
	[Property, Group( "ProcedualAim" )] public Curve AimPosCurve { get; set; }
	[Property, Group( "ProcedualAim" )] public Curve AimRotCurve { get; set; }
	[Property, Group( "ProcedualAim" )] public GameObject IronSightPoint { get; set; }
	[Property, Group( "ProcedualAim" )] public GameObject IronSightBack { get; set; }
	[Property, Group( "ProcedualAim" )] public float Distance { get; set; }
	[Property, Group( "ProcedualAim" )] public float Distance60 { get; set; }
	[Property, Group( "ProcedualAim" )] public float Distance120 { get; set; }
	[Property, Group( "ProcedualAim" )] public Scope ScopePoint { get; set; }
	[Property, Group( "ProcedualAim" )] public float ScopeCost { get; set; } = 10;


	[Property, ToggleGroup( "BulletGroups" )] public bool BulletGroups { get; set; }
	[Property, Group( "BulletGroups" )] public string BulletGroupName { get; set; } = "Ammo";
	[Property, Group( "BulletGroups" )] public int MaxBulletGroup { get; set; } = 4;


	[RequireComponent] public SkinnedModelRenderer Renderer { get; set; }

	public float YawInertia { get; private set; }
	public float PitchInertia { get; private set; }

	[Property] public float OffsetAt60 { get; set; }
	[Property] public Vector3 Offset { get; set; }
	[Property] public float OffsetAt120 { get; set; }

	private float XOffset
	{
		get
		{
			float fov = Preferences.FieldOfView;

			if ( fov < 90f )
			{
				float t = (fov - 60f) / (90f - 60f);
				return MathX.Lerp( OffsetAt60, 0, t );
			}
			else
			{
				float t = (fov - 90f) / (120f - 90f);
				return MathX.Lerp( 0, OffsetAt120, t );
			}
		}
	}

	private float AimOffset
	{
		get
		{
			float fov = Preferences.FieldOfView;

			if ( fov < 90f )
			{
				if ( Distance60 == 0 )
					return OffsetAt60;
				float t = (fov - 60f) / (90f - 60f);
				return MathX.Lerp( Distance60, 0, t );
			}
			else
			{
				if ( Distance120 == 0 )
					return OffsetAt120;
				float t = (fov - 90f) / (120f - 90f);
				return MathX.Lerp( 0, Distance120, t );
			}
		}
	}

	[Property] public Dictionary<string, TranslatedAnim> AnimParamTranslate { get; set; }
	[Property] public Dictionary<string, GameObject> AttachmentTranslate { get; set; }

	public enum SwayTypes
	{
		Graph,
		SwingAndBob,
		ProcedualSway
	}

	public struct TranslatedAnim
	{
		[KeyProperty] public string Param { get; set; }
		public bool Reset { get; set; }
		[ShowIf("Reset", true)] public float ResetDelay { get; set; }
	}
	public Transform GetAttachment(string name)
	{
		if (AttachmentTranslate != null && AttachmentTranslate.ContainsKey(name))
			return AttachmentTranslate[name].WorldTransform;
		return Renderer?.GetAttachment( name ) ?? WorldTransform;
	}

	public string GetAnim( string name )
	{
		if ( AnimParamTranslate != null && AnimParamTranslate.ContainsKey( name ) )
		{
			var translated = AnimParamTranslate[name];
			if ( translated.Reset )
				Reset( translated.Param, translated.ResetDelay );

			return translated.Param;
		}
		return name;
	}

	public async void Reset(string anim, float delay)
	{
		await Task.DelaySeconds( delay );
		Renderer?.Set( anim, false );
	}

	private Vector3 swingOffset;

	private float lastPitch;
	private float lastYaw;
	private float bobAnim;
	private float bobSpeed;
	private float pIntertiaSmooth;
	private float yIntertiaSmooth;
	private float jumpOffsetSmooth;

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

		if ( !pawn.Inventory.ActiveWeapon.IsValid() )
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

		LocalPosition = Offset + Vector3.Forward * XOffset;
		WorldRotation = inRot;


		if ( Renderer.TryGetBoneTransformLocal( "camera", out var bone ) )
		{
			pawn.Controller.Camera.LocalPosition += bone.Position;
			pawn.Controller.Camera.LocalRotation = bone.Rotation * 2;
		}

		var newPitch = WorldRotation.Pitch();
		var newYaw = WorldRotation.Yaw();

		bool pitchInRange = newPitch < 89f && newPitch > -89f;

		var pitchDelta = pitchInRange ? Angles.NormalizeAngle( newPitch - lastPitch ) : 0;
		var yawDelta = pitchInRange ? Angles.NormalizeAngle( lastYaw - newYaw ) : 0;

		PitchInertia += pitchDelta;
		YawInertia += yawDelta;

		pIntertiaSmooth = pIntertiaSmooth.LerpTo( PitchInertia, 10 * Time.Delta );
		yIntertiaSmooth = yIntertiaSmooth.LerpTo( YawInertia, 10 * Time.Delta );

		ProcedualAim(pawn);

		if ( swingAndBob )
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
				yIntertiaSmooth,
				pIntertiaSmooth,
				velocity,
				pawn.Controller.IsSprinting && Renderer.GetFloat( "attack_hold" ) <= 0 &&
				pawn.Controller.wishDirection.Length >= 0.1f,
				pawn.Controller.IsGrounded,
				pawn.Inventory.ActiveWeapon.Ammo <= 0
			);
		}

		switch (SwayType)
		{
			case SwayTypes.Graph:
				GraphSway(pawn);
				break;
			case SwayTypes.SwingAndBob:
				DoSwingAndBob( newPitch, pitchDelta, yawDelta );
				break;
			case SwayTypes.ProcedualSway:
				ProcedualSway( pawn );
				break;
		}

		if ( pitchInRange )
		{
			lastPitch = newPitch;
			lastYaw = newYaw;
		}

		YawInertia = YawInertia.LerpTo( 0, Time.Delta * InertiaDamping );
		PitchInertia = PitchInertia.LerpTo( 0, Time.Delta * InertiaDamping );

		DoBulletGroups(pawn);
	}

	public void DoBulletGroups( Pawn pawn )
	{
		if ( !BulletGroups )
			return;

		foreach ( var renderer in Renderers )
			renderer.SetBodyGroup( BulletGroupName, pawn.Inventory.ActiveWeapon.Ammo.Clamp( 0, MaxBulletGroup ) );
	}

	public void GraphSway(Pawn pawn)
	{
		var velocity = pawn.Controller.IsGrounded
				? GetPercentageBetween( pawn.Controller.Velocity.Length, 0, pawn.Controller.WalkSpeed )
					.Clamp( 0, 1 )
				: 0;
		Animate(
			yIntertiaSmooth,
			pIntertiaSmooth,
			velocity,
			pawn.Controller.IsSprinting && pawn.Controller.IsGrounded && Renderer.GetFloat( "attack_hold" ) <= 0 &&
			pawn.Controller.wishDirection.Length >= 0.1f,
			pawn.Controller.IsGrounded,
			pawn.Inventory.ActiveWeapon.Ammo <= 0
		);
	}

	public void ProcedualSway( Pawn pawn )
	{
		GraphSway( pawn );

		var rotateAroundObject = RotateAround.IsValid() ? RotateAround : GameObject.Parent;

		var rotateAroundPoint = GameObject.Parent.WorldTransform.PointToLocal( rotateAroundObject.WorldPosition );

		var pitchIntertia = (pIntertiaSmooth / 180) * PitchInertiaAngle.AsVector3();
		var yawIntertia = (yIntertiaSmooth / 180) * YawInertiaAngle.AsVector3();

		var rot = new Angles( pitchIntertia + yawIntertia );
		LocalTransform = LocalTransform.RotateAround(rotateAroundPoint, rot);

		var vel = 0f;
		if ( !pawn.Controller.IsGrounded )
			jumpOffsetSmooth += Time.Delta * 1 / JumpTime;
		else
			jumpOffsetSmooth -= Time.Delta * 1 / JumpDownTime;

		jumpOffsetSmooth = jumpOffsetSmooth.Clamp( 0, 1 );

		LocalPosition += JumpOffset * JumpCurve.Evaluate( jumpOffsetSmooth );

		if ( !IronSightBack.IsValid() ) return;

		var ironSightsPoint = GameObject.Parent.WorldTransform.PointToLocal( IronSightBack.WorldPosition ).z + 0.6f;

		LocalPosition -= Vector3.Up * ironSightsPoint.Clamp( 0, 100 );
	}
	private bool Aiming;
	private float aimSmooth;
	private float steadySmooth;
	public void ProcedualAim(Pawn pawn)
	{
		if ( !procedualAim ) return;


		var vel = 0f;
		aimSmooth += (Aiming && !GetBool("b_sprint") && !pawn.Inventory.ActiveWeapon.IsReloading ? Time.Delta : -Time.Delta) * 1 / AimTime;
		aimSmooth = aimSmooth.Clamp( 0, 1 );

		var goingSteady = ScopePoint.IsValid() && pawn.Stamina > 0 && Input.Down( "Walk" );
		if ( goingSteady )
			pawn.TakeStamina(Time.Delta * ScopeCost);

		if ( pawn.Stamina < 0.1f )
			goingSteady = false;

		steadySmooth += (goingSteady ? Time.Delta : -Time.Delta) * (1f / 0.3f);
		steadySmooth = steadySmooth.Clamp( 0, 1 );

		var targetPosOffset = Vector3.Zero;

		var parentPos = GameObject.Parent.WorldPosition;
		var parentRot = GameObject.Parent.WorldRotation;

		var ironRel = GameObject.Parent.WorldTransform.RotationToLocal( IronSightPoint.WorldRotation );
		var scopeRel = GameObject.Parent.WorldTransform.RotationToLocal( ScopePoint?.WorldRotation ?? IronSightPoint.WorldRotation );
		var blendedRel = Rotation.Lerp( ironRel, scopeRel, steadySmooth );

		var childRot = GameObject.Parent.WorldTransform.RotationToWorld( blendedRel );

		var offset = Rotation.Difference( childRot, parentRot ) * AimRotCurve.Evaluate( aimSmooth );

		WorldRotation = offset * WorldRotation;
		WorldPosition = parentPos + offset * (WorldPosition - parentPos);

		var ironPointPos = GameObject.Parent.WorldTransform.PointToLocal( IronSightPoint.WorldPosition );
		var scopePosRel = GameObject.Parent.WorldTransform.PointToLocal( ScopePoint?.WorldPosition ?? IronSightPoint.WorldPosition );

		targetPosOffset -= ironPointPos.LerpTo(scopePosRel, steadySmooth).WithX(ironPointPos.x);
		targetPosOffset += Vector3.Forward * (Distance + AimOffset);

		LocalPosition += targetPosOffset * AimPosCurve.Evaluate( aimSmooth );
	}

	[Rpc.Broadcast]
	public void Set( string name, bool value )
	{
		var anim = GetAnim( name );

		foreach(var renderer in Renderers )
			renderer?.Set( anim, value );
	}

	[Rpc.Broadcast]
	public void Set( string name, float value )
	{
		var anim = GetAnim( name );

		foreach ( var renderer in Renderers )
			renderer?.Set( anim, value );
	}

	[Rpc.Broadcast]
	public void Set( string name, int value )
	{
		if ( name == "ironsights" )
			Aiming = value != 0;

		var anim = GetAnim( name );

		foreach ( var renderer in Renderers )
			renderer?.Set( anim, value );
	}

	public bool GetBool( string name )
	{
		return Renderer?.GetBool( GetAnim( name ) ) ?? false;
	}

	public float GetFloat( string name )
	{
		return Renderer?.GetFloat( GetAnim( name )) ?? 0;
	}

	public int GetInt( string name )
	{
		return Renderer?.GetInt( GetAnim( name ) ) ?? 0;
	}

	private List<SkinnedModelRenderer> Renderers => GetRenderers();

	private List<SkinnedModelRenderer> GetRenderers()
	{
		var renderers = new List<SkinnedModelRenderer>();
		if ( SyncedRenderers != null )
			renderers.AddRange( SyncedRenderers );
		renderers.Add( Renderer );

		return renderers;
	}

	public void Animate( float yawInertia, float pitchInertia, float velocity, bool sprint, bool grounded, bool empty )
	{
		Set( "aim_yaw_inertia", yawInertia );
		Set( "aim_pitch_inertia", pitchInertia );
		Set( "move_bob", velocity );
		Set("b_sprint", sprint );
		Set( "b_grounded", grounded );
		Set( "b_empty", empty );
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
