using Sandbox;

public sealed class Scope : Component
{
	[Property] public CameraComponent CameraComponent { get; set; }
	[Property] public Material Target { get; set; }
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public float Zoom { get; set; } = 4;

	Material _material;

	private Texture RenderTexture;

	protected override void OnStart()
	{
		_material = Target.CreateCopy();

		Renderer.Materials.SetOverride( 0, _material );

		RenderTexture = Texture.CreateRenderTarget( "scope", ImageFormat.RGBA8888, Screen.Size );

		_material.Set( "Color", RenderTexture );

		CameraComponent.FieldOfView = 90 / Zoom;
	}

	protected override void OnUpdate()
	{
		CameraComponent.RenderToTexture( RenderTexture );

		CameraComponent.WorldTransform = Scene.Camera.WorldTransform;
	}
}
