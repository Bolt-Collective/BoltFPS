using System.Diagnostics;

namespace Seekers;

[Library( "tool_button", Title = "Button", Description = "Add Button To Object" )]
[Group( "construction" )]
public partial class Button : BaseTool
{
	[Property]
	public InputBind Bind { get; set; } = new("uparrow", true);

	[Property]
	public bool Toggle { get; set; } = true;

	[Property, Range(-10000f, 10000f)]
	public float Force { get; set; } = 600f;

	PreviewModel PreviewModel;
	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		PreviewModel = new PreviewModel
		{
			ModelPath = "models/button/button.vmdl",
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

			CreateThruster( new SelectionPoint( trace ), Bind.GetBroadcast(), Toggle, Network.Owner.Id );

			return true;
		}

		return false;
	}

	[Rpc.Host]
	public static void CreateThruster(SelectionPoint selectionPoint, BroadcastBind bind, bool toggle, Guid owner)
	{
		var button = new GameObject();
		button.WorldPosition = selectionPoint.WorldPosition;
		button.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 90, 0, 0 ) );

		var prop = button.Components.Create<Prop>();
		prop.IsStatic = true;
		prop.Model = Model.Load( "models/button/button.vmdl" );

		button.Components.Create<PropHelper>();

		button.Tags.Add( "button" );

		var buttonEntity = button.AddComponent<ButtonEntity>();
		buttonEntity.InputBind = new InputBind( bind );
		buttonEntity.Toggle = toggle;
		buttonEntity.EntityOwner = owner;

		button.SetParent( selectionPoint.GameObject );
		NoCollide.ApplyNoCollide( button, selectionPoint.GameObject, Guid.NewGuid() );

		button.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Button", button );
		}, prop: button );
	}
}
