using System;
using static Sandbox.Component;

namespace Seekers;

public sealed class PlayerUse : Component
{
	[RequireComponent] private Pawn Pawn { get; set; }

	private HighlightOutline lastGlow;

	protected override void OnUpdate()
	{
		var dis = 120f;
		if ( Pawn.Controller.ThirdPerson )
			dis += -Pawn.Controller.ThirdPersonCameraOffset.x;

		var eyeTrace = Scene.Trace
			.Ray( Pawn.AimRay, dis )
			.WithoutTags( "movement" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !eyeTrace.Hit || !eyeTrace.GameObject.IsValid() ||
		     !eyeTrace.GameObject.Components.TryGet( out IPressable pressable ) )
		{
			if ( lastGlow != null )
			{
				lastGlow.Enabled = false;
				lastGlow = null;
			}

			return;
		}

		var glow = eyeTrace.GameObject.Components.GetOrCreate<HighlightOutline>();

		float pulse = (MathF.Sin( Time.Now * 5f ) + 1f) * 0.5f;
		float alpha = 0.01f + pulse * 0.03f; // Range

		glow.Enabled = true;
		glow.InsideColor = new Color( 1f, 1f, 1f, alpha );
		glow.ObscuredColor = Color.Transparent;
		glow.Width = 0;

		if ( lastGlow != null && lastGlow != glow )
		{
			lastGlow.Enabled = false;
		}

		lastGlow = glow;

		var pressableEvent = new IPressable.Event( this );

		pressable.Look( pressableEvent );

		if ( Input.Pressed( "Use" ))
		{
			TryUse( pressable, pressableEvent );
		}
	}

	public async void TryUse( IPressable pressable, IPressable.Event pressableEvent )
	{
		await Task.Frame();
		await Task.Frame();

		if ( !Pawn.CanUse )
			return;

		if ( pressable is not null && pressable.CanPress( pressableEvent ) )
		{
			pressable.Press( pressableEvent );
		}
	}
}
