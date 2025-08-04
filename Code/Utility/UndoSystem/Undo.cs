using System;
using Sandbox;
using Seekers;

public class Undo
{
	public Guid Creator;
	public GameObject Prop;
	public Func<string> UndoCallback;
	public float Time;

	public Undo( Guid creator, GameObject prop, Func<string> callback )
	{
		Creator = creator;
		Prop = prop;
		UndoCallback = callback;
		Time = Sandbox.Time.Now;
	}
}
