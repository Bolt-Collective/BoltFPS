using ShrimplePawns;

namespace Seekers;

[Pawn( "prefabs/spectator.prefab" )]
public sealed class SpectatorPawn : Pawn
{
	[Property] public float Speed { get; set; } = 300;
	[Property] public SpectateState State { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	public Client SpectatedClient { get; set; }

	public enum SpectateState
	{
		Free,
		Player
	}

	protected override void OnStart()
	{
		base.OnStart();

		Camera.Enabled = !IsProxy;
		if ( Client.Local.IsValid() )
			WorldTransform = Client.Local.WorldTransform;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;

		switch ( State )
		{
			case SpectateState.Free:
				FreeSpectate();
				break;
			case SpectateState.Player:
				PlayerSpectate();
				break;
		}

		if ( Input.Pressed( "attack1" ) )
		{
			SpectateNextPlayer();
		}

		Camera.FovAxis = CameraComponent.Axis.Vertical;
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Preferences.FieldOfView, 9.0f / 16.0f );
	}

	void SpectateNextPlayer()
	{
		var allClients = Game.ActiveScene.GetAllComponents<Client>()
			.Where( x => x.Connection != Connection.Local && x.Team != TeamManager.SpectatorsTeam )
			.ToList();

		if ( allClients.Count == 0 )
			return;

		int currentPlayerIndex = 0;

		if ( SpectatedClient.IsValid() )
		{
			var index = allClients.IndexOf( SpectatedClient );
			if ( index != -1 )
			{
				currentPlayerIndex = index;
			}
		}

		currentPlayerIndex++;

		if ( currentPlayerIndex >= allClients.Count )
		{
			currentPlayerIndex = 0;
		}

		SpectatedClient = allClients[currentPlayerIndex];
		State = SpectateState.Player;
	}

	void FreeSpectate()
	{
		var angles = WorldRotation.Angles();
		angles += Input.AnalogLook;

		WorldRotation = angles;

		var moveDirection = Vector3.Zero;
		moveDirection += Input.AnalogMove * Speed * Time.Delta;

		WorldPosition += WorldRotation * moveDirection;
	}

	void PlayerSpectate()
	{
		if ( !SpectatedClient.IsValid() || Input.Pressed( "jump" ) )
		{
			State = SpectateState.Free;
			return;
		}

		if ( !SpectatedClient.CameraObject.IsValid() )
			return;

		WorldPosition = SpectatedClient.CameraObject.WorldPosition;

		WorldRotation = SpectatedClient.CameraObject.WorldRotation;
	}
}
