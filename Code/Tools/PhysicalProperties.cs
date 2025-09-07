namespace Seekers;

[Library( "tool_physicalproperties", Title = "Physical Properties",
	Description = "Change the physical properties of an object" )]
[Group( "Construction" )]
public class PhysicalProperties : BaseTool
{
	[Property, Dropdown( DropdownAttribute.NameTypes.File )]
	public List<string> Surfaces => GetSurfaces();


	[Property] public bool EnableGravity { get; set; } = true;

	public override bool UseGrid => false;

	public override bool Primary( SceneTraceResult trace )
	{
		if ( !Input.Pressed( "attack1" ) )
			return false;

		if ( !trace.Hit )
			return false;

		if ( !trace.GameObject.Components.TryGet( out PropHelper propHelper ) )
			return false;

		if ( !propHelper.Rigidbody.IsValid() )
			return false;

		if ( !propHelper.GetComponent<Collider>().IsValid() )
			return false;

		Log.Info( GetCurrentSurface() );
		propHelper.GetComponent<Collider>().Surface = GetCurrentSurface();
		propHelper.Rigidbody.Gravity = EnableGravity;

		return true;
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
