namespace Seekers;

public struct ToolHint
{
	public string Input { get; set; }
	public string Description { get; set; }

	public ToolHint( string input, string description )
	{
		Input = input;
		Description = description;
	}

	public static ToolHint ForBind( string description, InputBind bind )
		=> new ToolHint( bind?.ToString() ?? "unbound", description );
}
