public static class NetworkSound
{
	[Rpc.Broadcast]
	public static void ToggleSoundPoint( GameObject soundPointObj, bool shouldPlay )
	{
		var soundPoint = soundPointObj.GetComponent<SoundPointComponent>();
		if ( !soundPoint.IsValid() )
			return;

		if ( shouldPlay )
		{
			soundPoint.StartSound();
		}
		else
		{
			soundPoint.StopSound();
		}
	}
}

public static class SoundPointExtensions
{
	public static void BroadcastStart( this SoundPointComponent soundPoint )
	{
		if ( !soundPoint.IsValid() )
			return;

		NetworkSound.ToggleSoundPoint( soundPoint.GameObject, true );
	}

	public static void BroadcastStop( this SoundPointComponent soundPoint )
	{
		if ( !soundPoint.IsValid() )
			return;

		NetworkSound.ToggleSoundPoint( soundPoint.GameObject, false );
	}
}
