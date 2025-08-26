using System.Data;
using System.Diagnostics;
using static Sandbox.Physics.CollisionRules;
using static Sandbox.VideoWriter;

namespace Seekers;

[Library( "no_collide", Title = "No Collide", Description = "Removes Collison for props tag with the tool" )]
[Group( "construction" )]
public partial class NoCollide : BaseTool
{
	public override bool UseGrid => false;

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

			var guid = Guid.NewGuid();

			ApplyNoCollide( Selected, trace.GameObject, guid );

			var selectedObj = Selected;
			var targetObj = trace.GameObject;

			UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
			{
				if ( !selectedObj.IsValid() || !targetObj.IsValid() )
					return "skip";
				RemoveRules( targetObj, guid );
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
	public void RemoveRules(GameObject gameObject, Guid guid = default)
	{
		if ( !gameObject.IsValid() )
			return;

		foreach ( var tag in gameObject.Tags.ToArray() )
		{
			if ( !tag.StartsWith( "nocollide" ) )
				continue;

			var code = guid.ToString().Split( "-" ).First();

			if ( guid != default && !tag.Contains( code ) )
				continue;

			gameObject.Tags.Remove( tag );
		}
	}

	[Rpc.Host]
	public static void ApplyNoCollide( GameObject object1, GameObject object2, Guid guid )
	{
		var code = guid.ToString().Split("-").First();

		var tag1 = $"nocollide{code}A";
		var tag2 = $"nocollide{code}B";

		Pair rule = new Pair( tag1, tag2 );

		Result result = Result.Ignore;

		Game.ActiveScene.PhysicsWorld.CollisionRules.Pairs.Add( rule, result );

		object1.Tags.Add( tag1 );
		object2.Tags.Add( tag2 );
	}

	public static void RestoreNoCollides( List<GameObject> objects )
	{
		var codes = new Dictionary<string, (GameObject A, GameObject B)>();

		foreach ( var obj in objects )
		{
			foreach ( var tag in obj.Tags )
			{
				if ( tag.StartsWith( "nocollide" ) )
				{

					string withoutPrefix = tag.Substring( "nocollide".Length );

					if ( withoutPrefix.Length < 2 ) continue;

					string code = withoutPrefix.Substring( 0, withoutPrefix.Length - 1 );
					char side = withoutPrefix.Last();

					if ( !codes.ContainsKey( code ) )
						codes[code] = (null, null);

					if ( side == 'A' )
						codes[code] = (obj, codes[code].B);
					else if ( side == 'B' )
						codes[code] = (codes[code].A, obj);
				}
			}
		}

		foreach ( var kv in codes )
		{
			var code = kv.Key;
			var (objA, objB) = kv.Value;

			if ( !objA.IsValid() || !objB.IsValid() )
				return;

			string tag1 = $"nocollide{code}A";
			string tag2 = $"nocollide{code}B";

			var rule = new Pair( tag1, tag2 );
			Game.ActiveScene.PhysicsWorld.CollisionRules.Pairs.Add( rule, Result.Ignore );

			objA.Tags.Add( tag1 );
			objB.Tags.Add( tag2 );
		}
	}

}
