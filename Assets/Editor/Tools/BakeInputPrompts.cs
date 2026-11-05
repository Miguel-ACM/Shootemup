using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public sealed class InputPromptImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/InputPrompts/Icons/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 256;
    }
}

// Pack the unchanged publisher PNGs once, for real image glyphs in TextMeshPro captions.
public static class BakeInputPrompts
{
    [MenuItem("Tools/Shootemup/Bake input prompt atlas")]
    public static void Run()
    {
        const string folder = "Assets/Resources/InputPrompts";
        var paths = Directory.GetFiles(folder + "/Icons", "*.png").OrderBy(p => p).ToArray();
        var textures = paths.Select(p => {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(p));
            return texture;
        }).ToArray();
        var atlas = new Texture2D(2048, 2048, TextureFormat.RGBA32, false);
        var rects = atlas.PackTextures(textures, 2, 2048);
        string atlasPath = folder + "/Inline.png", assetPath = folder + "/Inline.asset";
        File.WriteAllBytes(atlasPath, atlas.EncodeToPNG());
        AssetDatabase.ImportAsset(atlasPath, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(atlasPath);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport();
        var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(assetPath);
        if (asset == null) { asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>(); AssetDatabase.CreateAsset(asset, assetPath); }
        asset.name = "Inline";
        var serialized = new SerializedObject(asset);
        serialized.FindProperty("m_Version").stringValue = "1.1.0";
        serialized.ApplyModifiedPropertiesWithoutUndo();
        asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(asset.name);
        asset.spriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
        asset.faceInfo = new FaceInfo { pointSize = 100, scale = 1, lineHeight = 100, ascentLine = 85, descentLine = -15 };
        asset.spriteCharacterTable.Clear();
        asset.spriteGlyphTable.Clear();
        for (int i = 0; i < paths.Length; i++)
        {
            var rect = rects[i];
            int x = Mathf.RoundToInt(rect.x * atlas.width), y = Mathf.RoundToInt(rect.y * atlas.height);
            int w = Mathf.RoundToInt(rect.width * atlas.width), h = Mathf.RoundToInt(rect.height * atlas.height);
            var glyph = new TMP_SpriteGlyph { index = (uint)i, glyphRect = new GlyphRect(x, y, w, h),
                metrics = new GlyphMetrics(w, h, 0, h * .85f, w), scale = 1 };
            asset.spriteGlyphTable.Add(glyph);
            asset.spriteCharacterTable.Add(new TMP_SpriteCharacter(0xFFFE, glyph) { name = Path.GetFileNameWithoutExtension(paths[i]), scale = 1 });
        }
        if (asset.material == null)
        {
            asset.material = new Material(Shader.Find("TextMeshPro/Sprite")) { name = "Input prompt glyphs" };
            AssetDatabase.AddObjectToAsset(asset.material, asset);
        }
        asset.material.mainTexture = asset.spriteSheet;
        asset.UpdateLookupTables();
        EditorUtility.SetDirty(asset);
        EditorUtility.SetDirty(asset.material);
        AssetDatabase.SaveAssets();
        foreach (var texture in textures) Object.DestroyImmediate(texture);
        Object.DestroyImmediate(atlas);
        Debug.Log("Baked " + paths.Length + " licensed input prompt glyphs");
    }
}
