using Sandbox;
using System.Numerics;

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

		RenderTexture = Texture.CreateRenderTarget( "scope", ImageFormat.RGBA8888, new Vector2(256,256) );

		_material.Set( "Color", RenderTexture );

		CameraComponent.FieldOfView = 15 / Zoom;

		//creates nre but works?
		try
		{
			CameraComponent.CustomProjectionMatrix = CreateSquareProjection( CameraComponent.FieldOfView, CameraComponent.ZNear, CameraComponent.ZFar );
		}
		catch { }
	}

	Matrix4x4 CreateSquareProjection( float verticalFovDegrees, float near, float far )
	{
		float fovRadians = MathF.PI / 180f * verticalFovDegrees;
		float f = 1.0f / MathF.Tan( fovRadians / 2.0f );

		return new Matrix4x4(
			f, 0, 0, 0,
			0, f, 0, 0,
			0, 0, far / (far - near), 1,
			0, 0, (-near * far) / (far - near), 0
		);
	}

	protected override void OnUpdate()
	{
		CameraComponent.RenderToTexture( RenderTexture );
	}
}
