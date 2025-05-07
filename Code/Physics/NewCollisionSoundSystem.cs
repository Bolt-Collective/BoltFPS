namespace Seekers.Physics;

/// <summary>
/// This system exists to collect pending collision sounds and filter them into a unique set, to avoid
/// unnesssary sounds playing, when they're going to be making the same sound anyway.
/// </summary>
public partial class NewCollisionSoundSystem : GameObjectSystem<NewCollisionSoundSystem>, ISceneCollisionEvents
{
	record struct PendingSound( Surface surface, Vector3 position, float speed, float priority );

	List<PendingSound> Pending = new List<PendingSound>();

	public NewCollisionSoundSystem( Scene scene ) : base( scene )
	{
		Listen( Stage.FinishUpdate, 100, ProcessQueue, "NewCollisionSoundSystem Queue" );
		DeleteOriginalCollisionSoundSystem();
	}

	public void DeleteOriginalCollisionSoundSystem()
	{
		if ( CollisionSoundSystem.Current != null )
		{
			CollisionSoundSystem.Current.Dispose();
		}
	}

	/// <summary>
	/// Register this physics collision with the sound system
	/// </summary>
	public void RegisterCollision( in Collision collision )
	{
		DeleteOriginalCollisionSoundSystem();
		if ( !collision.Self.Body.EnableCollisionSounds ) return;
		if ( !collision.Other.Body.EnableCollisionSounds ) return;

		/*		var othersurface = collision.Other.Surface;

				// FIXED! HACK HACK: use this to get the real surface when colliding with the map, see https://github.com/Facepunch/sbox-issues/issues/7123
				if ( collision.Other.GameObject.Components.TryGet<MapCollider>( out var map ) )
				{
					var tr = Scene.Trace.Ray( collision.Contact.Point + (collision.Contact.Speed.Normal * 2), collision.Contact.Point - collision.Contact.Speed ).IgnoreDynamic().IgnoreKeyframed().Run();
					//DebugOverlaySystem.Current.Line( tr.StartPosition, tr.EndPosition );
					othersurface = tr.Surface;
				}
		*/
		AddShapeCollision( collision.Other.Shape, collision.Other.Surface, collision.Contact );
		AddShapeCollision( collision.Self.Shape, collision.Self.Surface, collision.Contact );
	}

	void ISceneCollisionEvents.OnCollisionStart( Collision collision )
	{
		RegisterCollision( collision );
	}

	void ISceneCollisionEvents.OnCollisionUpdate( Collision collision )
	{
		RegisterCollision( collision );
	}

	/// <summary>
	/// Add a collision sound for this shape
	/// </summary>
	public void AddShapeCollision( PhysicsShape shape, Surface surface, in Vector3 position, float speed )
	{
		if ( !shape.IsValid() ) return;
		if ( !shape.Body.IsValid() ) return;
		if ( !shape.Body.EnableCollisionSounds ) return;
		if ( speed < 50.0f ) return;
		if ( surface == null ) return;


		// These are sent through an RPC now so these are worth crap
		//var listenerDistance = Sound.Listener.Position.Distance( position );

		// this is a bad attempt at ignoring distant, hardly doing anything sounds before it hits the sound system
		//if ( speed < listenerDistance * 0.1f ) return;

		float priority = speed; // - listenerDistance;

		// If we have more than 10, remove any that are slower/less significant
		if ( Pending.Count > 10 && Pending.RemoveAll( x => x.speed < speed ) == 0 )
			return;

		// check for redundancies
		for ( int i = 0; i < Pending.Count; i++ )
		{
			if ( Pending[i].surface.Index != surface.Index ) continue;

			// already playing a better one, ignore this shit one
			if ( Pending[i].priority >= priority ) return;

			// replace this one
			Pending[i] = new PendingSound( surface, position, speed, priority );
			return;
		}

		Pending.Add( new PendingSound( surface, position, speed, priority ) );
	}

	/// <summary>
	/// Add a collision sound for this shape
	/// </summary>
	public void AddShapeCollision( PhysicsShape shape, Surface surface, in PhysicsContact contact )
	{
		var point = contact.Point + (contact.Normal * -2);
		AddShapeCollision( shape, surface, point, MathF.Abs( contact.NormalSpeed ) );
	}

	RealTimeSince lastRan = 0;

	/// <summary>
	/// Create the pending sounds
	/// </summary>
	void ProcessQueue()
	{
		if ( lastRan < 0.05f ) return;
		lastRan = 0;

		foreach ( var pending in Pending )
		{
			if ( pending.surface == null ) continue;
			BroadcastSound( pending.surface, pending.position, pending.speed );
		}

		Pending.Clear();
	}

	[Rpc.Broadcast( NetFlags.Unreliable )]
	static void BroadcastSound( Surface surf, Vector3 position, float speed )
	{
		surf.PlayPhysicsCollisionSound( position, speed );
	}
}
