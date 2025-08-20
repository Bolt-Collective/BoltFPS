namespace Seekers;

public abstract partial class NPC : Knowable
{
	public bool CanHitEnemy(Knowable Enemy)
	{
		var trace = Scene.Trace.Ray( Hold.WorldPosition, Enemy.WorldPosition + Vector3.Up * 32 ).UseHitboxes().IgnoreGameObjectHierarchy(GameObject).WithoutTags( "movement" ).Run();

		Gizmo.Draw.Line( Hold.WorldPosition, Enemy.WorldPosition + Vector3.Up * 32 );

		if ( !trace.Hit )
			return false;

		return trace.GameObject.Root == Enemy.GameObject.Root;
	}
}
