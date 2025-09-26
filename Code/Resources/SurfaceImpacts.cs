[AssetType( Name = "Surface Extension", Extension = "simpact" )]
public class SurfaceImpacts : Surface
{
	public float Flammability { get; set; } = 1f;

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( $"💥", width, height, "#111" );
	}
}
