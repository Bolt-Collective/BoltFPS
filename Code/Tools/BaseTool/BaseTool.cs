using System.Text.Json.Serialization;

namespace Seekers;

public abstract class BaseTool : Component
{
	public virtual bool UseGrid => true;
	public ToolGun Parent { get; set; }
	public Pawn Owner { get; set; }

	public virtual IEnumerable<ToolHint> GetHints()
	{
		yield return new ToolHint( "attack1", "Primary" );
	}

	public virtual bool Primary( SceneTraceResult trace )
	{
		return false;
	}

	public virtual bool Secondary( SceneTraceResult trace )
	{
		return false;
	}

	public virtual bool Reload( SceneTraceResult trace )
	{
		return false;
	}

	public virtual void Disabled()
	{
	}

	public struct SelectionPoint
	{
		public GameObject GameObject { get; set; }
		public Vector3 LocalPosition { get; set; }
		public Vector3 LocalNormal { get; set; }

		public bool Active { get; set; } = false;

		public SelectionPoint( GameObject gameObject, Vector3 localPosition, Vector3 localNormal )
		{
			GameObject = gameObject;
			LocalPosition = localPosition;
			LocalNormal = localNormal;
		}

		public SelectionPoint( SceneTraceResult trace )
		{
			GameObject = trace.GameObject;
			LocalPosition = trace.GameObject.WorldTransform.PointToLocal( trace.HitPosition );
			LocalNormal = trace.GameObject.WorldTransform.WithPosition( 0 ).PointToLocal( trace.Normal );
		}

		public Vector3 WorldPosition => GameObject.WorldTransform.PointToWorld( LocalPosition );
		public Vector3 WorldNormal => GameObject.WorldTransform.WithPosition( 0 ).PointToWorld( LocalNormal );
	}
}

public class MaterialPath : Attribute
{
}
