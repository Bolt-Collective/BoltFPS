using static Sandbox.PhysicsContact;
using static Sandbox.Resources.ResourceCompileContext;

namespace Seekers;

public abstract class BaseJointTool : BaseTool
{
	public SelectionPoint selected = new();

	public virtual bool CanConstraintToSelf => false;

	public override IEnumerable<ToolHint> GetHints()
	{
		yield return new ToolHint( "attack1", "Select/Join" );
		yield return new ToolHint( "reload", "Disconnect" );
	}

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			var newSelectionPoint = new SelectionPoint( trace );
			newSelectionPoint.Active = true;
			if ( !selected.Active || !selected.GameObject.IsValid() )
			{
				selected = newSelectionPoint;
				return true;
			}

			if ( !selected.GameObject.IsValid() || !newSelectionPoint.GameObject.IsValid() )
				return false;

			if ( trace.GameObject == selected.GameObject && !CanConstraintToSelf )
				return false;

			Join( selected, newSelectionPoint );

			selected.Active = false;
			return true;
		}

		return false;
	}

	public void DisconnectTag( GameObject target, string tag)
	{
		if ( target.IsProxy )
			return;

		if ( !target.Components.TryGet( out PropHelper propHelper ) )
			return;

		foreach (var child in target.Children)
		{
			if ( !child.Tags.Contains( tag ) )
				continue;

			if ( child.Components.TryGet<Joint>(out var joint))
				propHelper.Joints.Remove( joint );

			if ( child.Components.TryGet<JointPoint>( out var jointPoint ))
			{
				jointPoint.otherPoint.Destroy();
			}

			child.Destroy();
		}
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( !trace.Hit )
			return false;

		if ( Input.Pressed( "reload" ) && trace.GameObject.Components.TryGet<PropHelper>( out var propHelper ) )
		{
			Disconnect( trace.GameObject );

			selected.Active = false;
			return true;
		}

		return false;
	}


	[Rpc.Broadcast]
	public virtual void Disconnect( GameObject target )
	{
	}

	[Rpc.Broadcast]
	public virtual void Join( SelectionPoint selection1, SelectionPoint selection2 )
	{
	}

	public static (GameObject, GameObject) GetJointPoints( SelectionPoint selection1, SelectionPoint selection2 )
	{
		var g1 = new GameObject();
		g1.SetParent( selection1.GameObject );
		g1.LocalPosition = selection1.LocalPosition;
		g1.LocalRotation = Rotation.LookAt( selection1.LocalNormal );

		var g2 = new GameObject();
		g2.SetParent( selection2.GameObject );
		g2.LocalPosition = selection2.LocalPosition;
		g2.LocalRotation = Rotation.LookAt( selection2.LocalNormal );

		g1.AddComponent<JointPoint>().otherPoint = g2;
		g2.AddComponent<JointPoint>().otherPoint = g1;

		g1.Network.AssignOwnership( Connection.Host );
		g1.NetworkSpawn();

		g2.Network.AssignOwnership( Connection.Host );
		g2.NetworkSpawn();

		return (g1, g2);
	}
}
