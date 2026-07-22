using System;
using System.IO;
using System.Linq;
using CrossDefense.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class SummonerSkillEffectSetup
    {
        const string MeteorPath = "Assets/Art/Projectiles/effect_fire_explosion_sheet.png";
        const string MeteorProjectilePath =
            "Assets/Art/Projectiles/projectile_summoner_meteor.png";
        const string IceWallPath = "Assets/Art/Projectiles/effect_ice_wall_sheet.png";
        const string EruptionPath =
            "Assets/Art/Projectiles/effect_explosion_slime_star3_eruption_sheet.png";
        const string AegisPath = "Assets/Art/Projectiles/effect_star3_impact_neutral.png";

        [MenuItem("Cross Defense/Setup/Summoner Skill Effects")]
        public static void Apply()
        {
            ConfigureNineFrameSheet(MeteorPath, new Vector2(0.5f, 0.5f));
            SummonerSkillIconSetup.ConfigureSingleSprite(MeteorProjectilePath);
            ConfigureNineFrameSheet(IceWallPath, new Vector2(0.5f, 0.1f));
            ConfigureNineFrameSheet(EruptionPath, new Vector2(0.5f, 0.1f));

            Sprite[] meteor = LoadFrames(MeteorPath);
            Sprite meteorProjectile = AssetDatabase.LoadAssetAtPath<Sprite>(MeteorProjectilePath);
            Sprite[] iceWall = LoadFrames(IceWallPath);
            Sprite[] eruption = LoadFrames(EruptionPath);
            Sprite aegis = AssetDatabase.LoadAssetAtPath<Sprite>(AegisPath);
            GameManager[] managers = UnityEngine.Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                var serialized = new SerializedObject(managers[i]);
                serialized.FindProperty("runtimeMeteorProjectileSprite").objectReferenceValue =
                    meteorProjectile;
                SetSpriteArray(serialized.FindProperty("runtimeMeteorEffectFrames"), meteor);
                SetSpriteArray(serialized.FindProperty("runtimeIceWallEffectFrames"), iceWall);
                SetSpriteArray(serialized.FindProperty("runtimeExplosionStar3EffectFrames"), eruption);
                serialized.FindProperty("runtimeAegisEffectSprite").objectReferenceValue = aegis;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(managers[i]);
            }

            if (managers.Length > 0)
                EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[CrossDefense] Summoner skill effects configured: " +
                $"meteorProjectile={(meteorProjectile != null ? 1 : 0)}, " +
                $"meteor={meteor.Length}, iceWall={iceWall.Length}, eruption={eruption.Length}, " +
                $"managers={managers.Length}.");
        }

        internal static void ConfigureNineFrameSheet(string path, Vector2 pivot)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                throw new InvalidOperationException($"TextureImporter를 찾을 수 없습니다: {path}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 128f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();

            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            ISpriteEditorDataProvider provider = CreateProvider(importer);
            if (provider == null)
                throw new InvalidOperationException($"Sprite data provider를 만들 수 없습니다: {path}");

            string baseName = Path.GetFileNameWithoutExtension(path);
            var rects = new SpriteRect[9];
            for (int index = 0; index < rects.Length; index++)
            {
                int column = index % 3;
                int rowFromTop = index / 3;
                rects[index] = new SpriteRect
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(column * 128, 384 - ((rowFromTop + 1) * 128), 128, 128),
                    alignment = SpriteAlignment.Custom,
                    pivot = pivot,
                    spriteID = GUID.Generate(),
                };
            }
            provider.SetSpriteRects(rects);
            ISpriteNameFileIdDataProvider names =
                provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
            names?.SetNameFileIdPairs(
                rects.Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID)).ToArray());
            provider.Apply();
            importer.SaveAndReimport();
        }

        static ISpriteEditorDataProvider CreateProvider(TextureImporter importer)
        {
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider =
                factories.GetSpriteEditorDataProviderFromObject(importer);
            provider?.InitSpriteEditorDataProvider();
            return provider;
        }

        internal static Sprite[] LoadFrames(string path) =>
            AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseIndex(sprite.name))
                .ToArray();

        static int ParseIndex(string name)
        {
            int separator = name.LastIndexOf('_');
            return separator >= 0 && int.TryParse(name[(separator + 1)..], out int index)
                ? index
                : int.MaxValue;
        }

        internal static void SetSpriteArray(SerializedProperty property, Sprite[] sprites)
        {
            property.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }
    }
}
