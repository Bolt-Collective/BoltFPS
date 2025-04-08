/// <summary>
/// A simple component that destroys its GameObject.
/// </summary>
public sealed class TimedDestroyFollowComponent : Component
{
	[Property] public GameObject Follow { get; set; }

	public Vector3 Offset { get; set; }

	/// <summary>
	/// How long until we destroy the GameObject.
	/// </summary>
	[Property] public float Time { get; set; } = 1f;

	/// <summary>
	/// The real time until we destroy the GameObject.
	/// </summary>
	[Property, ReadOnly] private TimeUntil TimeUntilDestroy { get; set; } = 0;

	protected override void OnStart()
	{
		TimeUntilDestroy = Time;
	}

	protected override void OnUpdate()
	{
		if ( TimeUntilDestroy )
		{
			GameObject.Destroy();
			return;
		}

		if ( Follow.IsValid() )
			WorldPosition = Follow.WorldPosition + Offset;
	}
}
