using XMovement;

namespace Seekers;

public class Footsteps : Component
{
	[RequireComponent] public PlayerWalkControllerComplex Player { get; set; }

	/// <summary>
	/// Draw debug overlay on footsteps
	/// </summary>
	public bool DebugFootsteps;

	TimeSince _timeSinceStep;

	protected override void OnEnabled()
	{
		base.OnEnabled();

		Player.BodyModelRenderer.OnFootstepEvent += OnFootstepEvent;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();

		Player.BodyModelRenderer.OnFootstepEvent -= OnFootstepEvent;
	}


	private void OnFootstepEvent( SceneModel.FootstepEvent e )
	{
		if ( !Player.Controller.IsOnGround )
			return;

		if ( _timeSinceStep < 0.2f )
			return;

		_timeSinceStep = 0;

		PlayFootstepSound( e.Transform.Position, e.Volume, e.FootId );
	}

	public void PlayFootstepSound( Vector3 worldPosition, float volume, int foot )
	{
		if ( IsProxy )
			return;

		volume *= Player.Controller.WishVelocity.Length.Remap( 0, 400, 0, 1 );
		if ( volume <= 0.1f ) return;

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

		var GroundSurface = tagMaterial == "" ? actualSurf.Replace() : (Surface.FindByName( tagMaterial ) ?? actualSurf.Replace());
		//Log.Info( actualSurf );


		var sound = foot == 0 ? GroundSurface.Sounds.FootLeft : GroundSurface.Sounds.FootRight;
		var soundEvent = ResourceLibrary.Get<SoundEvent>( sound );

		if ( soundEvent is null )
		{
			if ( DebugFootsteps )
			{
				DebugOverlay.Sphere( new Sphere( worldPosition, volume ), duration: 10, color: Color.Orange,
					overlay: true );
			}

			return;
		}

		SoundExtensions.BroadcastSound( soundEvent.ResourcePath, worldPosition );

		if ( DebugFootsteps )
		{
			DebugOverlay.Sphere( new Sphere( worldPosition, volume ), duration: 10, overlay: true );
			DebugOverlay.Text( worldPosition, $"{soundEvent.ResourceName}", size: 14, flags: TextFlag.LeftTop,
				duration: 10, overlay: true );
		}
	}
}
