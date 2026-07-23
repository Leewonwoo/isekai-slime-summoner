using UnityEditor;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class SummonerSkillIconSetup
    {
        static readonly string[] Paths =
        {
            "Assets/Art/UIIcons/icon_skill_meteor.png",
            "Assets/Art/UIIcons/icon_skill_ice_wall.png",
            "Assets/Art/UIIcons/icon_skill_aegis.png",
        };

        [MenuItem("Cross Defense/Setup/Summoner Skill Icons")]
        public static void Apply()
        {
            int configured = 0;
            for (int i = 0; i < Paths.Length; i++)
            {
                if (ConfigureSingleSprite(Paths[i]))
                    configured++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[CrossDefense] Summoner skill icons configured: {configured}/{Paths.Length}.");
        }

        internal static bool ConfigureSingleSprite(string path, float pixelsPerUnit = 100f)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                return false;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            return true;
        }
    }
}
