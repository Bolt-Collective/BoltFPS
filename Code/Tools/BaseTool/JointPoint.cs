namespace Seekers;

public class JointPoint : Component
{
	public GameObject otherPoint;

	protected override void OnDestroy()
	{
		otherPoint?.Destroy();
	}
}
