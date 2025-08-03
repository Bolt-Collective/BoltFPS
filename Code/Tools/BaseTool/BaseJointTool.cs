namespace Seekers;

public abstract class BaseJointTool : BaseTool
{
	SelectionPoint selected;

	public virtual bool CanConstraintToSelf => false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			var newSelectionPoint = SelectionPoint.GetPoint( trace );
			if ( selected == null )
			{
				selected = newSelectionPoint;
				return true;
			}

			if ( trace.GameObject == selected.GameObject && !CanConstraintToSelf )
				return false;

			Join( selected, newSelectionPoint );

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
	public virtual void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
		
	}

	public (GameObject, GameObject) GetJointPoints( SelectionPoint selection1, SelectionPoint selection2 )
	{
		var g1 = new GameObject();
		g1.SetParent( selection1.GameObject );
		g1.LocalPosition = selection1.LocalPosition;

		var g2 = new GameObject();
		g2.SetParent( selection2.GameObject );
		g2.LocalPosition = selection2.LocalPosition;

		g1.AddComponent<JointPoint>().otherPoint = g2;
		g2.AddComponent<JointPoint>().otherPoint = g1;

		return (g1, g2);
	}
}
