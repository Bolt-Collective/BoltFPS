using System;

namespace Seekers;

public sealed class PlayerUse : Component
{
	[RequireComponent] private Pawn Pawn { get; set; }

	private HighlightOutline lastGlow;

	protected override void OnUpdate()
	{
		var eyeTrace = Scene.Trace
			.Ray( Scene.Camera.Transform.World.ForwardRay, 60 )
			.WithoutTags( "hidezone" )
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

		if ( Input.Pressed( "Use" ) )
		{
			var pressableEvent = new IPressable.Event( this );

			if ( pressable is not null && pressable.CanPress( pressableEvent ) )
			{
				pressable.Press( pressableEvent );
			}
		}
	}
}
