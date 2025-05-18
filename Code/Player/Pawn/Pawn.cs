using Sandbox.Citizen;
using ShrimplePawns;
using XMovement;

namespace Seekers;

[Pawn( "prefabs/player.prefab" )]
public partial class Pawn : ShrimplePawns.Pawn
{
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
	public float Stamina { get; set; } = 1;

	public float MaxHealth => HealthComponent?.MaxHealth ?? 0;
	public float Health => HealthComponent?.Health ?? 0;
	public bool IsDead => Health <= 0;

	public bool IsMe => Network.Owner == Connection.Local;

	private PlayerWalkControllerComplex _controller;

	public PlayerWalkControllerComplex Controller
	{
		get
		{
			if ( !_controller.IsValid() )
			{
				_controller = GetComponent<PlayerWalkControllerComplex>();
			}

			return _controller;
		}
	}

	public SkinnedModelRenderer Renderer => Controller.BodyModelRenderer;
	public CitizenAnimationHelper AnimationHelper => Controller.AnimationHelper;

	public Ray AimRay =>
		new Ray( Controller.Head.WorldTransform.PointToWorld( Controller.Camera.LocalPosition.WithX( 0 ) ),
			Controller.Camera.WorldTransform.Forward );

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
			Controller.Controller.IgnoreLayers.Add( "prop" );
			Controller.Controller.IgnoreLayers.Add( "particles" );
			Controller.Controller.IgnoreLayers.Add( "projectile" );
			Controller.Controller.IgnoreLayers.Add( "player" );
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

		Controller.BodyModelRenderer.GameObject.SetParent( Game.ActiveScene );
		Controller.BodyModelRenderer.AddComponent<TimedDestroyComponent>().Time = 5;
		Controller.BodyModelRenderer.UseAnimGraph = false;
		Controller.BodyModelRenderer.RenderType = ModelRenderer.ShadowRenderType.On;
		if ( DeadModel.IsValid() )
			Controller.BodyModelRenderer.Model = DeadModel;
		Controller.BodyModelRenderer.Tags.Add( "ragdoll" );

		var modelPhysics = Controller.BodyModelRenderer.AddComponent<ModelPhysics>();
		modelPhysics.Model = Controller.BodyModelRenderer.Model;
		modelPhysics.Renderer = Controller.BodyModelRenderer;
		modelPhysics.PhysicsGroup?.AddVelocity( Controller.Controller.Velocity + damageInfo.Force / 15000 );

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

	protected override void OnUpdate()
	{
		Pickup();

		ClippingPrevention();

		if ( Local?.Team == TeamManager.SpectatorsTeam )
		{
			var spectatorPawn = Local?.GetComponent<SpectatorPawn>();
			if ( spectatorPawn?.SpectatedClient == Owner )
				Controller.Spectating = true;
		}
		else
		{
			Controller.Spectating = false;
		}
	}

	protected override void OnFixedUpdate()
	{
		if ( SpotLight.IsValid() )
			SpotLight.Enabled = Flashlight;

		if ( IsProxy )
			return;

		if ( !Owner.IsValid() )
			Owner = Client.Local;

		if ( SpotLight.IsValid() && Input.Pressed( "flashlight" ) )
		{
			Flashlight = !Flashlight;
			SoundExtensions.BroadcastSound( Flashlight ? "flashlight-on" : "flashlight-off", WorldPosition );
		}

		if ( Controller.IsValid() && Controller.IsRunning && Controller.EnableWalking &&
		     Controller.WishMove.Length > 0 )
		{
			Stamina = (Stamina - StaminaDecay * StaminaDecayBoost * Time.Delta).Clamp( 0, 1 );
			_staminaUsedTime = 0;
		}
		else if ( _staminaUsedTime > StaminaDelay )
		{
			Stamina = (Stamina + StaminaRegen * Time.Delta).Clamp( 0, 1 );
		}

		if ( Controller.IsValid() )
			Controller.EnableRunning = Stamina > 0;

		if ( Controller.IsValid() )
		{
			Controller.Camera.FovAxis = CameraComponent.Axis.Vertical;
			Controller.Camera.FieldOfView =
				Screen.CreateVerticalFieldOfView( Preferences.FieldOfView / Zoom, 9.0f / 16.0f );
			Controller.AimSensitivityScale = 1 / Zoom;
		}
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

	[ConCmd( "seekers.changeteam" )]
	public static void ChangeTeamCmd( string team )
	{
		Local.Owner.Team = ResourceLibrary.GetAll<Team>().FirstOrDefault( x => x.ResourceName == team );
		Local.Owner.Respawn( Local.Owner.Connection, Local.Owner.FindSpawnLocation() );
	}


	[ConCmd( "noclip", ConVarFlags.Cheat )]
	public static void Noclip()
	{
		Local.Controller.IsNoclipping = !Local.Controller.IsNoclipping;
	}
}
