using System.Data;
using System.Diagnostics;
using static Sandbox.Physics.CollisionRules;

namespace Seekers;

[Library( "no_collide", Title = "No Collide", Description = "Removes Collison for props tag with the tool" )]
[Group( "construction" )]
public partial class NoCollide : BaseTool
{
	[Sync(SyncFlags.FromHost)]
	public static int code { get; set; }

	GameObject Selected;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if(Selected == null)
			{
				Selected = trace.GameObject;
				return true;
			}

			var tag1 = $"nocollide{code}";
			var tag2 = $"nocollide{code+1}";

			IncreaseCode();

			BroadcastNoCollide( Selected, trace.GameObject, tag1, tag2 );

			var selectedObj = Selected;
			var targetObj = trace.GameObject;

			UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
			{
				if ( !selectedObj.IsValid() || !targetObj.IsValid() )
					return "skip";
				RemoveRules( targetObj );
				return "Undone NoCollide";
			}, prop: targetObj );

			Selected = null;
			return true;
		}

		return false;
	}

	public override bool Reload( SceneTraceResult trace )
	{
		if ( Input.Pressed( "reload" ) )
		{
			if ( Selected != null )
				return false;

			RemoveRules( trace.GameObject );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	public void RemoveRules(GameObject gameObject)
	{
		if ( !gameObject.IsValid() )
			return;

		foreach ( var tag in gameObject.Tags.ToArray() )
		{
			if ( tag.StartsWith( "nocollide" ) )
				gameObject.Tags.Remove( tag );
		}
	}

	[Rpc.Host]
	public void IncreaseCode()
	{
		code+=2;
	}

	[Rpc.Broadcast]
	private void BroadcastNoCollide( GameObject object1, GameObject object2, string tag1, string tag2 )
	{
		Pair rule = new Pair( tag1, tag2 );

		Result result = Result.Ignore;

		Scene.PhysicsWorld.CollisionRules.Pairs.Add( rule, result );

		object1.Tags.Add( tag1 );
		object2.Tags.Add( tag2 );
	}
}
