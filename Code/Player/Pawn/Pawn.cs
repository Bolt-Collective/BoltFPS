using Sandbox.Citizen;
using ShrimplePawns;

namespace Seekers;

[Pawn( "prefabs/player.prefab" )]
public partial class Pawn : ShrimplePawns.Pawn, IPlayerEvent
{
	public override Team TeamRef
	{
		get
		{
			if ( Owner.IsValid() && Owner.Team.IsValid() )
				return Owner.Team;

			return TeamManager.SpectatorsTeam;
		}
	}

	public override KnowledgeKind Kind => KnowledgeKind.Player;

	private static Pawn _local;

	public static Pawn Local
	{
		get
		{
			if ( !_local.IsValid() )
			{
				_local = Game.ActiveScene.GetAllComponents<Pawn>().FirstOrDefault( x => x.Network.IsOwner );
			}

			return _local;
		}
	}

	[Property] public PlayerInventory Inventory { get; set; }
	[Property] public SpotLight SpotLight { get; set; }
	[Property] public HealthComponent HealthComponent { get; set; }

	[Sync] public bool Flashlight { get; set; }

	[Sync] public Client Owner { get; set; }
	public float Zoom { get; set; } = 1f;
	public float FOVModifier { get; set; } = 1f;
	public float Stamina { get; set; } = 1;

	public float MaxHealth => HealthComponent?.MaxHealth ?? 100;
	public float Health => HealthComponent?.Health ?? 100;
	public bool IsDead => Health <= 0;

	[Property] public bool CanUse { get; set; } = true;


	public bool IsMe => Network.Owner == Connection.Local;

	private NormalMovement _controller;

	public NormalMovement Controller
	{
		get
		{
			if ( !_controller.IsValid() )
			{
				_controller = GetComponent<NormalMovement>();
			}

			return _controller;
		}
	}

	public CameraComponent Camera => Controller?.Camera ?? Scene?.Camera;

	public SkinnedModelRenderer Renderer => Controller.BodyModelRenderer;
	public AnimationHelper AnimationHelper => Controller.AnimationHelper;

	public Ray AimRay
	{
		get
		{
			var cam = Camera;

			if ( !cam.IsValid() )
				return new Ray();

			return new Ray( cam.WorldPosition,
				cam.WorldTransform.Forward );
		}
	}

	public ScreenShaker ScreenShaker => Controller.ScreenShaker;

	public void TakeDamage( float damage )
	{
		HealthComponent?.TakeDamage( this, damage );
	}

	public virtual Team Team
	{
		get => Owner.IsValid() ? Owner.Team : TeamManager.Instance.DefaultTeam;
		set
		{
			if ( !Owner.IsValid() )
			{
				return;
			}

			Owner.Team = value;
		}
	}

	protected override void OnStart()
	{
		if ( HealthComponent.IsValid() )
			HealthComponent.OnKilled += OnKilled;

		if ( Controller.IsValid() )
		{
			Controller.IgnoreLayers.Add( "prop" );
			Controller.IgnoreLayers.Add( "particles" );
			Controller.IgnoreLayers.Add( "projectile" );
			Controller.IgnoreLayers.Add( "player" );
		}
	}

	[Property] public GameObject DeathPrefab { get; set; }
	[Property] public Model DeadModel { get; set; }

	public virtual void OnKilled( DamageInfo damageInfo )
	{
		if ( DeathPrefab.IsValid() )
		{
			DeathPrefab.Clone( new Transform( WorldPosition, WorldRotation ) );
			GameObject.Destroy();
			return;
		}

		var bodyModel = Controller.BodyModelRenderer;
		bodyModel.GameObject.SetParent( Game.ActiveScene );
		bodyModel.AddComponent<TimedDestroyComponent>().Time = 5;
		bodyModel.UseAnimGraph = false;

		foreach ( var skinnedModelRenderer in bodyModel.Components.GetAll<SkinnedModelRenderer>() )
		{
			skinnedModelRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
		}

		if ( DeadModel.IsValid() )
			bodyModel.Model = DeadModel;


		bodyModel.Tags.Add( "ragdoll" );

		var modelPhysics = bodyModel.GetOrAddComponent<ModelPhysics>();
		modelPhysics.Model = bodyModel.Model;
		modelPhysics.Renderer = bodyModel;
		modelPhysics.MotionEnabled = true;

		foreach ( var body in modelPhysics.Bodies )
		{
			body.Component.Velocity += Controller.Velocity + damageInfo.Force / 15000;
		}

		GameObject.Destroy();
	}

	public override void OnAssign( ShrimplePawns.Client client )
	{
		Owner = client as Client;
	}

	[Property] public float StaminaDecay { get; set; } = 0.1f;
	[Property] public float StaminaDecayBoost { get; set; } = 1f;
	[Property] public float StaminaRegen { get; set; } = 0.05f;
	[Property] public float StaminaDelay { get; set; } = 5f;

	RealTimeSince _staminaUsedTime;

	/// <summary>
	/// True if the player wants the HUD not to draw right now
	/// </summary>
	public bool WantsHideHud
	{
		get
		{
			var weapon = GetComponent<PlayerInventory>()?.ActiveWeapon;
			if ( weapon.IsValid() && weapon.WantsHideHud )
				return true;

			return false;
		}
	}

	protected override void OnUpdate()
	{
		HealthComponent.IsGodMode = isGod;
		if ( !Owner.IsValid() )
			return;

		if ( !CanUse )
			return;

		Pickup();

		if ( Local?.Team == TeamManager.SpectatorsTeam )
		{
			var spectatorPawn = Local?.GetComponent<SpectatorPawn>();
			Controller.Orbiting = spectatorPawn?.Orbiting ?? false;


			if ( Owner.GetPawn().IsValid() && spectatorPawn?.SpectatedPawn == Owner?.GetPawn() )
				Controller.Spectating = true;
		}
		else
		{
			Controller.Spectating = false;
		}

		var ui = GetComponentInChildren<ScreenPanel>();

		if ( !ui.IsValid() )
			return;


		if ( WantsHideHud )
		{
			ui.Enabled = false;
		}
		else
		{
			ui.Enabled = true;
		}
	}

	[ConVar] public static bool god { get; set; } = false;

	[Sync] public bool isGod { get; set; }

	protected override void OnFixedUpdate()
	{
		if ( SpotLight.IsValid() )
			SpotLight.Enabled = Flashlight;

		if ( IsProxy )
			return;

		isGod = god;

		if ( !Owner.IsValid() )
			Owner = Client.Local;

		if ( Input.Pressed( "undo" ) )
			ConsoleSystem.Run( "undo" );

		if ( SpotLight.IsValid() && Input.Pressed( "flashlight" ) )
		{
			Flashlight = !Flashlight;
			SoundExtensions.BroadcastSound( Flashlight ? "flashlight-on" : "flashlight-off", WorldPosition );
		}

		if ( Controller.IsValid() && Controller.IsSprinting && !Controller.IgnoreMove &&
		     Controller.Velocity.WithZ( 0 ).Length > 5 && Controller.IsGrounded )
		{
			Stamina = (Stamina - StaminaDecay * StaminaDecayBoost * Time.Delta).Clamp( 0, 1 );
			_staminaUsedTime = 0;
		}
		else if ( _staminaUsedTime > StaminaDelay )
		{
			Stamina = (Stamina + StaminaRegen * Time.Delta).Clamp( 0, 1 );
		}

		if ( !Controller.IsValid() )
			return;

		Controller.EnableSprinting = Stamina > 0;

		if ( Input.Pressed( "View" ) && Controller.CanSetThirdPerson )
		{
			Controller.ThirdPerson = !Controller.ThirdPerson;
		}
	}

	public void OnCameraMove( ref Angles angles )
	{
		var ang = angles;
		Inventory?.ActiveWeapon?.OnCameraMove( this, ref ang );
	}

	public void OnCameraSetup( CameraComponent camera )
	{
		camera.FovAxis = CameraComponent.Axis.Vertical;
		camera.FieldOfView =
			Screen.CreateVerticalFieldOfView( (Preferences.FieldOfView / Zoom) * FOVModifier, 9.0f / 16.0f );
		Controller.AimSensitivityScale = 1 / Zoom;

		Inventory?.ActiveWeapon?.OnCameraSetup( this, camera );
	}

	[Rpc.Broadcast, ConCmd( "giveall" )]
	public static void GiveAllWeapons()
	{
		PlayerInventory.GiveWeapon( "usp" );
		PlayerInventory.GiveWeapon( "mp5" );
		PlayerInventory.GiveWeapon( "spaghelli m4" );
		PlayerInventory.GiveWeapon( "m4a1" );
		PlayerInventory.GiveWeapon( "m700" );
	}

	[ConCmd( "changeteam" )]
	public static void ChangeTeamCmd( string team )
	{
		Local.Owner.Team = ResourceLibrary.GetAll<Team>().FirstOrDefault( x => x.ResourceName == team );
		Local.Owner.Respawn( Local.Owner.Connection );
	}

	[ConCmd( "hurtme" )]
	public static void HurtMe( float damage )
	{
		Local?.TakeDamage( damage );
	}
}
