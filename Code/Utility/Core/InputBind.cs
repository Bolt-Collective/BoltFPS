using System.Text.Json.Serialization;

namespace Seekers;

public class InputBind
{
	[KeyProperty] public string InputKey { get; set; }
	public string InputName()
	{
		if ( Action )
			return Input.GetButtonOrigin( InputKey );
		return InputKey;
	}
	[KeyProperty] public bool Action { get; set; }

	[JsonConstructor]
	public InputBind( string InputKey, bool Action )
	{
		this.InputKey = InputKey;
		this.Action = Action;
	}

	public InputBind( InputBind inputBind )
	{
		InputKey = inputBind.InputKey;
		Action = inputBind.Action;
	}

	public bool Pressed()
	{
		return Action ? Input.Pressed( InputKey ) : Input.Keyboard.Pressed( InputKey );
	}

	public bool Down()
	{
		return Action ? Input.Down( InputKey ) : Input.Keyboard.Down( InputKey );
	}
	public bool Released()
	{
		return Action ? Input.Released( InputKey ) : Input.Keyboard.Released( InputKey );
	}

	public bool Listening;
	public bool Listen()
	{
		foreach ( var action in Input.GetActions() )
		{
			if ( !Input.Down( action.Name ) )
				continue;

			InputKey = action.Name;
			Action = true;

			return true;
		}

		foreach ( var key in GetAllKeys() )
		{
			if ( !Input.Keyboard.Down( key ) )
				continue;

			InputKey = key;
			Action = false;

			return true;
		}

		return false;
	}

	public static List<string> GetAllKeys()
	{
		return new List<string>
		{
			"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",

			"a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m",
			"n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",

			"KP_0", "KP_1", "KP_2", "KP_3", "KP_4", "KP_5", "KP_6", "KP_7", "KP_8", "KP_9",

			"KP_DIVIDE", "KP_MULTIPLY", "KP_MINUS", "KP_PLUS", "KP_ENTER", "KP_DEL",

			"<", ">", "[", "]", "SEMICOLON", "'", "`", ",", ".", "/", "\\", "-", "=",

			"ENTER", "SPACE", "BACKSPACE", "TAB", "CAPSLOCK", "NUMLOCK", "ESCAPE", "SCROLLLOCK",
			"INS", "DEL", "HOME", "END", "PGUP", "PGDN", "PAUSE",

			"SHIFT", "RSHIFT", "ALT", "RALT",

			"UPARROW", "LEFTARROW", "RIGHTARROW", "DOWNARROW"
		};
	}
}
