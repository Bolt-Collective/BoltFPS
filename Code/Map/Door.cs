using System;
using Seekers;

public sealed class Door : Component, Component.IPressable
{
	/// <summary>
	/// Animation curve to use, X is the time between 0-1 and Y is how much the door is open to its target angle from 0-1.
	/// </summary>
	[Property, Category( "Transform" )]
	public Curve AnimationCurve { get; set; } = new Curve( new Curve.Frame( 0f, 0f ), new Curve.Frame( 1f, 1.0f ) );

	/// <summary>
	/// Optional pivot point, origin will be used if not specified.
	/// </summary>
	[Property, Category( "Transform" ), Order( 99 )]
	public GameObject Pivot { get; set; }

	/// <summary>
	/// The axis the door rotates/moves on.
	/// </summary>
	[Property, Category( "Transform" ), Order( 99 )]
	public Angles Axis { get; set; } = new Angles( 90.0f, 0, 0 );

	/// <summary>
	/// How far should the door rotate.
	/// </summary>
	[Property, Category( "Transform" ), Order( 99 )]
	public float Distance { get; set; } = 90.0f;

	/// <summary>
	/// Sound to play when a door is opened.
	/// </summary>
	[Property, Group( "Sound" )]
	public SoundEvent OpenSound { get; set; }

	/// <summary>
	/// Sound to play when a door is fully opened.
	/// </summary>
	[Property, Group( "Sound" )]
	public SoundEvent OpenFinishedSound { get; set; }

	/// <summary>
	/// Sound to play when a door is closed.
	/// </summary>
	[Property, Group( "Sound" )]
	public SoundEvent CloseSound { get; set; }

	/// <summary>
	/// Sound to play when a door has finished closing.
	/// </summary>
	[Property, Group( "Sound" )]
	public SoundEvent CloseFinishedSound { get; set; }

	/// <summary>
	/// Sound to play when door is locked
	/// </summary>
	[Property, Group( "Sound" )]
	public SoundEvent DoorLockedSound { get; set; }


	/// <summary>
	/// How long in seconds should it take to open this door.
	/// </summary>
	[Property]
	public float OpenTime { get; set; } = 0.5f;

	/// <summary>
	/// Open away from the person who uses this door.
	/// </summary>
	[Property]
	public bool OpenAwayFromPlayer { get; set; } = true;

	/// <summary>
	/// Can we open the door at all?
	/// </summary>
	[Property]
	public bool Locked { get; set; } = false;

	[Property] public int KeyID { get; set; } = 0;

	[Property, ReadOnly] public Collider Collider { get; set; }

	public enum DoorMoveType
	{
		Moving,
		Rotating,
		AnimatingOnly
	}

	[Property] public DoorMoveType MoveDirType { get; set; } = DoorMoveType.Rotating;

	public enum DoorState
	{
		Open,
		Opening,
		Closing,
		Closed
	}

	Transform StartTransform { get; set; }
	public Vector3 PivotPosition { get; set; }
	bool ReverseDirection { get; set; }
	[Sync( SyncFlags.FromHost )] public TimeSince LastUse { get; set; }
	[Sync( SyncFlags.FromHost )] public DoorState State { get; set; } = DoorState.Closed;

	public List<NPC> OpeningNPCS = new();

	private DoorState DefaultState { get; set; } = DoorState.Closed;

	private string _hintText = "open";

	public string HintText
	{
		get => _hintText;
		set => _hintText = value;
	}

	protected override void OnStart()
	{
		Collider = GetComponent<Collider>();
		StartTransform = Transform.Local;
		if ( PivotPosition == Vector3.Zero )
			PivotPosition = Pivot is not null ? Pivot.WorldPosition : StartTransform.Position;
		DefaultState = State;
	}

	public void Open( Vector3 from )
	{
		ReverseDirection = Vector3.GetAngle( Pivot.WorldTransform.Forward, from - Pivot.WorldPosition ) > 90;
		LastUse = 0.0f;
		State = DoorState.Opening;
		if ( OpenSound is not null )
			SoundExtensions.BroadcastSound( OpenSound.ResourcePath, WorldPosition );
	}

	public void Close()
	{
		LastUse = 0.0f;
		State = DoorState.Closing;
		if ( CloseSound is not null )
			SoundExtensions.BroadcastSound( CloseSound.ResourcePath, WorldPosition );
	}

	protected override void OnFixedUpdate()
	{
		if ( State != DoorState.Opening && State != DoorState.Closing )
			return;

		var time = LastUse.Relative.Remap( 0.0f, OpenTime, 0.0f, 1.0f );

		var curve = AnimationCurve.Evaluate( time );

		if ( State == DoorState.Closing ) curve = 1.0f - curve;

		if ( MoveDirType == DoorMoveType.Rotating )
		{
			var targetAngle = Distance;
			if ( ReverseDirection ) targetAngle *= -1.0f;

			var axis = Rotation.From( Axis ).Up;

			Transform.Local =
				StartTransform.RotateAround( PivotPosition, Rotation.FromAxis( axis, targetAngle * curve ) );
		}

		if ( MoveDirType == DoorMoveType.Moving )
		{
			var dir = Axis.Forward;
			var boundSize = Collider.GetWorldBounds().Size;
			var fulldirection = dir * (MathF.Abs( boundSize.Dot( dir ) ) - Distance);

			Transform.Local = StartTransform.WithPosition( StartTransform.Position + (fulldirection * curve) );
		}

		// If we're done finalize the state and play the sound
		if ( time < 1f ) return;

		State = State == DoorState.Opening ? DoorState.Open : DoorState.Closed;

		if ( Networking.IsHost )
		{
			if ( State == DoorState.Open && OpenFinishedSound is not null )
				SoundExtensions.BroadcastSound( OpenFinishedSound.ResourcePath, WorldPosition );

			if ( State == DoorState.Closed && CloseFinishedSound is not null )
				SoundExtensions.BroadcastSound( CloseFinishedSound.ResourcePath, WorldPosition );
		}
	}

	public bool TryUnlock( Pawn pawn )
	{
		if ( !Locked || !pawn.IsValid() )
			return false;

		Locked = false;
		return true;
	}


	public bool Press( IPressable.Event e )
	{
		Press( e.Source.GameObject );

		return true;
	}

	[Rpc.Host]
	public void Press( GameObject source )
	{
		LastUse = 0;

		var pawn = source.GetComponent<Pawn>();

		if ( Locked && !TryUnlock( pawn ) )
		{
			SoundExtensions.BroadcastSound( DoorLockedSound?.ResourcePath ?? "", WorldPosition );
			return;
		}

		if ( State == DoorState.Closed )
		{
			Open( pawn.WorldPosition );
		}
		else if ( State == DoorState.Open )
		{
			Close();
		}

		return;
	}

	public bool CanPress( IPressable.Event e )
	{
		// Don't use doors already opening/closing
		return State is DoorState.Open or DoorState.Closed;
	}
}
