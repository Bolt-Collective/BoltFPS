
namespace Seekers;

public partial class Client : ShrimplePawns.Client
{
	public Transform FindSpawnLocation()
	{
		//
		// If we have any SpawnPoint components in the scene, then use those
		//
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();
		if ( spawnPoints.Length > 0 )
		{
			var transform = Random.Shared.FromArray( spawnPoints ).Transform.World;
			return transform.WithPosition( transform.Position + Vector3.Up * 5 );
		}

		//
		// Failing that, spawn where we are
		//
		return new Transform();
	}
}
