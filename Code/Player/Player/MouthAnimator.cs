using Sandbox.Audio;

namespace BoltFPS;

[Icon("sentiment_neutral")]
[Tint(EditorTint.Green)]
public sealed class MouthAnimator : Component
{
	[Property] public SkinnedModelRenderer Renderer { get; set; }
	[Sync, Property] public float SpeechVolume { get; set; }
	[Property] public MixerHandle LocalVoiceMixer { get; set; }

	protected override void OnUpdate()
	{
		if ( LocalVoiceMixer.Get() == null )
			return;

		if ( !IsProxy )
			SpeechVolume = LocalVoiceMixer.Get().Meter.Current.MaxLevel;

		Renderer.Morphs.Set( "jawopen", SpeechVolume );
		Renderer.Morphs.Set( "smile", 1 );
	}
}
