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
			if ( _team != null )
				lastTeam = _team;
			else
				lastTeam = value;

			_team = value;

			Respawning = false;

			if ( Networking.IsHost && TryGetPawn( out Pawn pawn ) && lastTeam != _team )
			{
				Respawn( Connection );
			}
		}
	}

	public bool Respawning;
	public Team TargetTeam;
	public Transform TargetSpawn;
	public RealTimeSince RespawnTimer;

	public void SetTeamRespawnTimer( Team team, Transform spawnPoint )
	{
		_team = TeamManager.GetTeam( "spectators" );
		Respawn( Connection );
		Respawning = true;
		TargetTeam = team;
		RespawnTimer = 0;
		TargetSpawn = spawnPoint;
	}

	[Sync( SyncFlags.FromHost )] public Team lastTeam { get; set; } = null;

	private Team _team = null;

	protected override void OnFixedUpdate()
	{
		base.OnFixedUpdate();

		if ( Networking.IsHost && Respawning && RespawnTimer > TargetTeam.RespawnTime )
		{
			_team = TargetTeam;
			Respawning = false;
			Respawn( Connection );
		}

		if ( IsProxy )
			return;

		if ( TryGetPawn( out Pawn pawn ) )
		{
			CameraObject =
				pawn?.Controller?.Camera.GameObject ??
				pawn?.Components.Get<CameraComponent>( FindMode.EnabledInSelfAndChildren )?.GameObject ?? null;

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

	public void Respawn( Connection channel )
	{
		if ( !Team.IsValid() )
			return;

		Respawning = false;

		// Create new pawn from team's prefab
		AssignConnection( channel );
		AssignPawn( Team.PawnPrefab );

		if ( Pawn is Pawn pawn )
		{
			GoToSpawn( pawn );
		}
	}

	[Rpc.Broadcast]
	public void GoToSpawn( Pawn pawn )
	{
		var spawnPoints = Scene.GetAllComponents<SpawnPoint>().ToArray();

		if ( pawn?.IsProxy ?? true )
			return;

		var randomSpawnPoint = Random.Shared.FromArray( spawnPoints );
		if ( randomSpawnPoint is null ) return;

		pawn.WorldPosition =
			randomSpawnPoint.WorldPosition + Vector3.Up * 10; // Offset to avoid clipping into the ground
		pawn.Controller.EyeAngles = randomSpawnPoint.WorldRotation.Angles();
	}
}
