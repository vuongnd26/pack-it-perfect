using UnityEditor;
using UnityEngine;

namespace PackItPerfect.EditorTools
{
    /// <summary>Imports only approved sourced PNGs with a stable full-rectangle sprite contract.</summary>
    public sealed class SourcedArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/Sourced/", System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 128f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spritePivot = new Vector2(0.5f, 0.5f);
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            if (assetPath.EndsWith("/panel.png") || assetPath.EndsWith("/button.png"))
                importer.spriteBorder = new Vector4(12f, 12f, 12f, 12f);
            var android = importer.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.format = TextureImporterFormat.ASTC_4x4;
            android.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
