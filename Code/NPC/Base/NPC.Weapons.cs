namespace Seekers;

public abstract partial class NPC : Knowable
{
	public Vector3? CanHitEnemy(Knowable Enemy)
	{
		foreach (var shootTarget in Enemy.ShootTargets)
		{
			var trace = Scene.Trace.Ray( Hold.WorldPosition, shootTarget.WorldPosition ).UseHitboxes().IgnoreGameObjectHierarchy( GameObject ).WithoutTags( "movement" ).Run();

			if ( !trace.Hit )
				continue;

			if( trace.GameObject.Root == Enemy.GameObject.Root )
				return shootTarget.WorldPosition;
		}
		return null;
	}
}
