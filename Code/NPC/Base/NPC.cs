using Sandbox.Citizen;

namespace Seekers;
public abstract partial class NPC : Knowable
{
	[Property] public string Name { get; set; }

	[Property, RequireComponent] public NavMeshAgent Agent { get; set; }

	[Property, Sync] public Tool CurrentTool { get; set; }

	[Property] public Team Team { get; set; }

	[Property] public GameObject Hold { get; set; }

	public override Team TeamRef => Team;

	public virtual bool ScanForEnemies => true;

	public virtual bool UseTool(GameObject Target)
	{
		if ( !CurrentTool.IsValid() )
			return false;

		CurrentTool.ToolMode.Use(Target);

		return true;
	}

	protected override void OnFixedUpdate()
	{
		ToolVisuals();
		previousTool = CurrentTool;
		if ( !Networking.IsHost )
			return;
	}

	private Tool previousTool;
	public void ToolVisuals()
	{
		
		if ( !CurrentTool.IsValid() || previousTool != CurrentTool )
		{
			foreach ( var child in Hold.Children )
				child.Destroy();

			return;
		}

		if ( Hold.Children.Count > 0 )
			return;

		var toolModel = CurrentTool.Model.Clone();

		toolModel.SetParent( Hold );
		toolModel.LocalTransform = new();
	}

	public abstract class ToolMode
	{
		public virtual void Use(GameObject Target) { }
	}

	[GameResource( "NPCTool", "npctool", "A reference to the tool type", Icon = "🔧" )]
	public class Tool : GameResource
	{
		public ToolMode ToolMode { get; set; }
		public Specifiers Specification { get; set; } = Specifiers.Weapon;

		public float MaxEngageDistance { get; set; } = 700;
		public float IdealEngageDistance { get; set; } = 512;
		public float MinEngageDistance { get; set; } = 256;
		public float DistancePadding { get; set; } = 0.4f;

		public CitizenAnimationHelper.HoldTypes HoldTypes { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;

		public GameObject Model { get; set; }

		public enum Specifiers
		{
			Weapon,
			Tool
		}
	}
}
