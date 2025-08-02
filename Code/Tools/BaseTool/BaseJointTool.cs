namespace Seekers;

public abstract class BaseJointTool : BaseTool
{
	GameObject selected;
	Vector3 selectedPos;

	public virtual bool CanConstraintToSelf => false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			var localPos = trace.GameObject.WorldTransform.PointToLocal( trace.HitPosition );

			if ( selected == null )
			{
				selected = trace.GameObject;
				selectedPos = localPos;
				return true;
			}

			if ( trace.GameObject == selected && !CanConstraintToSelf )
				return false;

			Join( selected, selectedPos, trace.GameObject, localPos );

			selected = null;
			return true;
		}

		return false;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "attack2" ) && trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			Disconnect( trace.GameObject );

			selected = null;
			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	public virtual void Disconnect(GameObject target)
	{

	}

	[Rpc.Broadcast]
	public virtual void Join(GameObject body1, Vector3 pos1, GameObject body2, Vector3 pos2)
	{
		
	}

	public (GameObject, GameObject) GetLocalPoints( GameObject body1, Vector3 pos1, GameObject body2, Vector3 pos2 )
	{
		var g1 = new GameObject();
		g1.SetParent( body1 );
		g1.LocalPosition = pos1;

		var g2 = new GameObject();
		g2.SetParent( body2 );
		g2.LocalPosition = pos2;

		g1.AddComponent<JointPoint>().otherPoint = g2;
		g2.AddComponent<JointPoint>().otherPoint = g1;

		return (g1, g2);
	}
}
