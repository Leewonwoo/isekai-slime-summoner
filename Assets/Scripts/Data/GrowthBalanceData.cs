using System;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum RunUpgradeType
    {
        AttackPower,
        AttackSpeed,
        CoreRecovery,
        CriticalChance,
        SummonCapacity,
    }

    public enum PermanentTraitType
    {
        SummonerPower,
        SummonerHaste,
        CoreVitality,
        SlimePower,
        SlimeHaste,
        LuckySummon,
        SummonCapacity,
        EquipmentSupply,
        RelicDiscovery,
    }

    /// <summary>
    /// 소환사 영구 성장과 런 전용 강화의 비용·배율을 한 곳에서 조정하는 데이터다.
    /// </summary>
    [CreateAssetMenu(fileName = "GrowthBalance", menuName = "Isekai Slime Summoner/Data/Growth Balance", order = 30)]
    public sealed class GrowthBalanceData : ScriptableObject
    {
        [Header("Permanent Summoner Level")]
        [Min(2)] [SerializeField] int summonerMaxLevel = 100;
        [Min(1)] [SerializeField] int summonerBaseExperienceToNext = 20;
        [Min(1f)] [SerializeField] float summonerExperienceGrowth = 1.18f;
        [Min(0)] [SerializeField] int summonerBaseExperiencePerKill = 1;
        [Min(0f)] [SerializeField] float summonerExperiencePerRewardGold = 1f;
        [Min(0f)] [SerializeField] float summonerDamagePerLevel = 0.04f;
        [Min(0f)] [SerializeField] float summonerMaxHpPerLevel = 0.03f;
        [Range(0f, 0.1f)] [SerializeField] float jackpotChancePerLevel = 0.0025f;
        [Range(0f, 0.5f)] [SerializeField] float maxJackpotChanceBonus = 0.15f;

        [Header("Permanent Level-up Traits")]
        [Min(0f)] [SerializeField] float traitSummonerDamagePerLevel = 0.08f;
        [Min(0f)] [SerializeField] float traitSummonerAttackSpeedPerLevel = 0.05f;
        [Min(0f)] [SerializeField] float traitCoreMaxHpPerLevel = 0.05f;
        [Min(0f)] [SerializeField] float traitSlimeDamagePerLevel = 0.06f;
        [Min(0f)] [SerializeField] float traitSlimeAttackSpeedPerLevel = 0.04f;
        [Range(0f, 0.1f)] [SerializeField] float traitJackpotChancePerLevel = 0.005f;
        [Min(1)] [SerializeField] int traitSummonCapacityPerLevel = 1;
        [Min(1)] [SerializeField] int traitSummonCapacityMaxLevel = 4;

        [Header("Instant Slime Level")]
        [Min(2)] [SerializeField] int slimeMaxLevel = 30;
        [Min(1)] [SerializeField] int slimeBaseLevelUpCost = 25;
        [Min(1f)] [SerializeField] float slimeLevelUpCostGrowth = 1.28f;
        [Min(0f)] [SerializeField] float slimeDamagePerLevel = 0.08f;
        [Min(0f)] [SerializeField] float slimeAttackSpeedPerLevel = 0.03f;

        [Header("Run Upgrades")]
        [Min(1)] [SerializeField] int runUpgradeMaxLevel = 20;
        [Min(1)] [SerializeField] int attackPowerBaseCost = 30;
        [Min(1)] [SerializeField] int attackSpeedBaseCost = 35;
        [Min(1)] [SerializeField] int coreRecoveryBaseCost = 25;
        [Min(1)] [SerializeField] int criticalChanceBaseCost = 45;
        [Min(1f)] [SerializeField] float runUpgradeCostGrowth = 1.35f;
        [Min(0f)] [SerializeField] float attackPowerPerLevel = 0.05f;
        [Min(0f)] [SerializeField] float attackSpeedPerLevel = 0.03f;
        [Range(0f, 0.25f)] [SerializeField] float criticalChancePerLevel = 0.015f;
        [Range(0f, 0.95f)] [SerializeField] float maxCriticalChance = 0.5f;
        [Min(1f)] [SerializeField] float criticalDamageMultiplier = 2f;
        [Min(1f)] [SerializeField] float coreRecoveryBaseAmount = 15f;
        [Min(0f)] [SerializeField] float coreRecoveryPerLevel = 5f;

        [Header("Summon Capacity")]
        [Min(1)] [SerializeField] int baseSummonCapacity = 9;
        [Min(1)] [SerializeField] int maxSummonCapacity = 12;
        [Min(1)] [SerializeField] int runSummonCapacityMaxLevel = 4;
        [Min(1)] [SerializeField] int runSummonCapacityBaseCost = 80;
        [Min(1f)] [SerializeField] float runSummonCapacityCostGrowth = 1.75f;
        [Min(1)] [SerializeField] int runSummonCapacityPerLevel = 1;

        public int SummonerMaxLevel => summonerMaxLevel;
        public int SlimeMaxLevel => slimeMaxLevel;
        public int RunUpgradeMaxLevel => runUpgradeMaxLevel;
        public float CriticalDamageMultiplier => criticalDamageMultiplier;
        public int BaseSummonCapacity => Mathf.Max(1, baseSummonCapacity);
        public int MaxSummonCapacity => Mathf.Max(BaseSummonCapacity, maxSummonCapacity);
        public int PermanentSummonCapacityMaxLevel => Mathf.Max(1, traitSummonCapacityMaxLevel);

        public int ExperienceToNextSummonerLevel(int level)
        {
            int clampedLevel = Mathf.Clamp(level, 1, summonerMaxLevel);
            if (clampedLevel >= summonerMaxLevel) return 0;
            double scaled = summonerBaseExperienceToNext *
                            Math.Pow(Math.Max(1f, summonerExperienceGrowth), clampedLevel - 1);
            return Mathf.Max(1, Mathf.RoundToInt((float)scaled));
        }

        public int SummonerExperienceReward(int rewardGold)
        {
            int scaledGold = Mathf.RoundToInt(Mathf.Max(0, rewardGold) * summonerExperiencePerRewardGold);
            return Mathf.Max(1, summonerBaseExperiencePerKill + scaledGold);
        }

        public float SummonerDamageMultiplier(int level) =>
            1f + Mathf.Max(0, Mathf.Min(level, summonerMaxLevel) - 1) * summonerDamagePerLevel;

        public float SummonerMaxHpMultiplier(int level) =>
            1f + Mathf.Max(0, Mathf.Min(level, summonerMaxLevel) - 1) * summonerMaxHpPerLevel;

        public float SummonerJackpotChanceBonus(int level) =>
            Mathf.Min(
                maxJackpotChanceBonus,
                Mathf.Max(0, Mathf.Min(level, summonerMaxLevel) - 1) * jackpotChancePerLevel);

        public float PermanentTraitValuePerLevel(PermanentTraitType type)
        {
            return type switch
            {
                PermanentTraitType.SummonerPower => traitSummonerDamagePerLevel,
                PermanentTraitType.SummonerHaste => traitSummonerAttackSpeedPerLevel,
                PermanentTraitType.CoreVitality => traitCoreMaxHpPerLevel,
                PermanentTraitType.SlimePower => traitSlimeDamagePerLevel,
                PermanentTraitType.SlimeHaste => traitSlimeAttackSpeedPerLevel,
                PermanentTraitType.LuckySummon => traitJackpotChancePerLevel,
                PermanentTraitType.SummonCapacity => traitSummonCapacityPerLevel,
                _ => 0f,
            };
        }

        public int PermanentSummonCapacityBonus(int level) =>
            Mathf.Clamp(level, 0, PermanentSummonCapacityMaxLevel) *
            Mathf.Max(1, traitSummonCapacityPerLevel);

        public float PermanentTraitMultiplier(PermanentTraitType type, int level) =>
            1f + Mathf.Max(0, level) * PermanentTraitValuePerLevel(type);

        public float PermanentTraitChanceBonus(PermanentTraitType type, int level) =>
            Mathf.Max(0, level) * PermanentTraitValuePerLevel(type);

        public int SlimeLevelUpCost(int currentLevel) =>
            GrowthCost(slimeBaseLevelUpCost, slimeLevelUpCostGrowth, Mathf.Max(1, currentLevel) - 1);

        public float SlimeDamageMultiplier(int level) =>
            1f + Mathf.Max(0, level - 1) * slimeDamagePerLevel;

        public float SlimeAttackSpeedMultiplier(int level) =>
            1f + Mathf.Max(0, level - 1) * slimeAttackSpeedPerLevel;

        public int RunUpgradeCost(RunUpgradeType type, int currentLevel) =>
            type == RunUpgradeType.SummonCapacity
                ? GrowthCost(runSummonCapacityBaseCost, runSummonCapacityCostGrowth, Mathf.Max(0, currentLevel))
                : GrowthCost(RunUpgradeBaseCost(type), runUpgradeCostGrowth, Mathf.Max(0, currentLevel));

        public int RunUpgradeMaxLevelFor(RunUpgradeType type) =>
            type == RunUpgradeType.SummonCapacity
                ? Mathf.Max(1, runSummonCapacityMaxLevel)
                : runUpgradeMaxLevel;

        public int RunSummonCapacityBonus(int level) =>
            Mathf.Clamp(level, 0, Mathf.Max(1, runSummonCapacityMaxLevel)) *
            Mathf.Max(1, runSummonCapacityPerLevel);

        public float RunAttackPowerMultiplier(int level) =>
            1f + Mathf.Max(0, level) * attackPowerPerLevel;

        public float RunAttackSpeedMultiplier(int level) =>
            1f + Mathf.Max(0, level) * attackSpeedPerLevel;

        public float RunCriticalChance(int level) =>
            Mathf.Min(maxCriticalChance, Mathf.Max(0, level) * criticalChancePerLevel);

        public float CoreRecoveryAmount(int purchasedLevel) =>
            coreRecoveryBaseAmount + Mathf.Max(0, purchasedLevel - 1) * coreRecoveryPerLevel;

        public static GrowthBalanceData CreateRuntimeDefault()
        {
            var data = CreateInstance<GrowthBalanceData>();
            data.hideFlags = HideFlags.HideAndDontSave;
            return data;
        }

        int RunUpgradeBaseCost(RunUpgradeType type)
        {
            return type switch
            {
                RunUpgradeType.AttackPower => attackPowerBaseCost,
                RunUpgradeType.AttackSpeed => attackSpeedBaseCost,
                RunUpgradeType.CoreRecovery => coreRecoveryBaseCost,
                RunUpgradeType.CriticalChance => criticalChanceBaseCost,
                RunUpgradeType.SummonCapacity => runSummonCapacityBaseCost,
                _ => attackPowerBaseCost,
            };
        }

        static int GrowthCost(int baseCost, float growth, int exponent)
        {
            double scaled = Math.Max(1, baseCost) * Math.Pow(Math.Max(1f, growth), Math.Max(0, exponent));
            return Mathf.Max(1, Mathf.RoundToInt((float)Math.Min(int.MaxValue, scaled)));
        }
    }
}
