using Sandbox.Audio;
using Seekers;
using System;

public static partial class SoundExtensions
{
	[Rpc.Broadcast]
	public static void BroadcastSound( string soundName, Vector3 position, float volume = 1.0f, float pitch = 1.0f,
		float spacialBlend = 1.0f )
	{
		var snd = Sound.Play( soundName, position );
		
		if (!snd.IsValid())
			return;
		
		snd.Volume = volume;
		snd.Pitch = pitch;
		snd.SpacialBlend = spacialBlend;
	}

	[Rpc.Broadcast]
	public static void BroadcastSound( SoundEvent sound, Vector3 position, float volume = 1.0f, float pitch = 1.0f,
		float spacialBlend = 1.0f )
	{
		BroadcastSound( sound.ResourcePath, position, volume, pitch, spacialBlend );
	}


	[Rpc.Broadcast]
	public static void FollowSound( SoundEvent Sound, GameObject Followed, string sender = default,
		string localHandle = "Game", string mainHandle = "Game" )
	{
		MakeFollowSound( Sound, Followed, sender, localHandle, mainHandle );
	}

	public static SoundPointComponent MakeFollowSound( SoundEvent Sound, GameObject Followed, string sender = default,
		string localHandle = "Game", string mainHandle = "Game" )
	{
		GameObject gameObject = new GameObject();

		var SoundPoint = gameObject.Components.Create<SoundPointComponent>();
		var FollowDestroyComponent = gameObject.AddComponent<TimedDestroyFollowComponent>();

		FollowDestroyComponent.Follow = Followed;
		FollowDestroyComponent.Time = 10;
		FollowDestroyComponent.Offset = Vector3.Up * 10;
		SoundPoint.SoundEvent = Sound;
		SoundPoint.TargetMixer =
			Mixer.FindMixerByName( sender == Connection.Local.Id.ToString() ? localHandle : mainHandle );
		SoundPoint.StartSound();

		return SoundPoint;
	}

	[Rpc.Broadcast]
	public static void TauntSound( SoundEvent Sound, GameObject Followed, string sender = default,
		string localHandle = "Game", string mainHandle = "Game" )
	{
		var soundPoint = MakeFollowSound( Sound, Followed, sender, localHandle, mainHandle );
		soundPoint.AddComponent<TauntSoundComponent>();
	}

	public static SoundEvent RandomSoundFromFolder( string folder )
	{
		IEnumerable<SoundEvent> Sounds = null;

		//error is INSIDE getall, Sorry FUCK
		//again, FUCK. WHY
		try
		{
			Sounds = ResourceLibrary.GetAll<SoundEvent>( folder );
		}
		catch { }

		if ( Sounds == null )
			return null;
		return Sounds.ElementAt( Game.Random.Next( 0, Sounds.Count() ) );
	}
}

public class TauntSoundComponent : Component
{
	[Property] public float MaxZDis { get; set; } = 200;
	[Property] public float MinVolume { get; set; } = 0.1f;

	float volume;

	GameObject _localPlayer;

	GameObject LocalPlayer
	{
		get
		{
			if ( !_localPlayer.IsValid() )
				_localPlayer = Pawn.Local?.GameObject;
			return _localPlayer;
		}
	}

	SoundPointComponent SoundPoint;

	protected override void OnStart()
	{
		SoundPoint = GetComponent<SoundPointComponent>();
		volume = SoundPoint.SoundEvent.Volume.FixedValue;
		SoundPoint.Volume = volume;
		SoundPoint.SoundOverride = true;
	}

	protected override void OnUpdate()
	{
		if ( !SoundPoint.IsValid() )
			return;

		if ( !LocalPlayer.IsValid() )
			return;

		var soundZDis = MathF.Abs( WorldPosition.z - LocalPlayer.WorldPosition.z );

		var distanceEffect = (1 - soundZDis / MaxZDis);

		SoundPoint.Volume = volume * EaseOutCubic( distanceEffect ).Clamp( MinVolume, 1 );
	}

	public static float EaseOutCubic( float x )
	{
		return 1 - MathF.Pow( 1 - x, 3 );
	}
}
