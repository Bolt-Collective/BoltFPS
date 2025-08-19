using System;
using static Sandbox.Connection;

namespace Seekers;
public interface IKnowable
{
	string KnowableID { get; }
	KnowledgeKind Kind { get; }
	Team Team { get; }
	Vector3 Position { get; }
	GameObject GameObject { get; }
}

public struct KnowledgeRecord
{
	public string Id;
	public KnowledgeKind Kind;
	public Knowable Knowable;
	public Team Team;
	public Vector3 LastPos;
	public float LastSeenTime;
	public int SeenCount;
	public float Threat;
	public GameObject Target;
}

public enum KnowledgeKind
{
	Default,
	Player,
	NPC,
	Interactable,
	Obstacle
}

public abstract class Knowable : Component, IKnowable
{
	[Property] public string KnowableID { get; set; } = Guid.NewGuid().ToString();
	public virtual KnowledgeKind Kind => KnowledgeKind.NPC;
	public virtual Team TeamRef => null;

	public Vector3 Position => GameObject.WorldPosition;
	Team IKnowable.Team => TeamRef;
	GameObject IKnowable.GameObject => GameObject;

	public Dictionary<string, KnowledgeRecord> Memory { get; private set; } = new();

	public virtual float GetThreat(IKnowable knowable)
	{
		return 1;
	}

	public KnowledgeRecord? GetNearest( bool onlyEnemies = false, KnowledgeKind kind = default)
	{
		KnowledgeRecord? best = null;
		var bestDist = float.MaxValue;

		foreach ( var rec in Memory.Values )
		{
			if ( onlyEnemies && !TeamRef.IsEnemy( rec.Team ) )
				continue;
			
			if ( kind != default && rec.Kind != kind )
				continue;
			
			var distance = Position.DistanceSquared( rec.LastPos );
			if ( distance < bestDist )
			{
				best = rec;
				bestDist = distance;
			}
		}

		return best;
	}
}

public class KnowledgeScanner : GameObjectSystem
{
	public KnowledgeScanner( Scene scene ) : base( scene )
	{
		Listen( Stage.PhysicsStep, 10, Scan, "DoingSomething" );
	}

	TimeUntil nextScan;

	void Scan()
	{
		if ( !Networking.IsHost )
			return;

		if ( nextScan > 0 )
			return;

		nextScan = 1;

		foreach ( var target in Scene.GetAllComponents<Knowable>() )
		{
			if ( target is not IKnowable knowable )
				continue;
			if ( !knowable.GameObject.IsValid() )
				continue;
			if (knowable.Kind != KnowledgeKind.NPC)
				continue;

			Scan( target );
		}
	}

	void Scan( Knowable thisKnowable )
	{
		foreach ( var target in Scene.GetAllComponents<Knowable>() )
		{
			if ( target is not IKnowable knowable )
				continue;
			if ( !knowable.GameObject.IsValid() )
				continue;

			if ( thisKnowable.Position.Distance( knowable.Position ) > 2048 )
				continue;

			if ( thisKnowable.Memory.TryGetValue( knowable.KnowableID, out var rec ) )
			{
				rec.LastPos = knowable.Position;
				rec.LastSeenTime = Time.Now;
				rec.SeenCount++;
				rec.Threat = MathF.Max( rec.Threat, 1 );
				thisKnowable.Memory[knowable.KnowableID] = rec;
			}
			else
			{
				thisKnowable.Memory[knowable.KnowableID] = new KnowledgeRecord
				{
					Id = knowable.KnowableID,
					Knowable = target,
					Kind = knowable.Kind,
					Team = knowable.Team,
					LastPos = knowable.Position,
					LastSeenTime = Time.Now,
					SeenCount = 1,
					Threat = thisKnowable.GetThreat( knowable ),
					Target = knowable.GameObject
				};
			}

		}
	}
}
