#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Applies correct import settings to anything ComfyUI generates under
    /// Assets/Art/Generated/, so nobody has to fix Filter Mode/compression by hand in
    /// the Inspector after every regeneration.
    ///
    /// FX sheets are imported as a single Sprite, not sliced via the Sprite Editor's
    /// multiple-sprite mode -- that API has moved between Unity versions and slicing
    /// programmatically without a live Editor to verify against risks silently doing
    /// nothing. Tools/ComfyUI/postprocess.py already writes a frame-rect JSON sidecar
    /// next to every FX sheet; runtime code (BattleVisuals/ClipPlayer, M4) builds
    /// per-frame Sprite.Create() calls from that JSON instead. Code-first, matches the
    /// project's minimal-Inspector-dependency approach anyway.
    /// </summary>
    public class GeneratedAssetImporter : AssetPostprocessor
    {
        const string GeneratedRoot = "Assets/Art/Generated/";
        const float PixelSpritePpu = 32f;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(GeneratedRoot)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;

            if (assetPath.Contains("/sprites/") || assetPath.Contains("/fx/"))
            {
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.spritePixelsPerUnit = PixelSpritePpu;
                importer.alphaIsTransparency = true;
            }
            else if (assetPath.Contains("/portraits/") || assetPath.Contains("/backgrounds/"))
            {
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Compressed;
            }
        }
    }
}
#endif
