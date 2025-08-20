namespace Seekers;
public class HumanNPC : NPC
{
	public Knowable ClosestEnemy { get; set; }

	public Range hideTime = new Range( 10, 30 ); 

	public enum States
	{
		None,
		Cover,
		Attack,
		Reload
	}

	protected override void OnStart()
	{
		CoverGenerator.doGenerateCover = true;
	}

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( !Networking.IsHost )
			return;

		ClosestEnemy = GetNearest(true)?.Knowable ?? null;

		if ( !ClosestEnemy.IsValid() )
			return;

		var canHitEnemy = CanHitEnemy(ClosestEnemy);

		var enemyDir = (ClosestEnemy.GameObject.WorldPosition - WorldPosition).WithZ(0).Normal;
		GameObject.WorldRotation = Rotation.LookAt( enemyDir );

		AttackPosition();
	}

	public void Attack()
	{

	}

	public void AttackPosition()
	{
		FindCover( ClosestEnemy );

		if ( CurrentCover != null )
		{
			Agent.MoveTo( CurrentCover.Position );
			return;
		}

		var targetPosition = MaintainAttackDistance( ClosestEnemy );

		if ( targetPosition.HasValue )
		{
			Agent.MoveTo( targetPosition.Value );
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.SolidSphere( targetPosition.Value, 5 );
		}
	}

	protected override void OnUpdate()
	{
		Gizmo.Draw.IgnoreDepth = true;
		Gizmo.Draw.Color = Color.White;
		Gizmo.Draw.SolidSphere( WorldPosition, 5 );
	}

}
