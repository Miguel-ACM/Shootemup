#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public sealed class ProjectileSpriteImporter : AssetPostprocessor
{
    public override uint GetVersion()=>2;
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/ProjectileSprites/") || !assetPath.EndsWith(".png"))return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.isReadable=true;
        importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled=false;importer.npotScale=TextureImporterNPOTScale.None;
        importer.alphaIsTransparency=false;importer.maxTextureSize=32;
    }
}
#endif
