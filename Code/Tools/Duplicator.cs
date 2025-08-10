using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Seekers;

[Library( "tool_duplicator", Title = "Duplicator", Description = "Duplicate Contraptions", Group = "construction" )]
public partial class Duplicator : BaseTool
{
	[Property,Range(0, 2048)]
	public float Area { get; set; } = 1024;

	[Property,Sync]
	public bool SpawnAtOrigalPosition { get; set; }

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

		if (!Input.Down("run"))
		{
			Vector3 position = trace.HitPosition.WithZ( Owner.WorldPosition.z );
			var groundTrace = ToolGun.TraceTool( [Owner.GameObject, trace.GameObject], trace.HitPosition, trace.HitPosition + Vector3.Down * 1024 );
			if (groundTrace.Hit)
				position = groundTrace.HitPosition;
			SaveDupe( trace.GameObject, trace.GameObject.WorldTransform.PointToLocal( trace.HitPosition.WithZ(Owner.WorldPosition.z) ) );
			return true;
		}

		var gameObjects = Scene.FindInPhysics( BBox.FromPositionAndSize( Owner.WorldPosition, 1024 ) );
		var props = new List<GameObject>();
		foreach ( var gameObject in gameObjects )
		{
			if ( !gameObject.Root.Components.TryGet<PropHelper>( out var propHelper ) )
				continue;

			if ( props.Contains( gameObject.Root ) )
				continue;

			props.Add( gameObject.Root );
		}

		SaveDupe( props, Owner.WorldPosition);

		return true;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;

		if ( !Input.Down( "run" ) )
			return;
		Gizmo.Draw.IgnoreDepth = false;
		Gizmo.Draw.Color = Color.Green.WithAlpha( 0.5f );
		Gizmo.Draw.SolidBox( BBox.FromPositionAndSize( Owner.WorldPosition, 1024 ) );
		var gameObjects = Scene.FindInPhysics( BBox.FromPositionAndSize( WorldPosition, 1024 ) );
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
		
		var jsonObject = JsonObject.Parse( SavedDupe ).AsObject();

		SceneUtility.MakeIdGuidsUnique( jsonObject );

		dupe.Deserialize( jsonObject );

		if (!SpawnAtOrigalPosition)
			dupe.WorldPosition = position;

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
			prop.SetParent( dupe );
		}

		SavedDupe = dupe.Serialize().ToString();

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

		if ( props.Count <= 0 )
			return;

		var dupe = new GameObject();
		dupe.WorldPosition = gameObject.WorldTransform.PointToWorld( localPoint );

		foreach ( var prop in props )
		{
			prop.SetParent( dupe );
		}

		SavedDupe = dupe.Serialize().ToString();

		foreach (var prop in props)
		{
			prop.SetParent( null );
		}

		dupe.DestroyImmediate();
	}

}
