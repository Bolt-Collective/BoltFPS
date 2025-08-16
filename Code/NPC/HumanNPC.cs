namespace Seekers;
public class HumanNPC : NPC
{
	protected override void OnUpdate()
	{
		var poo = GetNearest(true);

		Gizmo.Draw.Line( WorldPosition, poo?.LastPos ?? Vector3.Zero );
	}
}
