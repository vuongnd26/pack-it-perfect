// Gives every illustration under Resources an Android ASTC override, so phones keep a quarter of the texture memory and
// load faster. Runs for new and re-imported art; the Editor and desktop builds keep the uncompressed source look.

using UnityEditor;
using UnityEngine;

namespace PackItPerfect.Editor
{
    /// <summary>Applies the Android texture override to sprites in Assets/Resources.</summary>
    public sealed class MobileTextureImport : AssetPostprocessor
    {
        const string ROOT = "Assets/Resources/";
        const string BACKGROUND_PREFIX = "bg_";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ROOT) || !(assetImporter is TextureImporter importer)) return;
            if (importer.textureType != TextureImporterType.Sprite) return;
            if (assetPath.Contains("EmojiAtlas")) return; // TextMeshPro's colour emoji atlas keeps its own settings

            // Full-screen backdrops are smooth gradients: a coarser block size is invisible there and halves them again.
            bool background = System.IO.Path.GetFileName(assetPath).StartsWith(BACKGROUND_PREFIX);
            var wanted = background ? TextureImporterFormat.ASTC_6x6 : TextureImporterFormat.ASTC_4x4;
            var android = importer.GetPlatformTextureSettings("Android");
            if (android.overridden && android.format == wanted && android.maxTextureSize == 2048) return;
            android.name = "Android";
            android.overridden = true;
            android.maxTextureSize = 2048;
            android.format = wanted;
            android.textureCompression = TextureImporterCompression.Compressed;
            android.compressionQuality = 75;
            importer.SetPlatformTextureSettings(android);
        }
    }
}
