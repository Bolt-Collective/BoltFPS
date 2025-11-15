using Sandbox.Citizen;
using Sandbox.VR;
using System.IO;

namespace Seekers;

public abstract partial class NPC : Knowable
{
	[Property] public string Name { get; set; }
	[Property] public string Catagory { get; set; } = "Core";
	[Property, ImageAssetPath] public string Icon { get; set; }

	[Property, RequireComponent] public NavMeshAgent Agent { get; set; }
	[Property, RequireComponent] public HealthComponent HealthComponent { get; set; }

	[Property, Sync] public NPCToolResource CurrentTool { get; set; }

	[Property] public Team Team { get; set; }

	[Property] public GameObject Hold { get; set; }

	public override Team TeamRef => Team;

	public virtual bool ScanForEnemies => true;

	public class StateManager<T> where T : Enum
	{
		private Dictionary<T, float> weights;

		public StateManager()
		{
			weights = Enum.GetValues( typeof(T) )
				.Cast<T>()
				.ToDictionary( v => v, v => 0f );
		}

		public float Get( T value ) => weights[value];
		public void Set( T value, float weight ) => weights[value] = weight;
		public void Change( T value, float weight ) => weights[value] = (weights[value] + weight).Clamp( 0, 1 );

		public T GetBest()
		{
			return weights.Aggregate( ( l, r ) => l.Value > r.Value ? l : r ).Key;
		}
	}

	public virtual bool UseTool( GameObject Target )
	{
		if ( !CurrentTool.IsValid() )
			return false;

		CurrentTool.ToolMode.Use( Target );

		return true;
	}

	protected override void OnStart()
	{
		Agent.Enabled = true;
		if ( HealthComponent.IsValid() )
			HealthComponent.OnKilled += OnKilled;
	}

	[ConVar] public static bool ai_disable { get; set; } = false;

	RealTimeSince failedMoving { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( (Agent.Velocity.Length > 5 && Agent.TargetPosition.HasValue &&
		     Agent.TargetPosition.Value.Distance( WorldPosition ) > 5) || !Agent.Enabled )
			failedMoving = 0;

		if ( failedMoving > 2 )
		{
			Agent.Enabled = false;
			Agent.Enabled = true;
			failedMoving = 0;
		}

		ToolVisuals();
		previousTool = CurrentTool;

		Animate();

		if ( !Networking.IsHost )
			return;
		
		if ( ai_disable )
			return;
		Think();
	}

	public virtual void Think()
	{
	}

	public virtual void OnKilled( DamageInfo damageInfo )
	{
	}


	public virtual void Animate()
	{
	}

	private NPCToolResource previousTool;
	public GameObject ToolObject;

	public void ToolVisuals()
	{
		if ( !Hold.IsValid() )
			return;

		if ( !CurrentTool.IsValid() || previousTool != CurrentTool )
		{
			foreach ( var child in Hold.Children )
				child.Destroy();

			return;
		}

		if ( Hold.Children.Count > 0 )
			return;

		var toolModel = CurrentTool.Model.Clone();

		var toolComponent = toolModel.GetComponent<NPCTool>();
		if ( toolComponent.IsValid() )
			toolComponent.Owner = this;

		ToolObject = toolModel;
		toolModel.SetParent( Hold );
		toolModel.LocalTransform = new();
	}

	public abstract class ToolMode
	{
		public virtual void Use( GameObject Target ) { }
	}

	[AssetType( Name = "NPCTool", Extension = "npctool" )]
	public class NPCToolResource : GameResource
	{
		public string Name { get; set; }
		public string Category { get; set; }
		public ToolMode ToolMode { get; set; }
		public Specifiers Specification { get; set; } = Specifiers.Weapon;

		public float MaxEngageDistance { get; set; } = 700;
		public float IdealEngageDistance { get; set; } = 512;
		public float MinEngageDistance { get; set; } = 256;
		public float DistancePadding { get; set; } = 0.4f;

		public AnimationHelper.HoldTypes HoldTypes { get; set; } = AnimationHelper.HoldTypes.Pistol;

		public GameObject Model { get; set; }

		public enum Specifiers
		{
			Weapon,
			Tool
		}
	}

	public void CreateRagdoll( SkinnedModelRenderer body, DamageInfo damageInfo, Model replacement = null )
	{
		if ( !body.IsValid() )
			return;

		body.GameObject.SetParent( Game.ActiveScene );
		body.GameObject.DestroyAsync(15f);
		body.UseAnimGraph = false;
		body.RenderType = ModelRenderer.ShadowRenderType.On;

		if ( replacement.IsValid() )
			body.Model = replacement;
		body.Tags.Add( "ragdoll" );

		ModelPhysics modelPhysics = null;

		if ( body.Components.TryGet<ModelPhysics>( out var mp, FindMode.EverythingInSelf ) )
			modelPhysics = mp;
		else
			body.AddComponent<ModelPhysics>();

		modelPhysics.Enabled = true;
		modelPhysics.Model = body.Model;
		modelPhysics.Renderer = body;
		modelPhysics.MotionEnabled = true;
		foreach ( var bod in modelPhysics.Bodies )
		{
			bod.Component.Velocity += Agent.Velocity + damageInfo.Force / 15000;
		}
	}

	public static float GetRandomValue( RangedFloat rangedFloat )
	{
		return Game.Random.Next( (int)MathF.Round( rangedFloat.Min * 100 ),
			(int)MathF.Round( rangedFloat.Max * 100 ) ) / 100f;
	}
}
