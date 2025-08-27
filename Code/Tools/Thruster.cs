using Sandbox.UI;
using System.Diagnostics;

namespace Seekers;

[Library( "tool_thruster", Title = "Thruster", Description = "Add Thruster To Object" )]
[Group( "construction" )]
public partial class Thruster : BaseEntitySpawner<ThrusterEntity>
{
	[Property]
	public InputBind Forwards { get; set; } = new( "uparrow", true );

	[Property]
	public InputBind Backwards { get; set; } = new( "downarrow", true );

	[Property, Range( -10000f, 10000f )]
	public float Force { get; set; } = 600f;

	protected override string PreviewModelPath => "models/thruster/thrusterprojector.vmdl";
	protected override Rotation PreviewRotationOffset => Rotation.From( new Angles( 90, 0, 0 ) );
	protected override float PreviewNormalOffset => 0f;

	protected override void ApplyChanges( GameObject target )
	{
		ChangeValues( target, Forwards.GetBroadcast(), Backwards.GetBroadcast(), Force );
	}

	protected override void CreateEntity( SelectionPoint sp )
	{
		CreateThruster( sp, Forwards.GetBroadcast(), Backwards.GetBroadcast(), Force, Network.OwnerId );
	}

	[Rpc.Broadcast]
	public void ChangeValues( GameObject thruster, BroadcastBind forwards, BroadcastBind backwards, float force )
	{
		if ( thruster.IsProxy )
			return;

		var thrusterEntity = thruster.GetComponent<ThrusterEntity>();
		if ( !thrusterEntity.IsValid() )
			return;

		thrusterEntity.Force = force;
		thrusterEntity.ForwardBind = new InputBind( forwards );
		thrusterEntity.BackwardBind = new InputBind( backwards );
	}

	[Rpc.Host]
	public static void CreateThruster(SelectionPoint selectionPoint, BroadcastBind forwards, BroadcastBind backwards, float force, Guid owner)
	{
		var thruster = new GameObject();
		thruster.WorldPosition = selectionPoint.WorldPosition;
		thruster.WorldRotation = Rotation.LookAt( selectionPoint.WorldNormal ) * Rotation.From( new Angles( 90, 0, 0 ) );

		var prop =  thruster.Components.Create<Prop>();
		prop.IsStatic = true;
		prop.Model = Model.Load( "models/thruster/thrusterprojector.vmdl" );

		thruster.Components.Create<PropHelper>().CanFreeze = false;

		thruster.Tags.Add( "thruster" );

		var thrusterEntity = thruster.AddComponent<ThrusterEntity>();
		thrusterEntity.Force = force;
		thrusterEntity.ForwardBind = new InputBind( forwards );
		thrusterEntity.BackwardBind = new InputBind( backwards );
		thrusterEntity.EntityOwner = owner;

		thruster.SetParent( selectionPoint.GameObject );
		NoCollide.ApplyNoCollide( thruster, selectionPoint.GameObject, Guid.NewGuid() );

		thruster.NetworkSpawn();

		UndoSystem.Add( creator: owner, callback: () =>
		{
			return UndoSystem.UndoObjects( "Undone Thruster", thruster );
		}, prop: thruster );
	}
}
