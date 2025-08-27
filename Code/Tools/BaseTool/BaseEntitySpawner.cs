namespace Seekers;

public abstract class BaseEntitySpawner<TEntity> : BaseTool where TEntity : Component
{
	protected PreviewModel PreviewModel;
	private RealTimeSince timeSinceDisabled;

	protected abstract string PreviewModelPath { get; }
	protected virtual Rotation PreviewRotationOffset => Rotation.Identity;
	protected virtual float PreviewNormalOffset => 0f;

	protected override void OnStart()
	{
		if ( IsProxy ) return;

		PreviewModel = new PreviewModel
		{
			ModelPath = PreviewModelPath,
			RotationOffset = PreviewRotationOffset,
			NormalOffset = PreviewNormalOffset,
			FaceNormal = true
		};
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();

		if ( IsProxy ) return;
		if ( timeSinceDisabled < Time.Delta * 5f || !Parent.IsValid() ) return;

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

	protected abstract void ApplyChanges( GameObject target );

	protected abstract void CreateEntity( SelectionPoint selectionPoint );

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) )
			return false;

		if ( trace.GameObject.Components.TryGet<TEntity>( out var entity ) && EntityOwnedByLocal( entity ) )
		{
			ApplyChanges( trace.GameObject );
			return true;
		}

		CreateEntity( new SelectionPoint( trace ) );
		return true;
	}

	protected bool EntityOwnedByLocal( Component comp )
	{
		var ownedEntity = comp as OwnedEntity;

		if ( !ownedEntity.IsValid() )
			return false;

		return ownedEntity.EntityOwner == Connection.Local.Id;
	}
}
