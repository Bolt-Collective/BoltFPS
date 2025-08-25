using System.Diagnostics;

namespace Seekers;

[Library( "tool_thruster", Title = "Thruster", Description = "Add Thruster To Object" )]
[Group( "construction" )]
public partial class Thruster : BaseTool
{
	[Property]
	public InputBind Forwards { get; set; } = new("uparrow", true);

	[Property]
	public InputBind Backwards { get; set; } = new("downarrow", true);

	[Property, Range(-10000f, 10000f)]
	public float Force { get; set; } = 600f;

	PreviewModel PreviewModel;
	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		PreviewModel = new PreviewModel
		{
			ModelPath = "models/thruster/thrusterprojector.vmdl",
			RotationOffset = Rotation.From( new Angles( 90, 0, 0 ) ),
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

			CreateThruster( new SelectionPoint( trace ), Forwards, Backwards, Force, Network.Owner.Id );

			return true;
		}

		return false;
	}

	[Rpc.Host]
	public static void CreateThruster(SelectionPoint selectionPoint, InputBind forwards, InputBind backwards, float force, Guid owner)
	{
		var thruster = new GameObject();
		thruster.WorldPosition = selectionPoint.WorldPosition;
		thruster.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 90, 0, 0 ) );

		var modelRenderer = thruster.Components.Create<ModelRenderer>();
		modelRenderer.Model = Model.Load( "models/thruster/thrusterprojector.vmdl" );

		thruster.Tags.Add( "thruster" );

		var thrusterEntity = thruster.AddComponent<ThrusterEntity>();
		thrusterEntity.Force = force;
		thrusterEntity.ForwardBind = new InputBind( forwards );
		thrusterEntity.BackwardBind = new InputBind( backwards );
		thrusterEntity.Owner = owner;

		thruster.SetParent( selectionPoint.GameObject );

		thruster.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Thruster", thruster );
		}, prop: thruster );
	}
}
