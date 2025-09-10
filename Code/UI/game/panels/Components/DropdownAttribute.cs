using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Seekers.NPC;

namespace Seekers;

/// <summary>
/// Makes a List string property into a dropdown for the tool/utility tab.
/// If the property is static, the class must be the same name as its file.
/// And yes, it sucks, whatever. If Unity can do that shit, so can I.
/// </summary>
public class DropdownAttribute : Attribute
{
	public int Index = 0;

	public NameTypes NameType = NameTypes.None;

	/// <summary>
	/// Decides how to display the Dropdown's names.
	/// <para>File - displays the filename from a path (without extension).</para> 
	/// <para>NPCWeapon - displays the file as an NPCWeapon, showing category and name.</para> 
	/// I wanted you to be able to assign a custom function, but I can't, so fuck you.
	/// </summary>
	public enum NameTypes
	{
		None,
		File,
		NPCWeapon,
		NPC
	}

	public DropdownAttribute() { }

	public DropdownAttribute( NameTypes nameType )
	{
		NameType = nameType;
	}

	public string GetName( string value )
	{
		switch (NameType)
		{
			case NameTypes.None:
				return value;
			case NameTypes.File:
				return FileName( value );
			case NameTypes.NPCWeapon:
				return NPCWeaponName( value );
			case NameTypes.NPC:
				return NPCName( value );

		}

		return value;
	}

	public string FileName(string path)
	{
		return Path.GetFileNameWithoutExtension(path);
	}

	public static string NPCWeaponName( string path )
	{
		NPCToolResource item = ResourceLibrary.Get<NPCToolResource>( path );

		if ( !item.IsValid )
			return path;

		var category = "Other";
		if (item.Category != null && item.Category != "")
			category = item.Category;

		return $"{item.Name} ({category})";
	}

	public static string NPCName( string path )
	{

		if ( !GameObject.GetPrefab( path ).Components.TryGet<NPC>( out var npc ) )
			return Path.GetFileNameWithoutExtension( path );

		string category = "Other";

		if (npc.Catagory != null && npc.Catagory != "")
			category = npc.Catagory;

		return $"{npc.Name} ({category})";
	}


	public string GetCurrentName( List<string> items )
	{
		if ( items == null || items.Count == 0 )
			return null;

		if ( Index < 0 || Index >= items.Count )
			return GetName( items.FirstOrDefault() );
		return GetName( items[Index] );
	}

	public List<string> GetAllNames( List<string> items )
	{
		if ( items == null || items.Count == 0 )
			return new List<string>();

		var allNames = new List<string>();
		for ( int i = 0; i < items.Count; i++ )
		{
			allNames.Add( GetName( items[i] ) );
		}

		return allNames;
	}

	public static string GetValue<T>( string property )
	{
		var propDesc = PropertyExtensions.GetProp<T>(property);
		return GetValue( propDesc, (object)null );
	}

	public static string GetValue(object obj, string property )
	{
		var propDesc = PropertyExtensions.GetProp( obj, property );
		return GetValue( propDesc, obj );
	}

	public static void SetValue<T>( string property, int value )
	{
		var propDesc = PropertyExtensions.GetProp<T>( property );
		SetValue( propDesc, (object)null, value );
	}

	public static void SetValue( Type type, string property, int value )
	{
		var propDesc = PropertyExtensions.GetProp( type, property );
		SetValue( propDesc, (object)null, value );
	}

	public static void SetValue( object obj, string property, int value )
	{
		var propDesc = PropertyExtensions.GetProp( obj, property );
		SetValue( propDesc, obj, value );
	}

	public static string GetValue(PropertyDescription property, object obj)
	{
		var rawValue = property.GetValue( obj );

		if ( rawValue is not List<string> list )
			return null;

		DropdownAttribute dropdown = null;
		foreach(var attribute in property.Attributes)
		{
			if ( attribute is not DropdownAttribute dd )
				continue;

			dropdown = dd;
			break;
		}
		

		if( dropdown == null )
			return list.FirstOrDefault();

		if ( list.Count <= dropdown.Index )
			return list.FirstOrDefault();
		
		return list[dropdown.Index];
	}

	public static void SetValue( PropertyDescription property, object obj, int index )
	{
		var rawValue = property.GetValue( obj );

		if ( rawValue is not List<string> list )
			return;

		DropdownAttribute dropdown = null;
		foreach ( var attribute in property.Attributes )
		{
			if ( attribute is DropdownAttribute dd )
			{
				dropdown = dd;
				break;
			}
		}

		if ( dropdown == null )
			return;

		if ( index < 0 || index >= list.Count )
			index = 0;

		dropdown.Index = index;
	}


}

