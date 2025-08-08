namespace Seekers;

[Library( "tool_balloon", Title = "Balloons", Description = "Create Balloons!", Group = "construction" )]
public class BalloonTool : BaseTool
{
	PreviewModel PreviewModel;
	RealTimeSince timeSinceDisabled;

	protected override void OnStart()
	{
		if ( IsProxy )
			return;

		PreviewModel = new PreviewModel
		{
			ModelPath = "models/citizen_props/balloonregular01.vmdl_c",
			RotationOffset = Rotation.From( new Angles( 0, 0, 0 ) ),
			FaceNormal = false
		};
	}

	protected override void OnUpdate()
	{
		if ( IsProxy )
			return;

		if ( timeSinceDisabled < Time.Delta * 5f || !Parent.IsValid() )
			return;

		var trace = Parent.BasicTraceTool();

		PreviewModel.Update( trace );
	}

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !trace.Hit || !trace.GameObject.IsValid() )
			return false;

		if ( Input.Pressed( "attack1" ) )
		{
			if ( trace.Tags.Contains( "balloon" ) || trace.Tags.Contains( "player" ) )
				return true;

			var balloon = SpawnBalloon( trace );

			balloon.Components.Create<BalloonGravity>();

			PropHelper propHelper = balloon.Components.Get<PropHelper>();

			if ( !propHelper.IsValid() )
				return true;

			var connectionPoint = new GameObject();
			connectionPoint.SetParent( balloon );
			connectionPoint.LocalPosition = 0;

			var connectionPoint2 = new GameObject();
			connectionPoint2.SetParent( trace.GameObject );
			connectionPoint2.WorldPosition = trace.HitPosition;

			connectionPoint.AddComponent<JointPoint>().otherPoint = connectionPoint2;
			connectionPoint2.AddComponent<JointPoint>().otherPoint = connectionPoint;

			var rope = Rope.CreateRopeBetween( connectionPoint, connectionPoint2, 100 );

			UndoSystem.Add( creator: Network.Owner.Id, callback: () =>
			{
				return UndoSystem.UndoObjects( "Undone Balloon", rope?.Attachment ?? null, rope.GameObject, balloon );
			}, prop: balloon );

			return true;
		}

		return false;
	}

	void PositionBalloon( GameObject balloon, SceneTraceResult trace )
	{
		balloon.WorldPosition = trace.HitPosition;
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

	GameObject SpawnBalloon( SceneTraceResult trace )
	{
		var go = new GameObject()
		{
			Tags = { "solid", "balloon" }
		};

		PositionBalloon( go, trace );

		var prop = go.AddComponent<Prop>();
		prop.Model = Model.Load( "models/citizen_props/balloonregular01.vmdl_c" );

		var propHelper = go.AddComponent<PropHelper>();

		if ( prop.Components.TryGet<SkinnedModelRenderer>( out var renderer ) )
		{
			renderer.CreateBoneObjects = true;
		}

		var rb = propHelper.Rigidbody;
		if ( rb.IsValid() )
		{
			foreach ( var shape in rb.PhysicsBody.Shapes )
			{
				if ( !shape.IsMeshShape )
					continue;

				var newCollider = go.AddComponent<BoxCollider>();
				newCollider.Scale = prop.Model.PhysicsBounds.Size;
			}
		}

		go.Network.AssignOwnership(Connection.Host);
		go.NetworkSpawn();
		go.Network.SetOrphanedMode( NetworkOrphaned.Host );

		return go;
	}
}
