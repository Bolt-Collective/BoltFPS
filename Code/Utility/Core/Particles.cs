namespace Seekers;

public static class Particles
{
	public static GameObject CreateParticleSystem( GameObject prefab, Transform transform, float time = 1, GameObject parent = null )
	{
		SpawnParticleSystem( Connection.Local.Id, prefab, transform.Position, transform.Rotation, time, parent );

		return MakeParticleSystem( prefab, transform, time, parent );
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	public static void SpawnParticleSystem( Guid connection, GameObject prefab, Vector3 position, Rotation rotation, float time = 1, GameObject parent = null )
	{
		if ( Connection.Local.Id == connection )
			return;

		MakeParticleSystem( prefab, new Transform( position, rotation ), time, parent );
	}

	public static GameObject MakeParticleSystem( GameObject particle, Transform transform, float time = 1, GameObject parent = null )
	{
		if ( !particle.IsValid() )
			return null;

		var go = particle.Clone();
		go.SetParent( parent );
		go.WorldTransform = transform;
		var particleEffect = particle.Components.Get<ParticleEffect>( FindMode.EnabledInSelfAndChildren );
		if ( particleEffect.IsValid() )
		{
			particleEffect.Yaw = transform.Rotation.Yaw();
			particleEffect.Pitch = transform.Rotation.Pitch();
		}

		if ( time > 0 )
			go.DestroyAsync( time );

		return go;
	}
}
