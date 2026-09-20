#if UNITY_EDITOR
using UnityEditor;

namespace YesterdayMap.Editor
{
    public sealed class ExplorationItemIconImporter : AssetPostprocessor
    {
        private const string ItemIconFolder =
            "Assets/_Project/Scenes/UI/item_icon/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ItemIconFolder)) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
#endif
