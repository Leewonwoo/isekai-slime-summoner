using CrossDefense.Data;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.EditorTools
{
    public static class StageTimelineRepairUtility
    {
        const string StagePath = "Assets/Data/StageTimelines/Stage_01.asset";
        const string BalancePath = "Assets/Data/StageBalance_Default.asset";
        const string RewardCatalogPath = "Assets/Data/RunRewards/RunRewardCatalog_Default.asset";

        static readonly string[] EarlyMonsters =
        {
            "Assets/Data/Monsters/Monster_GoblinGrunt.asset",
            "Assets/Data/Monsters/Monster_GoblinFireScout.asset",
            "Assets/Data/Monsters/Monster_GoblinFrostStalker.asset",
            "Assets/Data/Monsters/Monster_GoblinNatureRaider.asset",
        };

        static readonly string[] MidMonsters =
        {
            "Assets/Data/Monsters/Monster_GoblinSlinger.asset",
            "Assets/Data/Monsters/Monster_GoblinIceArcher.asset",
            "Assets/Data/Monsters/Monster_GoblinFireBomber.asset",
            "Assets/Data/Monsters/Monster_GoblinThornHunter.asset",
            "Assets/Data/Monsters/Monster_GoblinBarkGuard.asset",
            "Assets/Data/Monsters/Monster_GoblinSporeShaman.asset",
        };

        static readonly string[] LateMonsters =
        {
            "Assets/Data/Monsters/Monster_GoblinFireMage.asset",
            "Assets/Data/Monsters/Monster_GoblinIceBruiser.asset",
            "Assets/Data/Monsters/Monster_GoblinIceShaman.asset",
            "Assets/Data/Monsters/Monster_GoblinChief.asset",
            "Assets/Data/Monsters/Monster_GoblinWarlord.asset",
        };

        static readonly string[] BossMonsters =
        {
            "Assets/Data/Monsters/Monster_GoblinChief.asset",
            "Assets/Data/Monsters/Monster_GoblinWarlord.asset",
        };

        [MenuItem("Isekai Slime Summoner/Repair Stage 01 Timeline", priority = 12)]
        public static void RepairStage01()
        {
            var timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(StagePath);
            if (timeline == null)
            {
                Debug.LogError($"StageTimeline not found or not parseable: {StagePath}");
                return;
            }

            var serialized = new SerializedObject(timeline);
            serialized.FindProperty("stageId").stringValue = "stage-01-50-waves";
            serialized.FindProperty("displayName").stringValue = "\uACE0\uBE14\uB9B0 \uC232";
            serialized.FindProperty("balanceProfile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<StageBalanceProfile>(BalancePath);
            serialized.FindProperty("randomSeed").intValue = 20260720;
            serialized.FindProperty("randomizeDirectionWeights").boolValue = true;
            serialized.FindProperty("directionWeightJitter").floatValue = 0.15f;
            serialized.FindProperty("runTraitInterval").intValue = 5;
            serialized.FindProperty("runRewardCatalog").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<RunRewardCatalog>(RewardCatalogPath);

            var waves = serialized.FindProperty("waves");
            waves.arraySize = 50;

            for (int index = 0; index < waves.arraySize; index++)
                ConfigureWave(waves.GetArrayElementAtIndex(index), index);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(timeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(StagePath, ImportAssetOptions.ForceUpdate);
            Debug.Log("Repaired Stage_01 timeline with 50 serialized waves.");
        }

        static void ConfigureWave(SerializedProperty wave, int index)
        {
            int waveNumber = index + 1;
            bool isBoss = waveNumber % 5 == 0;

            wave.FindPropertyRelative("label").stringValue = isBoss ? $"Wave {waveNumber} - BOSS" : $"Wave {waveNumber}";
            wave.FindPropertyRelative("isBoss").boolValue = isBoss;
            wave.FindPropertyRelative("preparationTime").floatValue = Mathf.Max(2.5f, 5f - index * 0.05f);
            wave.FindPropertyRelative("hpMultiplier").floatValue = 1f + index * 0.085f;
            wave.FindPropertyRelative("speedMultiplier").floatValue = 1f + index * 0.006f;
            wave.FindPropertyRelative("summonContractReward").intValue = waveNumber == 50 ? 0 : isBoss ? 2 : 1;

            ConfigureSpawnZoneWeights(wave.FindPropertyRelative("spawnZoneWeights"), index % 4);
            ConfigureDirectionWeights(wave.FindPropertyRelative("directionWeights"));
            ConfigureSpawns(wave.FindPropertyRelative("monsterSpawns"), index, isBoss);
        }

        static void ConfigureSpawnZoneWeights(SerializedProperty weights, int focusIndex)
        {
            string[] zones = { "top", "right", "bottom", "left" };
            for (int i = 0; i < zones.Length; i++)
                weights.FindPropertyRelative(zones[i]).floatValue = i == focusIndex ? 3f : 1f;
        }

        static void ConfigureDirectionWeights(SerializedProperty weights)
        {
            weights.FindPropertyRelative("north").floatValue = 0f;
            weights.FindPropertyRelative("east").floatValue = 0f;
            weights.FindPropertyRelative("south").floatValue = 0f;
            weights.FindPropertyRelative("west").floatValue = 0f;
        }

        static void ConfigureSpawns(SerializedProperty spawns, int index, bool isBoss)
        {
            int normalSpawnCount = index < 6 ? 1 : index < 24 ? 2 : 3;
            spawns.arraySize = normalSpawnCount + (isBoss ? 1 : 0);

            for (int i = 0; i < normalSpawnCount; i++)
            {
                var monster = PickMonster(index, i);
                int count = Mathf.Clamp(6 + index / 2 + i, 6, 12);
                float interval = Mathf.Max(0.34f, 0.52f - index * 0.0035f + i * 0.05f);
                SetSpawn(spawns.GetArrayElementAtIndex(i), monster, count, interval, 1.5f, 1f, 1f);
            }

            if (isBoss)
            {
                int bossSlot = spawns.arraySize - 1;
                var boss = LoadMonster(BossMonsters[(index / 5) % BossMonsters.Length]);
                float hpMultiplier = 3f + (index / 5) * 0.3f;
                SetSpawn(spawns.GetArrayElementAtIndex(bossSlot), boss, 1, 1.25f, 3f, hpMultiplier, 6f);
            }
        }

        static MonsterData PickMonster(int waveIndex, int spawnIndex)
        {
            string[] pool = waveIndex < 12 ? EarlyMonsters : waveIndex < 32 ? MidMonsters : LateMonsters;
            return LoadMonster(pool[(waveIndex + spawnIndex) % pool.Length]);
        }

        static MonsterData LoadMonster(string path)
        {
            var monster = AssetDatabase.LoadAssetAtPath<MonsterData>(path);
            if (monster != null)
                return monster;

            Debug.LogWarning($"Monster asset missing for Stage_01 repair: {path}");
            return AssetDatabase.LoadAssetAtPath<MonsterData>(EarlyMonsters[0]);
        }

        static void SetSpawn(SerializedProperty spawn, MonsterData monster, int count, float interval,
            float sizeMultiplier, float hpMultiplier, float rewardMultiplier)
        {
            spawn.FindPropertyRelative("monster").objectReferenceValue = monster;
            spawn.FindPropertyRelative("count").intValue = count;
            spawn.FindPropertyRelative("spawnInterval").floatValue = interval;
            spawn.FindPropertyRelative("hpMultiplier").floatValue = hpMultiplier;
            spawn.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            spawn.FindPropertyRelative("rewardMultiplier").floatValue = rewardMultiplier;
            spawn.FindPropertyRelative("sizeMultiplier").floatValue = sizeMultiplier;
        }
    }
}
