namespace Seekers;

[Library( "tool_physicalproperties", Title = "Physical Properties",
	Description = "Change the physical properties of an object" )]
[Group( "Construction" )]
public class PhysicalProperties : BaseTool
{
	[Property, Dropdown( DropdownAttribute.NameTypes.File )]
	public List<string> Surfaces => GetSurfaces();


	[Property, Sync] public bool EnableGravity { get; set; } = true;

	public override bool UseGrid => false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) )
			return false;

		if ( !trace.Hit )
			return false;

		BroadcastAttack( trace.GameObject, GetCurrentSurface() );

		return true;
	}

	[Rpc.Broadcast]
	private void BroadcastAttack( GameObject gameObject, Surface surface )
	{
		if ( !gameObject.Root.Components.TryGet( out PropHelper propHelper ) )
			return;

		if ( !propHelper.IsValid() )
			return;

		if ( !propHelper.Rigidbody.IsValid() )
			return;

		if ( !propHelper.GetComponent<Collider>().IsValid() )
			return;

		Log.Info( GetCurrentSurface() );
		propHelper.GetComponent<Collider>().Surface = surface;
		propHelper.Rigidbody.Gravity = EnableGravity;
	}

	Surface GetCurrentSurface()
	{
		var current = DropdownAttribute.GetValue( this, nameof(Surfaces) );
		Surface item = ResourceLibrary.Get<Surface>( current );
		return item;
	}

	List<string> GetSurfaces()
	{
		var surfaces = new List<string>();

		foreach ( var surface in ResourceLibrary.GetAll<Surface>() )
		{
			surfaces.Add( surface.ResourcePath );
		}

		surfaces.Sort();

		return surfaces;
	}
}
