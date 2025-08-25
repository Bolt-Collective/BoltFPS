using Sandbox.Engine;

namespace Seekers;

public sealed class ButtonEntity : OwnedEntity, Component.IPressable
{
	public bool Press(IPressable.Event e)
	{
		Log.Info( "pressed" );
		Set( Toggle ? !On : true );
		return true;
	}

	[RequireComponent,Property]
	public SkinnedModelRenderer Renderer { get; set; }

	[Property, Sync]
	public bool Toggle { get; set; }

	[Property]
	public InputBind InputBind { get; set; }

	public bool On { get; set; }

	public override void OwnerUpdate()
	{
		if (On)
		{
			InputBind.Override( InputBind );
			if ( !Toggle )
				Set( false );
		}
	}

	[Rpc.Broadcast]
	public void Set(bool value)
	{
		Renderer.Set( "on", value );
		On = value;
	}

	protected override void OnUpdate()
	{
		base.OnUpdate();
		Renderer.Set( "on", On );
	}

}
