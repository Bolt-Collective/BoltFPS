using XMovement;

namespace Seekers;

public class Footsteps : Component
{
	[RequireComponent] public PlayerWalkControllerComplex Player { get; set; }

	/// <summary>
	/// Draw debug overlay on footsteps
	/// </summary>
	[Property]
	public bool DebugFootsteps { get; set; } = false;

	TimeSince _timeSinceStep;

	private float GetStepFrequency()
	{
		if ( Player.IsCrouching ) return 0.5f;
		if ( Player.IsRunning ) return 0.28f;
		return 0.39f;
	}

	private float GetStepVolume()
	{
		if ( Player.IsCrouching ) return 0.35f;
		return 1f;
	}

	bool leftFoot = true;

	protected override void OnFixedUpdate()
	{
		if ( !Player.IsValid() )
			return;

		if ( !Player.Controller.IsOnGround )
			return;

		if ( Player.Controller.Velocity.Length < 55 )
			return;

		if ( _timeSinceStep < GetStepFrequency() )
			return;

		if ( IsProxy )
			return;

		leftFoot = !leftFoot;
		_timeSinceStep = 0;

		var tagMaterial = "";

		foreach ( var tag in Player.Controller.MoveTraceResult.Tags )
		{
			if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
			{
				tagMaterial = tag.Remove( 0, 2 );
				break;
			}
		}

		var actualSurf = Player.Controller.MoveTraceResult.Surface.GetRealSurface();

		var GroundSurface = tagMaterial == ""
			? actualSurf.Replace()
			: (Surface.FindByName( tagMaterial ) ?? actualSurf.Replace());

		var sound = leftFoot ? GroundSurface.Sounds.FootLeft : GroundSurface.Sounds.FootRight;
		var soundEvent = ResourceLibrary.Get<SoundEvent>( sound );

		var worldPosition = Player.Controller.MoveTraceResult.EndPosition;

		if ( soundEvent is null )
		{
			if ( DebugFootsteps )
			{
				DebugOverlay.Sphere( new Sphere( worldPosition, GetStepVolume() ), duration: 10, color: Color.Orange,
					overlay: true );
			}

			return;
		}

		SoundExtensions.BroadcastSound( soundEvent.ResourcePath, worldPosition, GetStepVolume(),
			spacialBlend: Pawn.Local.IsMe ? 0 : default );

		if ( DebugFootsteps )
		{
			DebugOverlay.Sphere( new Sphere( worldPosition, GetStepVolume() ), duration: 10, overlay: true );
			DebugOverlay.Text( worldPosition, $"{soundEvent.ResourceName}", size: 14, flags: TextFlag.LeftTop,
				duration: 10, overlay: true );
		}
	}
}
