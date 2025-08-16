using Sandbox.Citizen;

namespace Seekers;
public abstract partial class NPC : Knowable
{
	[Property] public string Name { get; set; }

	[Property] public Tool CurrentTool { get; set; }

	[Property] public Team Team { get; set; }

	public override Team TeamRef => Team;

	public virtual bool ScanForEnemies => true;

	public virtual bool UseTool(GameObject Target)
	{
		if ( !CurrentTool.IsValid() )
			return false;

		CurrentTool.ToolMode.Use(Target);

		return true;
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

		public CitizenAnimationHelper.HoldTypes HoldTypes { get; set; } = CitizenAnimationHelper.HoldTypes.Pistol;

		public GameObject Model { get; set; }

		public enum Specifiers
		{
			Weapon,
			Tool
		}
	}
}
