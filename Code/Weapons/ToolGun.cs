using System.Text.Json.Nodes;

namespace Seekers;

[Library( "weapon_tool", Title = "Toolgun" )]
public partial class ToolGun : BaseWeapon
{
	[ConVar( "tool_current" )] public static string UserToolCurrent { get; set; } = "tool_boxgun";

	public BaseTool CurrentTool { get; set; }

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

		if(Owner?.Inventory.IsValid() ?? false)
			Owner.Inventory.CanChange = !Input.Down("run");

		if (Input.Down("run") && Input.MouseWheel.Length > 0.5f)
		{
			float x = Input.MouseWheel.y;
			int result = MathF.Sign( x );

			subDiv += result;

			subDiv = subDiv.Clamp( 1, 10 );
		}

		var trace = BasicTraceTool();

		if ( !trace.Hit )
			return;

		GameObject gameObject = trace.GameObject.Root;

		if ( !gameObject.IsValid() )
			return;

		BBox bounds = default;

		if (trace.GameObject.Root.Components.TryGet(out ModelPhysics modelPhysics) && trace.GameObject.Components.TryGet(out Collider collider))
		{
			gameObject = trace.GameObject;
			bounds = collider.LocalBounds;
		}
        else if ( trace.GameObject.Root.Components.TryGet(out ModelRenderer modelRenderer))
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
		foreach(var intersection in intersections)
		{
			var distance = intersection.Distance( trace.HitPosition );

			if ( distance > closestDistance )
				continue;

			closestIntersection = intersection;
			closestDistance = distance;
		}

		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.SolidSphere( closestIntersection, 0.5f );

		if ( !Input.Down( "run" ) || (Owner?.Controller?.WishMove ?? Vector3.Zero).Length > 0.1f )
			return;

		Owner.Controller.IgnoreCam = true;

		var direction = closestIntersection - Scene.Camera.WorldPosition;

		Owner.Controller.LookAt( closestIntersection );
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
		if ( !IsProxy && CurrentTool.IsValid())
			SaveTool( CurrentTool );
		//Particles.MakeParticleSystem( "particles/tool_hit.vpcf", new Transform( position ) );
		ViewModel?.Set( "b_attack", true );
		Owner.Controller?.BodyModelRenderer?.Set( "b_attack", true );
		Sound.Play( "sounds/balloon_pop_cute.sound", WorldPosition );
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

		if ( CurrentTool.IsValid() && lastTool != UserToolCurrent)
			SaveTool( CurrentTool );

		lastTool = UserToolCurrent;

		CurrentTool?.Destroy();
		CurrentTool = baseTool;
		CurrentTool.Owner = Owner;
		CurrentTool.Parent = this;

		

		GameObject.Network.Refresh();
	}

	public void SaveTool(Component tool)
	{
		var jsonNode = tool.Serialize();

		if ( !FileSystem.Data.DirectoryExists( "tool-data" ) )
			FileSystem.Data.CreateDirectory( "tool-data" );

		FileSystem.Data.WriteJson( $"tool-data/{tool.GetType().Name}.json", jsonNode );
	}

	public void LoadTool(Component tool)
	{
		if ( !FileSystem.Data.FileExists( $"tool-data/{tool.GetType().Name}.json" ) )
			return;

		var jsonObject = FileSystem.Data.ReadJson<JsonObject>( $"tool-data/{tool.GetType().Name}.json" );

		if(jsonObject != null)
			tool.DeserializeImmediately( jsonObject );
	}


	public SceneTraceResult TraceTool( Vector3 start, Vector3 end, float radius = 2.0f )
	{
		var trace = Scene.Trace.Ray( start, end )
				.UseHitboxes()
				.WithAnyTags( "solid", "nocollide", "npc", "glass" )
				.WithoutTags( "debris", "player" )
				.IgnoreGameObjectHierarchy( Owner.GameObject )
				.Size( radius );

		var tr = trace.Run();

		return tr;
	}

	public SceneTraceResult BasicTraceTool()
	{
		return TraceTool( Owner.AimRay.Position, Owner.AimRay.Position + Owner.AimRay.Forward * 5000 );
	}
}
