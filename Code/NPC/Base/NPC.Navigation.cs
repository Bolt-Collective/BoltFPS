using Sandbox.Navigation;
using System.IO;
using System.Net;

namespace Seekers;

public abstract partial class NPC : Knowable
{
	public NavMesh ActiveMesh => Game.ActiveScene.NavMesh;
	public virtual float MaxEngageDistance => CurrentTool?.MaxEngageDistance ?? 700;
	public virtual float IdealEngageDistance => CurrentTool?.IdealEngageDistance ?? 400;
	public virtual float MinEngageDistance => CurrentTool?.MinEngageDistance ?? 250;
	public virtual float DistancePadding => CurrentTool?.DistancePadding ?? 0.4f;
	public virtual float MinCoverAngle => 45;

	public SceneTraceResult WallCheck( Vector3 position, Vector3 direction, float size = 5, bool ignoreDynamic = true, GameObject[] ignore = null )
	{
		var trace = Game.ActiveScene.Trace.Ray( position, position + direction ).IgnoreGameObjectHierarchy(GameObject).WithoutTags("player", "npc", "movement").Size( size );

		if (ignore != null)
		{
			foreach ( var gameObject in ignore )
				trace = trace.IgnoreGameObjectHierarchy( gameObject );
		}

		if ( ignoreDynamic )
			trace = trace.IgnoreDynamic();

		return trace.Run();
	}
}

