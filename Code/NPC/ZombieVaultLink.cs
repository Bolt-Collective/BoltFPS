
using Seekers;

public class ZombieVaultLink : NavMeshLink
{
	protected override void OnLinkEntered( NavMeshAgent agent )
	{
		if ( !agent.GameObject.Root.Components.TryGet<ZombieNPC>( out var zombie ) )
			return;

		zombie.VaultLink( this );
	}
}
