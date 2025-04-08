namespace Seekers;

public partial class Pawn : ShrimplePawns.Pawn
{
	public Vector3 MoveToResult( Vector3 startPosition, Vector3 targetPosition, string[] WithoutTags )
	{
		var pos = startPosition;
		var delta = targetPosition - pos;

		var mover = new CharacterControllerHelper( Controller.Controller.BuildTrace( pos, pos, 0, 0.8f ).WithoutTags( WithoutTags ), pos, delta )
		{
			MaxStandableAngle = Controller.Controller.GroundAngle
		};

		mover.TryMove( 1f );

		return mover.Position;
	}

	Vector3 worldCheck;
	bool worldCheckSet;
	RealTimeSince TimeSinceClipPrevention;
	TimeSince UnStuckTime = 100;

	float stuckTime;

	void ClippingPrevention()
	{
		if ( IsProxy )
			return;

		if ( !worldCheckSet )
		{
			worldCheck = WorldPosition;
			worldCheckSet = true;
		}

		if ( (UnStuckTime > 1 && TimeSinceClipPrevention > 1) && Controller.Controller.IgnoreLayers.ToList().Contains( "worldprop" ) )
		{
			Controller.Controller.IgnoreLayers.Remove( "worldprop" );
		}
		if ( (UnStuckTime <= 1 || TimeSinceClipPrevention <= 1) && !Controller.Controller.IgnoreLayers.Contains( "worldprop" ) )
		{
			Controller.Controller.IgnoreLayers.Add( "worldprop" );
		}

		if ( UnStuckTime > 1 )
			worldCheck = MoveToResult( worldCheck, WorldPosition, ["worldprop"] );
		else
			worldCheck = WorldPosition;

		if ( Vector3.DistanceBetween( worldCheck, WorldPosition ) > 0.01f && UnStuckTime > 1)
		{
			stuckTime += Time.Delta;
			TimeSinceClipPrevention = 0;
			WorldPosition = worldCheck;
		}
		else
			stuckTime = (stuckTime - Time.Delta).Clamp(0,1000);

		if(stuckTime > 5 )
		{
			ToastNotification.Current?.AddToast( "If you're stuck try run the command: seekers.unstuck" );
			Log.Info( "If you're stuck try run the command: seekers.unstuck" );
			stuckTime = 0;
		}
	}

	[ConCmd("seekers.unstuck")]
	public static void UnStuck()
	{
		var pawn = Client.Local?.GetPawn<Pawn>();

		if ( !pawn.IsValid() )
			pawn = Local;

		if ( !pawn.IsValid() )
			return;

		pawn.UnStuckTime = 0;
	}
}
