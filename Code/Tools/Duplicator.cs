using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Nodes;

namespace Seekers;

[Library( "tool_duplicator", Title = "Duplicator", Description = "Duplicates and saves contraptions" )]
[Group( "construction" )]
public partial class Duplicator : BaseTool
{

	public override bool UseGrid => useGrid;

	private bool useGrid = true;

	[Property]
	private FileBrowser FileBrowser { get; set; }
	
	[Property,Range(0, 2048)]
	public float SaveArea { get; set; } = 1024;

	[Property,Sync]
	public bool SpawnAtOriginalPosition { get; set; }

	[Sync( SyncFlags.FromHost )]
	public string SavedDupe { get; set; }

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !Input.Pressed( "attack1" ) )
			return false;

		SpawnDupe( trace.HitPosition );

		return true;
	}

	public override bool Secondary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !Input.Pressed( "attack2" ) )
			return false;

		var groundTrace = ToolGun.TraceTool( [Owner.GameObject, trace.GameObject], trace.HitPosition, trace.HitPosition + Vector3.Down * 1024 );

		if (!Input.Down("run"))
		{
			Vector3 position = trace.HitPosition.WithZ( Owner.WorldPosition.z );
			if (groundTrace.Hit)
				position = groundTrace.HitPosition;
			SaveDupe( trace.GameObject, trace.GameObject.WorldTransform.PointToLocal( trace.HitPosition.WithZ(Owner.WorldPosition.z) ) );
			return true;
		}

		var gameObjects = Scene.FindInPhysics( BBox.FromPositionAndSize( trace.HitPosition, 1024 ) );
		var props = new List<GameObject>();
		foreach ( var gameObject in gameObjects )
		{
			if ( !gameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				continue;

			if ( props.Contains( gameObject.Root ) )
				continue;

			props.Add( gameObject.Root );
		}

		SaveDupe( props, groundTrace.HitPosition);

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		useGrid = !Input.Down( "run" );

		if ( !Input.Down( "run" ) )
			return;

		if ( !Parent.IsValid() )
			return;

		var trace = Parent.BasicTraceTool();

		if ( !trace.Hit )
			return;	

		Gizmo.Draw.IgnoreDepth = false;
		Gizmo.Draw.Color = Color.Green.WithAlpha( 0.2f );
		Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( trace.EndPosition, 1024 ) );
		Gizmo.Draw.Color = Color.Green.WithAlpha( 0.5f );
		var gameObjects = Scene.FindInPhysics( BBox.FromPositionAndSize( trace.EndPosition, 1024 ) );
		var props = new List<PropHelper>();
		foreach ( var gameObject in gameObjects )
		{
			if ( !gameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				continue;

			if ( props.Contains( propHelper ) )
				continue;

			props.Add( propHelper );
		}

		foreach (var prop in props)
		{
			if ( prop.Components.TryGet<ModelRenderer>( out var modelRenderer ) )
				Gizmo.Draw.SolidBox(modelRenderer.Bounds);
		}
	}

	[Rpc.Host]
	public void SpawnDupe(Vector3 position)
	{
		if ( SavedDupe == null || SavedDupe == "" )
			return;

		var dupe = new GameObject();

		Log.Info( SavedDupe );
		
		var jsonObject = JsonObject.Parse( SavedDupe ).AsObject();

		SceneUtility.MakeIdGuidsUnique( jsonObject );

		dupe.Deserialize( jsonObject );

		if (!SpawnAtOriginalPosition)
			dupe.WorldPosition = position;

		foreach(var ownedEntity in dupe.Components.GetAll<OwnedEntity>(FindMode.EnabledInSelfAndDescendants))
		{
			ownedEntity.EntityOwner = Network.OwnerId;
		}

		var props = new List<GameObject>();
		foreach (var prop in new List<GameObject>(dupe.Children))
		{
			prop.SetParent( null );
			prop.NetworkSpawn();
			props.Add( prop );
		}

		UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Dupe", props.ToArray() );
		}, prop: props[0] );

		NoCollide.RestoreNoCollides( dupe.GetAllObjects( true ).ToList() );

		dupe.DestroyImmediate();
	}

	[Rpc.Host]
	public void SaveDupe( List<GameObject> props, Vector3 position )
	{
		if ( props.Count <= 0 )
			return;

		var dupe = new GameObject();
		dupe.WorldPosition = position;

		foreach ( var prop in props )
		{
			if ( prop.Root.Components.TryGet<PropHelper>(out var propHelper) )
			{
				prop.SetParent( dupe );
				Log.Info( prop.Name );	
			}
		}

		var dupeObject = dupe.Serialize();

		SceneUtility.MakeIdGuidsUnique( dupeObject );

		SavedDupe = dupeObject.ToString();

		foreach ( var prop in props )
		{
			prop.SetParent( null );
		}

		dupe.DestroyImmediate();
	}

	[Rpc.Host]
	public void SaveDupe(GameObject gameObject, Vector3 localPoint)
	{
		if ( !gameObject.Components.TryGet<PropHelper>( out var propHelper ) )
			return;

		var props = PhysGun.GetAllConnectedProps( gameObject );

		SaveDupe( props, gameObject.WorldTransform.PointToWorld( localPoint ) );
	}

}
