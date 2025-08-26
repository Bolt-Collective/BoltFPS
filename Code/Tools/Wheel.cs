using System.Diagnostics;

namespace Seekers;

[Library( "tool_wheel", Title = "Wheel", Description = "Attach a Wheel To Object" )]
[Group( "construction" )]
public partial class Wheel : BaseTool
{
	[Property]
	public InputBind Forwards { get; set; } = new( "uparrow",true );

	[Property]
	public InputBind Backwards { get; set; } = new( "downarrow",true );

	[Property, Range( -1000f, 1000f )]
	public float Torque { get; set; } = 60f;

	PreviewModel PreviewModel;
	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		PreviewModel = new PreviewModel
		{
			ModelPath = "models/citizen_props/wheel01.vmdl",
			NormalOffset = 8f,
			RotationOffset = Rotation.From( new Angles( 0, 90, 0 ) ),
			FaceNormal = true
		};
	}
	RealTimeSince timeSinceDisabled;


	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy )
			return;

		if ( timeSinceDisabled < Time.Delta * 5f || !Parent.IsValid() )
			return;

		var trace = Parent.BasicTraceTool();

		PreviewModel.Update( trace );
	}

	protected override void OnDestroy()
	{
		PreviewModel?.Destroy();
		base.OnDestroy();
	}

	public override void Disabled()
	{
		timeSinceDisabled = 0;
		PreviewModel?.Destroy();
	}

	public override bool Primary( SceneTraceResult trace )
	{
		if ( Input.Pressed( "attack1" ) )
		{
			if (trace.GameObject.Components.TryGet<WheelEntity>(out var wheel) && wheel.EntityOwner == Connection.Local.Id)
			{
				ChangeValues( trace.GameObject, Forwards.GetBroadcast(), Backwards.GetBroadcast(), Torque );
				return true;
			}

			CreateWheel( new SelectionPoint( trace ), Forwards.GetBroadcast(), Backwards.GetBroadcast(), Torque, Network.OwnerId );

			return true;
		}

		return false;
	}

	[Rpc.Broadcast]
	public void ChangeValues(GameObject wheel, BroadcastBind forwards, BroadcastBind backwards, float torque )
	{
		if ( wheel.IsProxy )
			return;

		var wheelEntity = wheel.GetComponent<WheelEntity>();

		if ( !wheelEntity.IsValid() )
			return;

		wheelEntity.Torque = torque;
		wheelEntity.ForwardBind = new InputBind( forwards );
		wheelEntity.BackwardBind = new InputBind( backwards );
	}

	[Rpc.Host]
	public static void CreateWheel( SelectionPoint selectionPoint, BroadcastBind forwards, BroadcastBind backwards, float torque, Guid owner )
	{
		var wheel = new GameObject();
		wheel.WorldPosition = selectionPoint.WorldPosition + selectionPoint.WorldNormal * 8;
		wheel.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 0, 90, 0 ) );

		var modelProp = wheel.Components.Create<Prop>();
		modelProp.Model = Model.Load( "models/citizen_props/wheel01.vmdl" );

		modelProp.AddComponent<PropHelper>();

		var rb = wheel.AddComponent<Rigidbody>();

		var wheelEntity = wheel.AddComponent<WheelEntity>();
		wheelEntity.Torque = torque;
		wheelEntity.ForwardBind = new InputBind( forwards );
		wheelEntity.BackwardBind = new InputBind( backwards );
		wheelEntity.EntityOwner = owner;

		wheel.NetworkSpawn();

		Hinge.HingeObjects( wheel, selectionPoint.GameObject, wheel.WorldTransform.PointToLocal( selectionPoint.WorldPosition ), selectionPoint.LocalPosition, selectionPoint.WorldNormal, new Angles(90,0,0) );

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Wheel", wheel );
		}, prop: wheel );
	}
}
