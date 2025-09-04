﻿namespace Seekers;

[AssetType( Name = "Sandbox Entity", Extension = "sent", Category = "Sandbox", Flags = AssetTypeFlags.NoEmbedding )]
public class ScriptedEntity : GameResource
{
	[Property] public PrefabFile Prefab { get; set; }

	[Property] public string Title { get; set; }

	[Property] public string Description { get; set; }

	/// <summary>
	/// If this entity uses code then you should enable this so the code is included when publishing.
	/// </summary>
	[Property]
	public bool IncludeCode { get; set; }

	public override Bitmap RenderThumbnail( ThumbnailOptions options )
	{
		// No prefab - can't make a thumbnail
		if ( Prefab is null ) return default;

		var bitmap = new Bitmap( options.Width, options.Height );
		bitmap.Clear( Color.Transparent );

		SceneUtility.RenderGameObjectToBitmap( Prefab.GetScene(), bitmap );

		return bitmap;
	}

	protected override Bitmap CreateAssetTypeIcon( int width, int height )
	{
		return CreateSimpleAssetTypeIcon( "📦", width, height, "#f54248" );
	}

	public override void ConfigurePublishing( ResourcePublishContext context )
	{
		if ( Prefab is null )
		{
			context.SetPublishingDisabled( "Invalid: missing a prefab" );
			return;
		}

		context.IncludeCode = IncludeCode;
	}
}
