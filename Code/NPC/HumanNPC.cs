namespace Seekers;
public class HumanNPC : NPC
{
	public Knowable ClosestEnemy { get; set; }
	protected override void OnFixedUpdate()
	{
		ClosestEnemy = GetNearest(true)?.Knowable ?? null;

		if ( !ClosestEnemy.IsValid() )
			return;

		FindCover( ClosestEnemy );

		if (CurrentCover != null)
			Agent.MoveTo( CurrentCover.Position );
	}

	protected override void OnUpdate()
	{
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = CurrentCover == null ? Color.Red : Color.White;
		Gizmo.Draw.SolidSphere( WorldPosition, 5 );
		Gizmo.Draw.SolidSphere( ClosestEnemy?.Position ?? Vector3.Zero, 10 );
	}

}
