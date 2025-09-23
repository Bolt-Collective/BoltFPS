using System.Text.Json.Nodes;

namespace Seekers;

[Library( "weapon_tool", Title = "Toolgun" )]
public partial class ToolGun : BaseWeapon
{
	public static string UserToolCurrent { get; set; }

	public static BaseTool CurrentTool { get; set; }

	[Feature( "Effects" )] [Property] public GameObject SuccessImpactEffect { get; set; }

	[Feature( "Effects" )] [Property] public GameObject SuccessBeamEffect { get; set; }

	protected override void OnEnabled()
	{
		base.OnEnabled();
		if ( IsProxy )
			return;
	}

	protected override void OnDisabled()
	{
		base.OnDisabled();
		if ( IsProxy )
			return;

		if ( CurrentTool.IsValid() )
			SaveTool( CurrentTool );

		CurrentTool?.Disabled();
	}

	string lastTool;

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		if ( lastTool != UserToolCurrent )
		{
			UpdateTool();
		}
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		if ( IsProxy )
			return;


		Seekers.CurrentTool.Show = this.Active;

		if ( Owner?.Inventory.IsValid() ?? false )
			Owner.Inventory.CanChange = !Input.Down( "run" );

		if ( Input.Down( "run" ) && Input.MouseWheel.Length > 0.5f )
		{
			float x = Input.MouseWheel.y;
			int result = MathF.Sign( x );

			subDiv += result;

			subDiv = subDiv.Clamp( 1, 15 );
		}

		var trace = BasicTraceTool();

		if ( !trace.Hit )
			return;

		GameObject gameObject = trace.GameObject;

		if ( !gameObject.IsValid() )
			return;

		if ( !CurrentTool.IsValid() || !CurrentTool.UseGrid )
			return;

		BBox bounds = default;

		if ( trace.GameObject.Root.Components.TryGet( out ModelPhysics modelPhysics ) &&
		     trace.GameObject.Components.TryGet( out Collider collider ) )
		{
			gameObject = trace.GameObject;
			bounds = collider.LocalBounds;
		}
		else if ( trace.GameObject.Components.TryGet( out ModelRenderer modelRenderer ) )
		{
			bounds = modelRenderer.LocalBounds;
		}

		if ( bounds == default )
			return;

		var intersections = CreateGrid( bounds, gameObject, trace );

		if ( !Owner?.Controller.IsValid() ?? true )
			return;

		Owner.Controller.IgnoreCam = false;

		if ( intersections.Count <= 0 )
			return;

		Vector3 closestIntersection = intersections[0];
		float closestDistance = 100000;
		foreach ( var intersection in intersections )
		{
			var distance = intersection.Distance( trace.HitPosition );

			if ( distance > closestDistance )
				continue;

			closestIntersection = intersection;
			closestDistance = distance;
		}

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.SolidSphere( closestIntersection, 0.5f );

		if ( !Input.Down( "run" ) || (Owner?.Controller?.wishDirection ?? Vector3.Zero).Length > 0.1f )
			return;

		Owner.Controller.IgnoreCam = true;

		var direction = closestIntersection - Scene.Camera.WorldPosition;

		Owner.Controller.LookAt( closestIntersection );
	}

	public override void OnControl()
	{
		UpdateScreen();

		base.OnControl();
	}

	public override void AttackPrimary()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Primary( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition );
	}

	public override void AttackSecondary()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Secondary( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition );
	}

	public override void Reload()
	{
		var trace = BasicTraceTool();

		if ( !(CurrentTool?.Reload( trace ) ?? false) )
			return;

		ToolEffects( trace.EndPosition );
	}

	[Rpc.Broadcast]
	void ToolEffects( Vector3 position )
	{
		if ( !IsProxy && CurrentTool.IsValid() )
			SaveTool( CurrentTool );
		ViewModel?.Set( "b_attack", true );
		Owner.Controller?.BodyModelRenderer?.Set( "b_attack", true );

		var snd = Sound.Play( ShootSound, WorldPosition );
		snd.SpacialBlend = Owner.IsValid() && Owner.IsMe ? 0.0f : 1.0f;

		if ( SuccessImpactEffect is { } impactPrefab )
		{
			var impact = impactPrefab.Clone( new Transform( position ), null, false );
			impact.NetworkSpawn();
			impact.Enabled = true;
		}

		if ( SuccessBeamEffect is { } beamEffect )
		{
			var go = beamEffect.Clone( new Transform( Attachment( "muzzle" ).Position ), null, false );

			foreach ( var beam in go.GetComponentsInChildren<BeamEffect>( true ) )
			{
				beam.TargetPosition = position;
			}

			go.NetworkSpawn();
			go.Enabled = true;
		}
	}

	public void UpdateTool()
	{
		var comp = TypeLibrary.GetType<BaseTool>( UserToolCurrent );

		if ( comp == null )
			return;

		var tool = Components.Create( comp, true );

		var baseTool = tool as BaseTool;

		if ( !baseTool.IsValid() )
		{
			tool?.Destroy();
		}

		LoadTool( tool );
		tool.Enabled = true;

		if ( CurrentTool.IsValid() && lastTool != UserToolCurrent )
			SaveTool( CurrentTool );

		lastTool = UserToolCurrent;

		CurrentTool?.Destroy();
		CurrentTool = baseTool;
		CurrentTool.Owner = Owner;
		CurrentTool.Parent = this;


		GameObject.Network.Refresh();
	}

	public void SaveTool( Component tool )
	{
		var jsonNode = tool.Serialize();

		if ( !FileSystem.Data.DirectoryExists( "tool-data" ) )
			FileSystem.Data.CreateDirectory( "tool-data" );

		FileSystem.Data.WriteJson( $"tool-data/{tool.GetType().Name}.json", jsonNode );
	}

	public void LoadTool( Component tool )
	{
		if ( !FileSystem.Data.FileExists( $"tool-data/{tool.GetType().Name}.json" ) )
			return;

		var jsonObject = FileSystem.Data.ReadJson<JsonObject>( $"tool-data/{tool.GetType().Name}.json" );

		if ( jsonObject != null )
			tool.DeserializeImmediately( jsonObject );
	}


	public static SceneTraceResult TraceTool( GameObject[] ignors, Vector3 start, Vector3 end, float radius = 0f )
	{
		var trace = Game.ActiveScene.Trace.Ray( start, end )
			.UseHitboxes()
			.WithAnyTags( "solid", "nocollide", "npc", "glass", "worldprop" )
			.WithoutTags( "debris", "player", "movement" )
			.Size( radius );

		foreach ( var ignore in ignors )
		{
			trace = trace.IgnoreGameObjectHierarchy( ignore );
		}

		var tr = trace.Run();

		return tr;
	}

	public SceneTraceResult BasicTraceTool()
	{
		if ( !Owner.IsValid() || !Owner.GameObject.IsValid() )
			return default;
		return TraceTool( [Owner.GameObject], Owner.AimRay.Position,
			Owner.AimRay.Position + Owner.AimRay.Forward * 5000 );
	}
}
