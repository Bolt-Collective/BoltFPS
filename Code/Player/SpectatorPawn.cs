using ShrimplePawns;

namespace Seekers;

[Pawn( "prefabs/spectator.prefab" )]
public sealed class SpectatorPawn : Pawn
{
	[Property] public float Speed { get; set; } = 300;
	[Property] public SpectateState State { get; set; }
	[Property] public CameraComponent Camera { get; set; }

	public Pawn SpectatedPawn { get; set; }

	public bool Orbiting = false;

	private Angles orbitAngles = Angles.Zero;
	private float orbitDistance = 150.0f;

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
				if ( Input.Pressed( "reload" ) )
					Orbiting = !Orbiting;
				else
					PlayerSpectate();
				break;
		}

		if ( Input.Pressed( "attack1" ) )
		{
			SpectateNextPlayer();
		}
		else if ( Input.Pressed( "attack2" ) )
		{
			SpectateNextPlayer( true );
		}

		if ( Orbiting )
		{
			OrbitSpectate();
		}

		Camera.FovAxis = CameraComponent.Axis.Vertical;
		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( Preferences.FieldOfView, 9.0f / 16.0f );
	}

	void SpectateNextPlayer( bool reverse = false )
	{
		var allClients = Game.ActiveScene.GetAllComponents<Pawn>()
			.Where( x => x.Network.Owner != Connection.Local && x.Team != TeamManager.SpectatorsTeam && !x.IsDead )
			.ToList();

		if ( allClients.Count == 0 )
			return;

		int currentPlayerIndex = 0;

		if ( SpectatedPawn.IsValid() )
		{
			var index = allClients.IndexOf( SpectatedPawn );
			if ( index != -1 )
			{
				currentPlayerIndex = index;
			}
		}

		if ( reverse )
		{
			currentPlayerIndex--;
			if ( currentPlayerIndex < 0 )
			{
				currentPlayerIndex = allClients.Count - 1;
			}
		}
		else
		{
			currentPlayerIndex++;
			if ( currentPlayerIndex >= allClients.Count )
			{
				currentPlayerIndex = 0;
			}
		}

		SpectatedPawn = allClients[currentPlayerIndex];
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
		if ( !SpectatedPawn.IsValid() )
		{
			State = SpectateState.Free;
			return;
		}

		if ( Input.Pressed( "jump" ) )
		{
			State = SpectateState.Free;
			return;
		}

		if ( !SpectatedPawn.Controller.IsValid() )
			return;

		if ( !SpectatedPawn.Controller.Camera.IsValid() )
			return;

		WorldPosition = SpectatedPawn.Controller.Camera.WorldPosition;
		WorldRotation = SpectatedPawn.Controller.Camera.WorldRotation;
	}

	void OrbitSpectate()
	{
		if ( !SpectatedPawn.IsValid() || !SpectatedPawn.Controller.Camera.IsValid() )
		{
			State = SpectateState.Free;
			return;
		}

		var focusPoint = SpectatedPawn.Controller.Head.WorldPosition;


		orbitAngles.pitch += -Input.AnalogLook.pitch;
		orbitAngles.yaw += Input.AnalogLook.yaw;
		orbitAngles.roll = 0;

		orbitAngles.pitch = orbitAngles.pitch.Clamp( -89, 89 );

		// Adjust orbit distance with scroll
		orbitDistance += Input.MouseWheel.y * -20f;
		orbitDistance = orbitDistance.Clamp( 50, 500 );

		// Convert orbit angles to direction vector and calculate position
		var orbitRotation = Rotation.From( orbitAngles );
		var orbitOffset = orbitRotation.Forward * -orbitDistance;

		WorldPosition = focusPoint + orbitOffset;
		WorldRotation = (focusPoint - WorldPosition).Normal.EulerAngles;
	}
}
