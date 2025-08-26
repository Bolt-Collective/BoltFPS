using Sandbox.UI;
using System.Diagnostics;

namespace Seekers;

[Library( "tool_hoverball", Title = "Hoverball", Description = "Add Hoverball To Object" )]
[Group( "construction" )]
public partial class Hoverball : BaseTool
{
	[Property]
	public InputBind Up { get; set; } = new("uparrow", true);

	[Property]
	public InputBind Down { get; set; } = new("downarrow", true);

	[Property, Range(0f, 1f)]
	public float Strength { get; set; } = 0.5f;

	[Property, Range( 0f, 1000f )]
	public float Speed { get; set; } = 250f;

	PreviewModel PreviewModel;
	protected override void OnStart()
	{
		if ( IsProxy )
			return; 

		PreviewModel = new PreviewModel
		{
			ModelPath = "models/hoverball/hoverball.vmdl",
			RotationOffset = Rotation.From( new Angles( 0, 0, 0 ) ),
			NormalOffset = 9,
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

			CreateHoverball( new SelectionPoint( trace ), Up.GetBroadcast(), Down.GetBroadcast(), Strength, Speed, Network.Owner.Id );

			return true;
		}

		return false;
	}

	[Rpc.Host]
	public static void CreateHoverball(SelectionPoint selectionPoint, BroadcastBind up, BroadcastBind down, float force, float speed, Guid owner)
	{
		var hoverball = new GameObject();
		hoverball.WorldPosition = selectionPoint.WorldPosition + selectionPoint.WorldNormal * 9;
		hoverball.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal );

		var modelProp = hoverball.Components.Create<Prop>();
		modelProp.IsStatic = true;
		modelProp.Model = Model.Load( "models/hoverball/hoverball.vmdl" );

		modelProp.AddComponent<PropHelper>();

		var hoverballEntity = hoverball.AddComponent<HoverballEntity>();
		hoverballEntity.Strength = force;
		hoverballEntity.Speed = speed;
		hoverballEntity.UpBind = new InputBind( up );
		hoverballEntity.DownBind = new InputBind( down );
		hoverballEntity.EntityOwner = owner;
		hoverballEntity.TargetRotation = selectionPoint.GameObject.WorldRotation;

		hoverball.SetParent( selectionPoint.GameObject );
		NoCollide.ApplyNoCollide( hoverball, selectionPoint.GameObject, Guid.NewGuid() );

		hoverball.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Hoverball", hoverball );
		}, prop: hoverball );
	}
}
