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

		var text = new TextRendering.Scope( toolName, Color.White, 100 );

		text.LineHeight = 0.75f;
		text.FontName = "Work Sans";
		text.TextColor = Color.White.Lighten( 2f );
		text.FontWeight = 700;

		cl.Paint.DrawText( text, new Rect( new Vector2( 0, 0 ), ScreenTexture.Size ), TextFlag.Center );

		cl.ClearRenderTarget();
	}
}
