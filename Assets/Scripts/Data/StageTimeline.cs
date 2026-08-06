using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum StageWaveKind { Normal, Boss, Rush }
    public enum PostWaveEvent { None, Merchant }

    [Serializable]
    public sealed class GoldenGoblinSettings
    {
        [SerializeField] bool enabled;
        [SerializeField] MonsterData monster;
        [Range(0f, 1f)] [SerializeField] float appearanceChance = 0.05f;
        [Min(1)] [SerializeField] int guaranteedInterval = 15;
        [Min(0f)] [SerializeField] float warningLeadTime = 1.2f;
        [Min(0.1f)] [SerializeField] float escapeDuration = 10f;
        [Min(0)] [SerializeField] int totalGoldReward = 50;
        [Min(0.01f)] [SerializeField] float hpMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float speedMultiplier = 1f;
        [Min(0.1f)] [SerializeField] float sizeMultiplier = 1f;

        public bool Enabled => enabled;
        public MonsterData Monster => monster;
        public float AppearanceChance => Mathf.Clamp01(appearanceChance);
        public int GuaranteedInterval => Mathf.Max(1, guaranteedInterval);
        public float WarningLeadTime => Mathf.Max(0f, warningLeadTime);
        public float EscapeDuration => Mathf.Max(0.1f, escapeDuration);
        public int TotalGoldReward => Mathf.Max(0, totalGoldReward);
        public float HpMultiplier => Mathf.Max(0.01f, hpMultiplier);
        public float SpeedMultiplier => Mathf.Max(0.01f, speedMultiplier);
        public float SizeMultiplier => Mathf.Max(0.1f, sizeMultiplier);
    }

    /// <summary>Reusable stage-wide balance knobs. Assign one profile to many stages to tune them together.</summary>
    [CreateAssetMenu(fileName = "StageBalanceProfile", menuName = "Isekai Slime Summoner/Data/Stage Balance Profile", order = 11)]
    public sealed class StageBalanceProfile : ScriptableObject
    {
        [Min(0.01f)] [SerializeField] float hpMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float speedMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float rewardMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float spawnIntervalMultiplier = 1f;

        public float HpMultiplier => hpMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float RewardMultiplier => rewardMultiplier;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;
    }

    [Serializable]
    public sealed class DirectionWeightSet
    {
        [Min(0)] [SerializeField] float north = 1f;
        [Min(0)] [SerializeField] float east = 1f;
        [Min(0)] [SerializeField] float south = 1f;
        [Min(0)] [SerializeField] float west = 1f;

        public float North => north;
        public float East => east;
        public float South => south;
        public float West => west;
        public float Total => north + east + south + west;
        public bool HasAnyDirection => Total > 0f;

        public float Get(Direction direction)
        {
            return direction switch
            {
                Direction.North => north,
                Direction.East => east,
                Direction.South => south,
                _ => west,
            };
        }
    }

    [Serializable]
    public sealed class SpawnZoneWeightSet
    {
        [Min(0)] [SerializeField] float top = 1f;
        [Min(0)] [SerializeField] float right = 1f;
        [Min(0)] [SerializeField] float bottom = 1f;
        [Min(0)] [SerializeField] float left = 1f;

        public float Top => top;
        public float Right => right;
        public float Bottom => bottom;
        public float Left => left;
        public float Total => top + right + bottom + left;
        public bool HasAnyZone => Total > 0f;

        public float Get(SpawnZone zone)
        {
            return zone switch
            {
                SpawnZone.Top => top,
                SpawnZone.Right => right,
                SpawnZone.Bottom => bottom,
                _ => left,
            };
        }
    }

    [Serializable]
    public sealed class MonsterSpawnEntry
    {
        [SerializeField] MonsterData monster;
        [Min(1)] [SerializeField] int count = 1;
        [Min(0.05f)] [SerializeField] float spawnInterval = 0.5f;
        [Min(0.01f)] [SerializeField] float hpMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float speedMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float rewardMultiplier = 1f;
        [Min(0.1f)] [SerializeField] float sizeMultiplier = 1f;

        public MonsterData Monster => monster;
        public int Count => count;
        public float SpawnInterval => spawnInterval;
        public float HpMultiplier => hpMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public float RewardMultiplier => rewardMultiplier;
        public float SizeMultiplier => sizeMultiplier;

        public static MonsterSpawnEntry CreatePrototype(
            MonsterData monster,
            int count,
            float interval,
            float sizeMultiplier = 1f)
        {
            return new MonsterSpawnEntry
            {
                monster = monster,
                count = count,
                spawnInterval = interval,
                sizeMultiplier = Mathf.Max(0.1f, sizeMultiplier),
            };
        }
    }

    [Serializable]
    public sealed class StageWave
    {
        [SerializeField] string label = "Wave";
        [SerializeField] StageWaveKind kind;
        // 기존 Stage_01의 보스 플래그를 보존하는 마이그레이션 필드.
        [SerializeField] bool isBoss;
        [Min(0)] [SerializeField] float preparationTime = 5f;
        [Min(0.01f)] [SerializeField] float hpMultiplier = 1f;
        [Min(0.01f)] [SerializeField] float speedMultiplier = 1f;
        [Min(0)] [SerializeField] int summonContractReward = 1;
        [Min(0)] [SerializeField] int clearGoldBonus;
        [SerializeField] PostWaveEvent postClearEvent;
        [Min(1)] [SerializeField] int maxLivingMonsters = 64;
        [SerializeField] SpawnZoneWeightSet spawnZoneWeights = new();
        // 기존 에셋 호환용. 새 런타임은 spawnZoneWeights를 우선 사용한다.
        [SerializeField] DirectionWeightSet directionWeights = new();
        [SerializeField] List<MonsterSpawnEntry> monsterSpawns = new();

        public string Label => label;
        public StageWaveKind Kind => isBoss ? StageWaveKind.Boss : kind;
        public bool IsBoss => Kind == StageWaveKind.Boss;
        public bool IsRush => Kind == StageWaveKind.Rush;
        public float PreparationTime => preparationTime;
        public float HpMultiplier => hpMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public int SummonContractReward => summonContractReward;
        public int ClearGoldBonus => Mathf.Max(0, clearGoldBonus);
        public PostWaveEvent PostClearEvent => postClearEvent;
        public int MaxLivingMonsters => Mathf.Max(1, maxLivingMonsters);
        public SpawnZoneWeightSet SpawnZoneWeights => spawnZoneWeights;
        public DirectionWeightSet DirectionWeights => directionWeights;
        public IReadOnlyList<MonsterSpawnEntry> MonsterSpawns => monsterSpawns;

        public int TotalMonsterCount
        {
            get
            {
                int total = 0;
                foreach (var spawn in monsterSpawns)
                    if (spawn != null) total += Mathf.Max(0, spawn.Count);
                return total;
            }
        }

        public static StageWave CreatePrototype(string label, MonsterData monster, int count, bool isBoss = false)
        {
            return new StageWave
            {
                label = label,
                kind = isBoss ? StageWaveKind.Boss : StageWaveKind.Normal,
                isBoss = isBoss,
                preparationTime = 1f,
                summonContractReward = isBoss ? 2 : 1,
                monsterSpawns = new List<MonsterSpawnEntry>
                {
                    MonsterSpawnEntry.CreatePrototype(monster, count, 0.35f),
                },
            };
        }
    }

    /// <summary>
    /// Runtime stage data. The editor changes this asset only; the game loop can consume the read-only properties.
    /// </summary>
    [CreateAssetMenu(fileName = "StageTimeline", menuName = "Isekai Slime Summoner/Data/Stage Timeline", order = 0)]
    public sealed class StageTimeline : ScriptableObject
    {
        [Header("Stage")]
        [SerializeField] string stageId = "stage-01";
        [SerializeField] string displayName = "Stage 1";
        [SerializeField] string nextSceneName;
        [SerializeField] StageBalanceProfile balanceProfile;

        [Header("Run Variation")]
        [SerializeField] int randomSeed = 2026;
        [SerializeField] bool randomizeDirectionWeights = true;
        [Range(0f, 1f)] [SerializeField] float directionWeightJitter = 0.15f;

        [Header("Timeline")]
        [Min(1)] [SerializeField] int runTraitInterval = 5;
        [Min(0)] [SerializeField] int startingOverdriveGauge;
        [SerializeField] RunRewardCatalog runRewardCatalog;
        [SerializeField] GoldenGoblinSettings goldenGoblin = new();
        [SerializeField] List<StageWave> waves = new();

        public string StageId => stageId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string NextSceneName => nextSceneName ?? string.Empty;
        public StageBalanceProfile BalanceProfile => balanceProfile;
        public int RandomSeed => randomSeed;
        public bool RandomizeDirectionWeights => randomizeDirectionWeights;
        public float DirectionWeightJitter => directionWeightJitter;
        public int RunTraitInterval => Mathf.Max(1, runTraitInterval);
        public int StartingOverdriveGauge => Mathf.Max(0, startingOverdriveGauge);
        public RunRewardCatalog RunRewardCatalog => runRewardCatalog;
        public GoldenGoblinSettings GoldenGoblin => goldenGoblin;
        public IReadOnlyList<StageWave> Waves => waves;
        public int WaveCount => waves.Count;

        public bool ShouldOfferRunTrait(int clearedWave) =>
            clearedWave > 0 && clearedWave % RunTraitInterval == 0;

        public bool ShouldSpawnGoldenGoblin(int day, int runEventSeed)
        {
            if (day <= 0 || goldenGoblin == null || !goldenGoblin.Enabled ||
                goldenGoblin.Monster == null)
                return false;
            if (day % goldenGoblin.GuaranteedInterval == 0)
                return true;

            int seed = GoldenGoblinRollSeed(randomSeed, runEventSeed, day);
            return new System.Random(seed).NextDouble() < goldenGoblin.AppearanceChance;
        }

        public static int GoldenGoblinRollSeed(int stageSeed, int runEventSeed, int day)
        {
            unchecked
            {
                uint hash = 2166136261u;
                hash = (hash ^ (uint)stageSeed) * 16777619u;
                hash = (hash ^ (uint)runEventSeed) * 16777619u;
                hash = (hash ^ (uint)day) * 16777619u;
                hash = (hash ^ 0x474F4C44u) * 16777619u;
                return (int)(hash & 0x7FFFFFFF);
            }
        }

        public bool TryGetWave(int zeroBasedIndex, out StageWave wave)
        {
            if (zeroBasedIndex >= 0 && zeroBasedIndex < waves.Count)
            {
                wave = waves[zeroBasedIndex];
                return wave != null;
            }

            wave = null;
            return false;
        }

        public IEnumerable<MonsterData> EnumerateMonsters()
        {
            var seen = new HashSet<string>();
            if (goldenGoblin?.Monster != null &&
                seen.Add(goldenGoblin.Monster.MonsterId))
                yield return goldenGoblin.Monster;
            foreach (StageWave wave in waves)
            {
                if (wave?.MonsterSpawns == null) continue;
                foreach (MonsterSpawnEntry entry in wave.MonsterSpawns)
                    if (entry?.Monster != null && seen.Add(entry.Monster.MonsterId)) yield return entry.Monster;
            }
        }

        public static StageTimeline CreatePrototype(int waveCount = 3, Sprite defaultMonsterSprite = null,
            Sprite[] defaultMonsterMoveFrames = null)
        {
            var timeline = CreateInstance<StageTimeline>();
            timeline.stageId = "runtime-prototype";
            timeline.displayName = "Runtime Prototype";
            timeline.randomSeed = 20260714;
            timeline.randomizeDirectionWeights = false;
            timeline.runRewardCatalog = RunRewardCatalog.CreateRuntimeDefault();

            var basic = MonsterData.CreatePrototype("runtime-basic", "Prototype Goblin", MonsterShape.Grunt,
                MonsterAttribute.None, 40, 0.75f, 5, 2, defaultMonsterSprite, defaultMonsterMoveFrames);
            var fast = MonsterData.CreatePrototype("runtime-fast", "Prototype Fast Goblin", MonsterShape.Scout,
                MonsterAttribute.Fire, 24, 1.2f, 4, 3, defaultMonsterSprite, defaultMonsterMoveFrames);

            int count = Mathf.Max(1, waveCount);
            for (int i = 0; i < count; i++)
            {
                var monster = i % 2 == 0 ? basic : fast;
                timeline.waves.Add(StageWave.CreatePrototype($"Wave {i + 1}", monster, 3 + i * 2, i == count - 1));
            }

            return timeline;
        }

        public float GetMonsterHpMultiplier(StageWave wave, MonsterSpawnEntry spawn)
        {
            return GetProfileValue(profile => profile.HpMultiplier) * wave.HpMultiplier * spawn.HpMultiplier;
        }

        public float GetMonsterSpeedMultiplier(StageWave wave, MonsterSpawnEntry spawn)
        {
            return GetProfileValue(profile => profile.SpeedMultiplier) * wave.SpeedMultiplier * spawn.SpeedMultiplier;
        }

        public float GetGoldenGoblinHpMultiplier(StageWave wave) =>
            GetProfileValue(profile => profile.HpMultiplier) * wave.HpMultiplier;

        public float GetGoldenGoblinSpeedMultiplier(StageWave wave) =>
            GetProfileValue(profile => profile.SpeedMultiplier) * wave.SpeedMultiplier;

        public float GetSpawnInterval(StageWave wave, MonsterSpawnEntry spawn)
        {
            return spawn.SpawnInterval * GetProfileValue(profile => profile.SpawnIntervalMultiplier);
        }

        public SpawnZone ChooseSpawnZone(StageWave wave, System.Random random)
        {
            var weights = wave.SpawnZoneWeights;
            if (weights == null || !weights.HasAnyZone)
                return (SpawnZone)random.Next(0, 4);

            float roll = (float)random.NextDouble() * weights.Total;
            foreach (var zone in new[] { SpawnZone.Top, SpawnZone.Right, SpawnZone.Bottom, SpawnZone.Left })
            {
                roll -= weights.Get(zone);
                if (roll <= 0f) return zone;
            }

            return SpawnZone.Left;
        }

        public int GetMonsterReward(MonsterSpawnEntry spawn)
        {
            if (spawn == null || spawn.Monster == null) return 0;
            float reward = spawn.Monster.RewardGold * spawn.RewardMultiplier * GetProfileValue(profile => profile.RewardMultiplier);
            return Mathf.Max(0, Mathf.RoundToInt(reward));
        }

        public IEnumerable<string> Validate()
        {
            if (runRewardCatalog == null)
                yield return "The stage has no run reward catalog.";
            else
            {
                foreach (string warning in runRewardCatalog.Validate())
                    yield return $"Run reward catalog: {warning}";
            }

            if (goldenGoblin != null && goldenGoblin.Enabled)
            {
                if (goldenGoblin.Monster == null)
                    yield return "Golden goblin is enabled but has no Monster Profile.";
                else if (goldenGoblin.Monster.Behavior != MonsterBehavior.GoldenRunner)
                    yield return "Golden goblin Monster Profile is not configured as GoldenRunner.";
                if (goldenGoblin.AppearanceChance < 0f || goldenGoblin.AppearanceChance > 1f)
                    yield return "Golden goblin appearance chance must be between 0 and 1.";
            }

            if (waves == null || waves.Count == 0)
            {
                yield return "The stage has no waves.";
                yield break;
            }

            for (int i = 0; i < waves.Count; i++)
            {
                var wave = waves[i];
                if (wave == null)
                {
                    yield return $"Wave {i + 1} is null.";
                    continue;
                }

                if (wave.MonsterSpawns == null || wave.MonsterSpawns.Count == 0)
                    yield return $"Wave {i + 1} has no monster entries.";
                if (wave.SpawnZoneWeights == null || !wave.SpawnZoneWeights.HasAnyZone)
                    yield return $"Wave {i + 1} has no viewport spawn-zone weight.";
                if (wave.SummonContractReward < 0)
                    yield return $"Wave {i + 1} has a negative summon contract reward.";
                if (wave.IsRush && wave.MaxLivingMonsters > 64)
                    yield return $"Wave {i + 1} rush living-monster cap exceeds 64.";

                if (wave.MonsterSpawns == null) continue;
                for (int j = 0; j < wave.MonsterSpawns.Count; j++)
                {
                    if (wave.MonsterSpawns[j] == null || wave.MonsterSpawns[j].Monster == null)
                        yield return $"Wave {i + 1}, monster entry {j + 1} has no Monster Profile.";
                    else if (wave.MonsterSpawns[j].SizeMultiplier <= 0f)
                        yield return $"Wave {i + 1}, monster entry {j + 1} has an invalid size multiplier.";
                }
            }
        }

        float GetProfileValue(Func<StageBalanceProfile, float> selector) =>
            balanceProfile == null ? 1f : selector(balanceProfile);
    }
}
