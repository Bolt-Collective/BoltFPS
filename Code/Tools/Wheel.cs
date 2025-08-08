using System.Diagnostics;

namespace Seekers;

[Library( "tool_wheel", Title = "Wheel", Description = "Add Wheel To Object", Group = "construction" )]
public partial class Wheel : BaseTool
{
	[Property]
	public InputBind Forwards { get; set; } = new( "uparrow" );

	[Property]
	public InputBind Backwards { get; set; } = new( "downarrow" );

	[Property, Range( -10000f, 10000f )]
	public float Torque { get; set; } = 600f;

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

			CreateWheel( new SelectionPoint( trace ), Forwards, Backwards, Torque, Network.Owner.Id );

			return true;
		}

		return false;
	}

	[Rpc.Host]
	public static void CreateWheel( SelectionPoint selectionPoint, InputBind forwards, InputBind backwards, float torque, Guid owner )
	{
		var wheel = new GameObject();
		wheel.WorldPosition = selectionPoint.WorldPosition + selectionPoint.WorldNormal * 8;
		wheel.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 0, 90, 0 ) );

		var modelRenderer = wheel.Components.Create<ModelRenderer>();
		modelRenderer.Model = Model.Load( "models/citizen_props/wheel01.vmdl" );

		var modelCollider = wheel.Components.Create<ModelCollider>();
		modelCollider.Model = Model.Load( "models/citizen_props/wheel01.vmdl" );

		var rb = wheel.AddComponent<Rigidbody>();

		//var thrusterEntity = thruster.AddComponent<ThrusterEntity>();
		//thrusterEntity.Force = force;
		//thrusterEntity.ForwardBind = new InputBind( forwards );
		//thrusterEntity.BackwardBind = new InputBind( backwards );
		//thrusterEntity.Owner = owner;

		var wheelEntity = wheel.AddComponent<WheelEntity>();
		wheelEntity.Torque = torque;
		wheelEntity.ForwardBind = new InputBind( forwards );
		wheelEntity.BackwardBind = new InputBind( backwards );
		wheelEntity.Owner = owner;

		wheel.NetworkSpawn();

		Hinge.HingeObjects( wheel, selectionPoint.GameObject, wheel.WorldTransform.PointToLocal( selectionPoint.WorldPosition ), selectionPoint.LocalPosition, selectionPoint.WorldNormal, new Angles(90,0,0) );

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Wheel", wheel );
		}, prop: wheel );
	}
}
