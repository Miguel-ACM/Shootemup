using UnityEditor;
using UnityEngine;

/// <summary>Keep the cloud artwork on the same coarse texture grid as the PSX scene.</summary>
public sealed class CloudBillowImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (assetPath != "Assets/Resources/CloudBillow.png")
            return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Sprite;
        texture.textureShape = TextureImporterShape.Texture2D;
        texture.spriteImportMode = SpriteImportMode.Single;
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.mipmapEnabled = false;
        texture.alphaIsTransparency = true;
        texture.isReadable = true; // The curtain samples one opaque white texel for its backing.
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.maxTextureSize = 128;
        texture.npotScale = TextureImporterNPOTScale.None;
    }
}
