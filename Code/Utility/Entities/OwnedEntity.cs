namespace Seekers;

public abstract class OwnedEntity : Component
{
	[Property, Sync]
	public Guid EntityOwner { get; set; }

	protected override void OnUpdate()
	{
		if ( Connection.Local.Id != EntityOwner )
			return;

		OwnerUpdate();
	}

	public virtual void OwnerUpdate()
	{

	}
}
