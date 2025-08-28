using Sandbox;
using Sandbox.UI;
using System.Diagnostics;

namespace Seekers;

[Library( "tool_hoverball", Title = "Hoverball", Description = "Attaches a hoverball to an object" )]
[Group( "construction" )]
public partial class Hoverball : BaseEntitySpawner<HoverballEntity>
{
	[Property]
	public InputBind Up { get; set; } = new( "uparrow", true );

	[Property]
	public InputBind Down { get; set; } = new( "downarrow", true );

	[Property, Range( 0f, 1f )]
	public float Strength { get; set; } = 0.5f;

	[Property, Range( 0f, 1000f )]
	public float Speed { get; set; } = 250f;

	protected override string PreviewModelPath => "models/hoverball/hoverball.vmdl";
	protected override Rotation PreviewRotationOffset => Rotation.Identity;
	protected override float PreviewNormalOffset => 9f;

	protected override void ApplyChanges( GameObject target )
	{
		ChangeValues( target, Up.GetBroadcast(), Down.GetBroadcast(), Strength, Speed );
	}

	protected override void CreateEntity( SelectionPoint sp )
	{
		CreateHoverball( sp, Up.GetBroadcast(), Down.GetBroadcast(), Strength, Speed, Network.OwnerId );
	}

	[Rpc.Broadcast]
	public void ChangeValues( GameObject hoverball, BroadcastBind up, BroadcastBind down, float strength, float speed )
	{
		if ( hoverball.IsProxy )
			return;

		var hoverballEntity = hoverball.GetComponent<HoverballEntity>();

		if ( !hoverballEntity.IsValid() )
			return;

		hoverballEntity.Strength = strength;
		hoverballEntity.Speed = speed;
		hoverballEntity.UpBind = new InputBind( up );
		hoverballEntity.DownBind = new InputBind( down );
	}

	[Rpc.Host]
	public static void CreateHoverball(SelectionPoint selectionPoint, BroadcastBind up, BroadcastBind down, float force, float speed, Guid owner)
	{
		var hoverball = new GameObject();
		hoverball.WorldPosition = selectionPoint.WorldPosition + selectionPoint.WorldNormal * 9;
		hoverball.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal );

		var modelProp = hoverball.Components.Create<Prop>();
		modelProp.Model = Model.Load( "models/hoverball/hoverball.vmdl" );

		modelProp.Components.Create<PropHelper>().CanFreeze = false;

		var hoverballEntity = hoverball.AddComponent<HoverballEntity>();
		hoverballEntity.Strength = force;
		hoverballEntity.Speed = speed;
		hoverballEntity.UpBind = new InputBind( up );
		hoverballEntity.DownBind = new InputBind( down );
		hoverballEntity.EntityOwner = owner;
		hoverballEntity.ConnectedObject = selectionPoint.GameObject;
		hoverballEntity.LocalPos = selectionPoint.GameObject.WorldTransform.PointToLocal(hoverball.WorldPosition);
		hoverballEntity.HeightOffset = hoverball.WorldPosition.z - selectionPoint.GameObject.WorldPosition.z;

		Weld.WeldObjects( hoverball, selectionPoint.GameObject, hoverball.WorldTransform.PointToLocal( selectionPoint.WorldPosition ) );
		NoCollide.ApplyNoCollide( hoverball, selectionPoint.GameObject, Guid.NewGuid() );

		hoverball.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Hoverball", hoverball );
		}, prop: hoverball );
	}
}
