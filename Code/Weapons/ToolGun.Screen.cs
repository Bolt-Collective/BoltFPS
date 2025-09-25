using Sandbox.Rendering;

namespace Seekers;

public partial class ToolGun
{
	private Texture ScreenTexture;

	void UpdateScreen() //
	{
		if ( !ViewModel.IsValid() )
			return;

		var tgViewmodel = ViewModel?.Components.GetInDescendants<SkinnedModelRenderer>();

		if ( !tgViewmodel.IsValid() )
			return;

		if ( !tgViewmodel.Model.IsValid() )
			return;

		var oldScreenMat = tgViewmodel?.Model?.Materials.FirstOrDefault( x => x.Name.Contains( "display" ) );


		var index = tgViewmodel.Model.Materials.IndexOf( oldScreenMat );
		Log.Info( index );
		if ( index < 2 ) return;


		ScreenTexture ??= Texture.CreateRenderTarget()
			.WithSize( 512, 128 )
			.WithInitialColor( Color.Red )
			.Create();

		ScreenTexture.Clear( Color.Random );

		oldScreenMat?.Set( "Color", ScreenTexture );

		OverlayScreenOnModel( tgViewmodel );
	}

	void OverlayScreenOnModel( SkinnedModelRenderer renderer )
	{
		var rt = RenderTarget.From( ScreenTexture );

		var cl = new CommandList();
		renderer.ExecuteBefore = cl;
		cl.SetRenderTarget( rt );
		cl.Clear( Color.Black );

		var toolName = CurrentTool?.GetType().Name;

		float glow = 0.85f + 0.15f * MathF.Sin( Time.Now * 6f );

		float jitter = 0.02f * MathF.Sin( Time.Now * 120f );
		float intensity = Math.Clamp( glow + jitter, 0f, 1f );

		var text = new TextRendering.Scope( toolName, Color.White, 100 );

		text.FilterMode = FilterMode.Point;
		text.FontName = "Roboto";
		text.FontWeight = 700;

		var baseColor = new Color( 0.8f, 0.9f, 1f ); // soft bluish white
		text.TextColor = baseColor * intensity;

		cl.Paint.DrawText( text, new Rect( new Vector2( 0, 0 ), ScreenTexture.Size ), TextFlag.Center );

		cl.ClearRenderTarget();
	}
}
