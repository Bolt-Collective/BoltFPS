namespace Seekers;

public class JointPoint : Component
{
	[Property]
	public GameObject otherPoint { get; set; }

	protected override void OnDestroy()
	{
		otherPoint?.Destroy();
	}
}
