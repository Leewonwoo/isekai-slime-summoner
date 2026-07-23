using System;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class FeatureCatalogSetup
    {
        const string EquipmentPath = "Assets/Data/EquipmentCatalog_Default.asset";
        const string MerchantPath = "Assets/Data/MerchantCatalog_Default.asset";
        const string MonsterPath = "Assets/Data/MonsterCatalog_Default.asset";
        const string TimelinePath = "Assets/Data/StageTimelines/Stage_01.asset";
        const string ScenePath = "Assets/Scenes/SampleScene.unity";

        static readonly int[] RushWaves = { 8, 18, 28, 38, 48 };
        static readonly int[] RushGold = { 40, 70, 100, 130, 160 };
        static readonly string[] MonsterAssets =
        {
            "Monster_GoblinGrunt", "Monster_GoblinFireScout", "Monster_GoblinIceBruiser",
            "Monster_GoblinNatureRaider", "Monster_GoblinChief", "Monster_GoblinWarlord",
            "Monster_GoblinSlinger", "Monster_GoblinGolden", "Monster_GoblinFireMage",
            "Monster_GoblinFireBomber", "Monster_GoblinFrostStalker", "Monster_GoblinIceArcher",
            "Monster_GoblinIceShaman", "Monster_GoblinThornHunter", "Monster_GoblinBarkGuard",
            "Monster_GoblinSporeShaman",
        };

        [MenuItem("Cross Defense/Setup/Apply Codex Rush Merchant Data")]
        public static void Apply()
        {
            EquipmentCatalog equipment = EnsureEquipmentCatalog();
            MerchantCatalog merchant = EnsureMerchantCatalog(equipment);
            MonsterCatalog monsters = EnsureMonsterCatalog();
            ConfigureTimeline();
            AssignSceneCatalogs(equipment, merchant, monsters);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrossDefense] 슬라임 해금·도감·러쉬·행상인 데이터 구성을 완료했습니다.");
        }

        public static void ApplyFromCommandLine()
        {
            Apply();
            EditorApplication.Exit(0);
        }

        static EquipmentCatalog EnsureEquipmentCatalog()
        {
            EquipmentCatalog existing = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>(EquipmentPath);
            if (existing != null) return existing;
            if (AssetDatabase.LoadMainAssetAtPath(EquipmentPath) != null)
                AssetDatabase.DeleteAsset(EquipmentPath);
            EquipmentCatalog catalog = EquipmentCatalog.CreateRuntimeDefault();
            catalog.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(catalog, EquipmentPath);
            foreach (EquipmentData item in catalog.Equipment)
            {
                item.hideFlags = HideFlags.None;
                item.name = item.EquipmentId;
                AssetDatabase.AddObjectToAsset(item, catalog);
            }
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static MerchantCatalog EnsureMerchantCatalog(EquipmentCatalog equipment)
        {
            MerchantCatalog existing = AssetDatabase.LoadAssetAtPath<MerchantCatalog>(MerchantPath);
            if (existing != null)
            {
                var serialized = new SerializedObject(existing);
                serialized.FindProperty("equipmentCatalog").objectReferenceValue = equipment;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(existing);
                return existing;
            }
            MerchantCatalog catalog = MerchantCatalog.CreateRuntimeDefault(equipment);
            catalog.hideFlags = HideFlags.None;
            AssetDatabase.CreateAsset(catalog, MerchantPath);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static MonsterCatalog EnsureMonsterCatalog()
        {
            MonsterCatalog catalog = AssetDatabase.LoadAssetAtPath<MonsterCatalog>(MonsterPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<MonsterCatalog>();
                AssetDatabase.CreateAsset(catalog, MonsterPath);
            }
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("monsters");
            list.arraySize = MonsterAssets.Length;
            for (int i = 0; i < MonsterAssets.Length; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<MonsterData>($"Assets/Data/Monsters/{MonsterAssets[i]}.asset");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        static void ConfigureTimeline()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(TimelinePath);
            if (timeline == null) throw new InvalidOperationException("Stage_01 asset not found.");
            var serialized = new SerializedObject(timeline);
            SerializedProperty waves = serialized.FindProperty("waves");
            for (int i = 0; i < waves.arraySize; i++)
            {
                int waveNumber = i + 1;
                SerializedProperty wave = waves.GetArrayElementAtIndex(i);
                bool legacyBoss = wave.FindPropertyRelative("isBoss").boolValue;
                int rushIndex = Array.IndexOf(RushWaves, waveNumber);
                SerializedProperty kind = wave.FindPropertyRelative("kind");
                bool alreadyRush = kind.enumValueIndex == (int)StageWaveKind.Rush;
                if (rushIndex >= 0)
                {
                    kind.enumValueIndex = (int)StageWaveKind.Rush;
                    wave.FindPropertyRelative("isBoss").boolValue = false;
                    wave.FindPropertyRelative("label").stringValue = $"Wave {waveNumber} - RUSH";
                    wave.FindPropertyRelative("clearGoldBonus").intValue = RushGold[rushIndex];
                    wave.FindPropertyRelative("postClearEvent").enumValueIndex = (int)PostWaveEvent.Merchant;
                    wave.FindPropertyRelative("maxLivingMonsters").intValue = 64;
                    if (!alreadyRush)
                    {
                        SerializedProperty spawns = wave.FindPropertyRelative("monsterSpawns");
                        for (int j = 0; j < spawns.arraySize; j++)
                        {
                            SerializedProperty spawn = spawns.GetArrayElementAtIndex(j);
                            SerializedProperty count = spawn.FindPropertyRelative("count");
                            SerializedProperty interval = spawn.FindPropertyRelative("spawnInterval");
                            count.intValue = Mathf.Max(1, Mathf.CeilToInt(count.intValue * 2.5f));
                            interval.floatValue = Mathf.Max(0.05f, interval.floatValue * 0.35f);
                        }
                    }
                }
                else
                {
                    kind.enumValueIndex = legacyBoss ? (int)StageWaveKind.Boss : (int)StageWaveKind.Normal;
                    wave.FindPropertyRelative("maxLivingMonsters").intValue = 64;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(timeline);
        }

        static void AssignSceneCatalogs(EquipmentCatalog equipment, MerchantCatalog merchant, MonsterCatalog monsters)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            if (game == null) throw new InvalidOperationException("GameManager not found in SampleScene.");
            var serialized = new SerializedObject(game);
            serialized.FindProperty("equipmentCatalog").objectReferenceValue = equipment;
            serialized.FindProperty("merchantCatalog").objectReferenceValue = merchant;
            serialized.FindProperty("monsterCatalog").objectReferenceValue = monsters;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(game);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
