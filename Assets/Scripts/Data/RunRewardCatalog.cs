using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum RunRewardCategory
    {
        Awakening,
        SummonerEvolution,
        SlimeArmy,
        Summon,
    }

    public enum RunRewardEffect
    {
        AwakenFireball,
        AwakenIceLance,
        AwakenThunderSlash,
        Multicast,
        FireBurn,
        FireBurst,
        IcePierce,
        IceFrost,
        ThunderChain,
        ThunderOverload,
        SlimeRevive,
        MergeFrenzy,
        SlimeShield,
        TripleSummon,
        MergeSupport,
        JackpotEgg,
    }

    public enum SummonerAttackArchetype
    {
        None,
        EnergyBolt,
        Fireball,
        IceLance,
        ThunderSlash,
    }

    [Serializable]
    public sealed class RunRewardDefinition
    {
        [SerializeField] string rewardId = "reward-id";
        [SerializeField] string displayName = "보상";
        [TextArea(2, 4)] [SerializeField] string description = "보상 설명";
        [SerializeField] RunRewardCategory category;
        [SerializeField] RunRewardEffect effect;
        [SerializeField] SummonerAttackArchetype requiredAttack;
        [SerializeField] Sprite icon;
        [Min(1)] [SerializeField] int maxLevel = 1;
        [Min(1)] [SerializeField] int weight = 100;
        [Min(0)] [SerializeField] int minimumSelection;
        [SerializeField] string exclusiveGroup;
        [SerializeField] float primaryValue;
        [SerializeField] float secondaryValue;
        [SerializeField] float tertiaryValue;
        [SerializeField] int count;

        public string RewardId => rewardId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? RewardId : displayName;
        public string Description => description ?? string.Empty;
        public RunRewardCategory Category => category;
        public RunRewardEffect Effect => effect;
        public SummonerAttackArchetype RequiredAttack => requiredAttack;
        public Sprite Icon => icon;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public int Weight => Mathf.Max(1, weight);
        public int MinimumSelection => Mathf.Max(0, minimumSelection);
        public string ExclusiveGroup => exclusiveGroup ?? string.Empty;
        public float PrimaryValue => primaryValue;
        public float SecondaryValue => secondaryValue;
        public float TertiaryValue => tertiaryValue;
        public int Count => count;
        public bool IsImmediate => category == RunRewardCategory.Summon;

        public bool SupportsAttack(SummonerAttackArchetype attack) =>
            requiredAttack == SummonerAttackArchetype.None || requiredAttack == attack;

        internal static RunRewardDefinition Create(
            string id,
            string name,
            string description,
            RunRewardCategory category,
            RunRewardEffect effect,
            SummonerAttackArchetype requiredAttack = SummonerAttackArchetype.None,
            int maxLevel = 1,
            int weight = 100,
            int minimumSelection = 0,
            string exclusiveGroup = "",
            float primaryValue = 0f,
            float secondaryValue = 0f,
            float tertiaryValue = 0f,
            int count = 0)
        {
            return new RunRewardDefinition
            {
                rewardId = id,
                displayName = name,
                description = description,
                category = category,
                effect = effect,
                requiredAttack = requiredAttack,
                maxLevel = maxLevel,
                weight = weight,
                minimumSelection = minimumSelection,
                exclusiveGroup = exclusiveGroup,
                primaryValue = primaryValue,
                secondaryValue = secondaryValue,
                tertiaryValue = tertiaryValue,
                count = count,
            };
        }
    }

    /// <summary>5웨이브 3택의 표시 정보, 호환 조건과 효과 수치를 한 에셋에서 관리한다.</summary>
    [CreateAssetMenu(fileName = "RunRewardCatalog", menuName = "Isekai Slime Summoner/Data/Run Reward Catalog", order = 31)]
    public sealed class RunRewardCatalog : ScriptableObject
    {
        [SerializeField] List<RunRewardDefinition> rewards = new();

        public IReadOnlyList<RunRewardDefinition> Rewards => rewards;

        public RunRewardDefinition Find(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
                return null;
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward != null && reward.RewardId == rewardId)
                    return reward;
            }
            return null;
        }

        public RunRewardDefinition Find(RunRewardEffect effect)
        {
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward != null && reward.Effect == effect)
                    return reward;
            }
            return null;
        }

        public IEnumerable<string> Validate()
        {
            if (rewards == null || rewards.Count == 0)
            {
                yield return "Run reward catalog has no rewards.";
                yield break;
            }

            var ids = new HashSet<string>();
            int awakeningCount = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward == null)
                {
                    yield return $"Reward {i + 1} is null.";
                    continue;
                }
                if (string.IsNullOrWhiteSpace(reward.RewardId))
                    yield return $"Reward {i + 1} has no ID.";
                else if (!ids.Add(reward.RewardId))
                    yield return $"Duplicate reward ID: {reward.RewardId}";
                if (reward.Category == RunRewardCategory.Awakening)
                    awakeningCount++;
            }

            if (awakeningCount != 3)
                yield return $"Exactly three awakening rewards are required, but found {awakeningCount}.";
        }

        public static RunRewardCatalog CreateRuntimeDefault()
        {
            var catalog = CreateInstance<RunRewardCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.rewards = CreateDefaultRewards();
            return catalog;
        }

        internal static List<RunRewardDefinition> CreateDefaultRewards()
        {
            return new List<RunRewardDefinition>
            {
                RunRewardDefinition.Create(
                    "awaken-fireball", "화염구",
                    "자동공격이 범위 피해와 연소를 주는 화염구로 바뀌고, 클릭 지점에서 화염이 폭발합니다.",
                    RunRewardCategory.Awakening, RunRewardEffect.AwakenFireball,
                    primaryValue: 0.95f, secondaryValue: 2f, tertiaryValue: 3f),
                RunRewardDefinition.Create(
                    "awaken-ice-lance", "빙결창",
                    "자동공격이 적을 관통하고 둔화시키는 빙결창으로 바뀌며, 클릭 방향으로 얼음창을 발사합니다.",
                    RunRewardCategory.Awakening, RunRewardEffect.AwakenIceLance,
                    primaryValue: 0.3f, secondaryValue: 2f, count: 3),
                RunRewardDefinition.Create(
                    "awaken-thunder-slash", "뇌격참",
                    "자동공격이 자연 속성 뇌격으로 바뀌어 주변 적에게 전이되고, 클릭 지점에 낙뢰를 내립니다.",
                    RunRewardCategory.Awakening, RunRewardEffect.AwakenThunderSlash,
                    primaryValue: 0.65f, count: 2),
                RunRewardDefinition.Create(
                    "summoner-multicast", "마력 분열",
                    "공격할 때 투사체를 짧은 간격으로 연속 발사합니다. 주변 적을 우선 추적하고 대상이 부족하면 같은 적을 공격합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.Multicast,
                    maxLevel: 2, primaryValue: 0.65f, count: 1),
                RunRewardDefinition.Create(
                    "fire-burning-core", "타오르는 핵",
                    "화염구의 연소 피해와 지속시간이 증가합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.FireBurn,
                    SummonerAttackArchetype.Fireball, maxLevel: 2,
                    primaryValue: 1.5f, secondaryValue: 0.5f),
                RunRewardDefinition.Create(
                    "fire-great-burst", "대폭발",
                    "일정 횟수마다 더 크고 강한 화염구를 발사합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.FireBurst,
                    SummonerAttackArchetype.Fireball, maxLevel: 2,
                    primaryValue: 1.55f, secondaryValue: 1.25f, count: 5),
                RunRewardDefinition.Create(
                    "ice-absolute-pierce", "절대 관통",
                    "빙결창이 더 많은 적을 관통합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.IcePierce,
                    SummonerAttackArchetype.IceLance, maxLevel: 2, count: 2),
                RunRewardDefinition.Create(
                    "ice-deep-frost", "혹한",
                    "빙결창의 둔화율과 지속시간이 증가합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.IceFrost,
                    SummonerAttackArchetype.IceLance, maxLevel: 2,
                    primaryValue: 0.1f, secondaryValue: 0.5f),
                RunRewardDefinition.Create(
                    "thunder-chain", "연쇄 뇌격",
                    "뇌격이 더 많은 적에게 전이되고 전이 피해가 증가합니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.ThunderChain,
                    SummonerAttackArchetype.ThunderSlash, maxLevel: 2,
                    primaryValue: 0.05f, count: 2),
                RunRewardDefinition.Create(
                    "thunder-overload", "과부하",
                    "일정 횟수마다 명중 지점에 강한 자연 속성 폭발을 일으킵니다.",
                    RunRewardCategory.SummonerEvolution, RunRewardEffect.ThunderOverload,
                    SummonerAttackArchetype.ThunderSlash, maxLevel: 2,
                    primaryValue: 1.3f, secondaryValue: 1.2f, count: 5),
                RunRewardDefinition.Create(
                    "slime-revive", "끈질긴 젤리",
                    "각 슬라임이 웨이브마다 한 번 쓰러진 자리에서 부활합니다.",
                    RunRewardCategory.SlimeArmy, RunRewardEffect.SlimeRevive,
                    maxLevel: 2, primaryValue: 0.35f, secondaryValue: 0.2f),
                RunRewardDefinition.Create(
                    "slime-merge-frenzy", "합체 폭주",
                    "머지 성공 후 일정 시간 모든 슬라임의 공격속도가 증가합니다.",
                    RunRewardCategory.SlimeArmy, RunRewardEffect.MergeFrenzy,
                    maxLevel: 2, primaryValue: 10f, secondaryValue: 0.3f, tertiaryValue: 0.1f),
                RunRewardDefinition.Create(
                    "slime-shield", "보호 젤",
                    "웨이브 시작 시 모든 슬라임이 최대 HP 비례 보호막을 얻습니다.",
                    RunRewardCategory.SlimeArmy, RunRewardEffect.SlimeShield,
                    maxLevel: 2, primaryValue: 0.2f, secondaryValue: 0.15f),
                RunRewardDefinition.Create(
                    "summon-triple", "꽝 없는 3연 소환",
                    "계약서를 쓰지 않고 재화 꽝 없는 슬라임 소환을 3회 진행합니다.",
                    RunRewardCategory.Summon, RunRewardEffect.TripleSummon, count: 3),
                RunRewardDefinition.Create(
                    "summon-merge-support", "합성 지원",
                    "보유 중인 슬라임과 같은 종류·성급을 1마리 지급합니다.",
                    RunRewardCategory.Summon, RunRewardEffect.MergeSupport, count: 1),
                RunRewardDefinition.Create(
                    "summon-jackpot-egg", "대박 슬라임 알",
                    "★1 70% / ★2 28% / ★3 2% 확률로 슬라임 한 마리를 지급합니다.",
                    RunRewardCategory.Summon, RunRewardEffect.JackpotEgg,
                    primaryValue: 0.28f, secondaryValue: 0.02f, count: 1),
            };
        }
    }
}
