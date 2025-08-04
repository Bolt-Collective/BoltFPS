namespace Seekers;
public abstract class BaseTool : Component
{
	public ToolGun Parent { get; set; }
	public Pawn Owner { get; set; }

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

		public SelectionPoint ( GameObject gameObject, Vector3 localPosition, Vector3 localNormal )
		{
			GameObject = gameObject;
			LocalPosition = localPosition;
			LocalNormal = localNormal;
		}

		public static SelectionPoint GetPoint(SceneTraceResult result)
		{
			return new SelectionPoint( result.GameObject, result.GameObject.WorldTransform.PointToLocal( result.HitPosition ), result.GameObject.WorldTransform.PointToWorld( result.Normal ) );
		}
	}
}
