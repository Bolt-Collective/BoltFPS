namespace Seekers;

[Icon( "people" )]
public partial class Client : ShrimplePawns.Client
{
	[Sync, Property] public GameObject CameraObject { get; set; }

	public static Client Local => Game.ActiveScene.GetAllComponents<Client>().FirstOrDefault( x => !x.IsProxy );


	[Sync( SyncFlags.FromHost ), Property] public long SteamId { get; set; }

	[Sync, Property] public int Kills { get; set; }
	[Sync, Property] public int Deaths { get; set; }

	[Sync, Property] public string CurrentVote { get; set; }

	public bool AbleToVote { get; set; } = true;


	[Property]
	[Sync( SyncFlags.FromHost )]
	[Group( "Setup" )]
	public Team Team
	{
		get => _team;
		set
		{
			lastTeam = value;

			_team = value;

			if ( Networking.IsHost && TryGetPawn( out Pawn pawn ) )
			{
				Respawn( Connection, FindSpawnLocation() );
			}
		}
	}

	[Sync( SyncFlags.FromHost )] public Team lastTeam { get; set; } = TeamManager.BasicTeam;

	private Team _team;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		//if ( Networking.IsHost )
			//DetectAFK();

		if ( IsProxy )
			return;

		if ( TryGetPawn( out Pawn pawn ) )
		{
			CameraObject =
				pawn?.Controller?.Camera.GameObject ?? pawn?.Components.Get<CameraComponent>( FindMode.EnabledInSelfAndChildren )?.GameObject ?? null;

			if ( CameraObject.IsValid() )
				WorldTransform = CameraObject.WorldTransform;
		}
	}

	/* Find Way To Move This to Seperate Script

	float stillTime = 0;
	public void DetectAFK()
	{
		if ( !TryGetPawn( out Pawn pawn ) )
			return;

		if ( pawn.Team != TeamManager.HuntersTeam )
			return;

		if ( GameManager.Instance?.GameState != RoundState.Active )
			return;

		var isStill = pawn.Controller?.Controller.WishVelocity.Length < 1;

		var previousStillTime = stillTime;

		if ( isStill )
			stillTime += Time.Delta;
		else
			stillTime = 0;

		if ( stillTime >= 40 && previousStillTime < 40)
			AFKWarning();

		if ( stillTime >= 50 && previousStillTime < 50 )
		{
			stillTime = 0;
			pawn.HealthComponent?.TakeDamage( pawn, 100000 );
		}
	}

	[Rpc.Owner]
	public void AFKWarning()
	{
		ToastNotification.Current?.AddToast("You are AFK, Move or get eliminated in 10s.");
	}
	*/

	[Rpc.Owner]
	public void GiveAchievement( string achievement )
	{
		Log.Info( $"Achievement unlocked for {Connection.DisplayName}, {achievement}" );
		Sandbox.Services.Achievements.Unlock( achievement );
	}

	[Rpc.Owner]
	public void SetStat( string ident, double value )
	{
		Sandbox.Services.Stats.SetValue( ident, value );
	}

	[Rpc.Owner]
	public void IncrementStat( string ident, double value )
	{
		Sandbox.Services.Stats.Increment( ident, value );
	}

	public void Respawn( Connection channel, Transform startTransform )
	{
		if ( !Team.IsValid() )
			return;

		// Cleanup existing pawn
		/*
		if ( Pawn?.GameObject?.IsValid() ?? false )
		{
			Pawn.DestroyGameObject();
		}
		*/

		// Create new pawn from team's prefab
		AssignConnection( channel );
		AssignPawn( Team.PawnPrefab );

		if ( Pawn is Pawn pawn )
		{
			GoToSpawn( pawn.GameObject, startTransform );
		}
	}

	[Rpc.Broadcast]
	public void GoToSpawn( GameObject pawn, Transform transform )
	{
		if ( pawn?.IsProxy ?? true )
			return;

		var pawnComp = pawn.GetComponent<Pawn>();
		if ( !pawnComp.IsValid() )
			return;
		
		if ( pawnComp.Controller.IsValid())
			pawnComp.Controller.Controller.Velocity = 0;

		pawn.WorldTransform = transform;
	}
}
