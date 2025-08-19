using Sandbox.Navigation;
using System.IO;
using System.Net;

namespace Seekers;

public abstract partial class NPC : Knowable
{
	public NavMesh ActiveMesh => Game.ActiveScene.NavMesh;
	public virtual float MaxEngageDistance => CurrentTool?.MaxEngageDistance ?? 400;
	public virtual float IdealEngageDistance => CurrentTool?.IdealEngageDistance ?? 250;
	public virtual float MinEngageDistance => CurrentTool?.MinEngageDistance ?? 150;
	public virtual float MinCoverAngle => 45;
}

