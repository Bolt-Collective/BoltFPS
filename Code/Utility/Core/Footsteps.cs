
namespace Seekers;

public class Footsteps : Component
{
	[RequireComponent] public Pawn Player { get; set; }

	/// <summary>
	/// Draw debug overlay on footsteps
	/// </summary>
	[Property]
	public bool DebugFootsteps { get; set; } = false;

	TimeSince _timeSinceStep;

	private float GetStepFrequency()
	{
		if ( Player.Controller.IsCrouching ) return 0.5f;
		if ( Player.Controller.IsRunning ) return 0.28f;
		return 0.39f;
	}

	private float GetStepVolume()
	{
		if ( Player.Controller.IsCrouching ) return 0.25f;
		return 0.4f;
	}

	bool leftFoot = true;

	protected override void OnFixedUpdate()
	{
		if ( !Player.IsValid() )
			return;

		if ( !Player.Controller.IsGrounded )
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

		foreach ( var tag in Player.Controller.GroundObject.Tags )
		{
			if ( tag.StartsWith( "m-" ) || tag.StartsWith( "m_" ) )
			{
				tagMaterial = tag.Remove( 0, 2 );
				break;
			}
		}

		var actualSurf = Player.Controller.GroundSurface;

		var GroundSurface = tagMaterial == ""
			? actualSurf.ReplaceSurface()
			: (Surface.FindByName( tagMaterial ) ?? actualSurf.ReplaceSurface());

		var sound = leftFoot ? GroundSurface.SoundCollection.FootLeft : GroundSurface.SoundCollection.FootRight;

		var worldPosition = Player.Controller.WorldPosition;

		if ( sound is null )
		{
			if ( DebugFootsteps )
			{
				DebugOverlay.Sphere( new Sphere( worldPosition, GetStepVolume() ), duration: 10, color: Color.Orange,
					overlay: true );
			}

			return;
		}

		SoundExtensions.BroadcastSound( sound, worldPosition,
			GetStepVolume() * Player.Controller.WishVelocity.Length.Remap( 0, 400, 0, 1 ),
			spacialBlend:
			Player.IsValid() && Player.IsMe ? 0 : 1 );

		if ( DebugFootsteps )
		{
			DebugOverlay.Sphere( new Sphere( worldPosition, GetStepVolume() ), duration: 10, overlay: true );
			DebugOverlay.Text( worldPosition, $"{sound.ResourceName}", size: 14, flags: TextFlag.LeftTop,
				duration: 10, overlay: true );
		}
	}
}
