namespace Seekers;

public static partial class RigidbodyExtensions
{
	[Rpc.Broadcast]
	public static void BroadcastApplyForce( this Rigidbody self, Vector3 force )
	{
		self?.ApplyForce( force );
	}
}
