namespace Seekers;

public static class PropertyExtensions
{
	public static PropertyDescription GetProp( this object obj, string property )
	{
		if ( obj == null ) return null;

		var typeDesc = TypeLibrary.GetType( obj.GetType() );
		return typeDesc?.Properties.FirstOrDefault( p => p.Name == property );
	}

	public static PropertyDescription GetProp<T>( string property )
	{
		return TypeLibrary.GetType<T>()
			.Properties
			.FirstOrDefault( p => p.Name == property );
	}

	public static PropertyDescription GetProp( Type type, string property )
	{
		return TypeLibrary.GetType(type)
			.Properties
			.FirstOrDefault( p => p.Name == property );
	}
}
