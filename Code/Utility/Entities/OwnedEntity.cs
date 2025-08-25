namespace Seekers;

public abstract class OwnedEntity : Component
{
	[Property]
	public Guid Owner { get; set; }

	protected override void OnUpdate()
	{
		if ( Connection.Local.Id != Owner )
			return;

		OwnerUpdate();
	}

	public virtual void OwnerUpdate()
	{

	}
}
