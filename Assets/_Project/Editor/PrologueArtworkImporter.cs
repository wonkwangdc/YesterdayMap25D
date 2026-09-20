#if UNITY_EDITOR
using UnityEditor;

namespace YesterdayMap.EditorTools
{
    public sealed class PrologueArtworkImporter : AssetPostprocessor
    {
        internal const string ArtworkFolder =
            "Assets/_Project/Art/UI/Prologue poto/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtworkFolder)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
