
using Seekers;

public class DoorLink : NavMeshLink
{
	protected override void OnLinkEntered( NavMeshAgent agent )
	{
		if ( !agent.GameObject.Root.Components.TryGet<ZombieNPC>( out var zombie ) )
			return;

		zombie.DoorLink( this, agent.CurrentLinkTraversal.Value.AgentInitialPosition, agent.CurrentLinkTraversal.Value.LinkExitPosition );
	}
}
