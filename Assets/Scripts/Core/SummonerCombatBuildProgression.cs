using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct SummonerCombatBuildSnapshot
    {
        public int SummonerLevel { get; }
        public int PendingChoiceCount { get; }
        public bool IsChoicePending { get; }

        public SummonerCombatBuildSnapshot(
            int summonerLevel,
            int pendingChoiceCount,
            bool isChoicePending)
        {
            SummonerLevel = Mathf.Max(1, summonerLevel);
            PendingChoiceCount = Mathf.Max(0, pendingChoiceCount);
            IsChoicePending = isChoicePending;
        }
    }

    public readonly struct SummonerCombatBuildProfile
    {
        public int AdditionalProjectileCount { get; }
        public float AttackSpeedMultiplier { get; }
        public int SpreadProjectileCount { get; }
        public float SpreadDamageMultiplier { get; }
        public int AdditionalPierceCount { get; }
        public float PierceRetainedDamageMultiplier { get; }
        public int SplitProjectileCount { get; }
        public float SplitDamageMultiplier { get; }
        public int RicochetCount { get; }
        public float RicochetDamageMultiplier { get; }
        public float AfterimageChance { get; }
        public float AfterimageDamageMultiplier { get; }
        public float CriticalBurstRadius { get; }
        public float CriticalBurstDamageMultiplier { get; }
        public float SlimeResonanceChance { get; }
        public float SlimeResonanceDamageMultiplier { get; }
        public int OverdriveAttackInterval { get; }
        public float OverdriveDuration { get; }
        public float OverdriveAttackSpeedMultiplier { get; }
        public int OverdriveProjectileBonus { get; }

        public SummonerCombatBuildProfile(
            int additionalProjectileCount,
            float attackSpeedMultiplier,
            int spreadProjectileCount,
            float spreadDamageMultiplier,
            int additionalPierceCount,
            float pierceRetainedDamageMultiplier,
            int splitProjectileCount,
            float splitDamageMultiplier,
            int ricochetCount,
            float ricochetDamageMultiplier,
            float afterimageChance,
            float afterimageDamageMultiplier,
            float criticalBurstRadius,
            float criticalBurstDamageMultiplier,
            float slimeResonanceChance,
            float slimeResonanceDamageMultiplier,
            int overdriveAttackInterval,
            float overdriveDuration,
            float overdriveAttackSpeedMultiplier,
            int overdriveProjectileBonus)
        {
            AdditionalProjectileCount = Mathf.Max(0, additionalProjectileCount);
            AttackSpeedMultiplier = Mathf.Max(1f, attackSpeedMultiplier);
            SpreadProjectileCount = Mathf.Max(0, spreadProjectileCount);
            SpreadDamageMultiplier = Mathf.Clamp(spreadDamageMultiplier, 0.05f, 1f);
            AdditionalPierceCount = Mathf.Max(0, additionalPierceCount);
            PierceRetainedDamageMultiplier = Mathf.Clamp(pierceRetainedDamageMultiplier, 0.05f, 1f);
            SplitProjectileCount = Mathf.Max(0, splitProjectileCount);
            SplitDamageMultiplier = Mathf.Clamp(splitDamageMultiplier, 0.05f, 1f);
            RicochetCount = Mathf.Max(0, ricochetCount);
            RicochetDamageMultiplier = Mathf.Clamp(ricochetDamageMultiplier, 0.05f, 1f);
            AfterimageChance = Mathf.Clamp01(afterimageChance);
            AfterimageDamageMultiplier = Mathf.Clamp(afterimageDamageMultiplier, 0.05f, 1f);
            CriticalBurstRadius = Mathf.Max(0f, criticalBurstRadius);
            CriticalBurstDamageMultiplier = Mathf.Clamp(criticalBurstDamageMultiplier, 0.05f, 1f);
            SlimeResonanceChance = Mathf.Clamp01(slimeResonanceChance);
            SlimeResonanceDamageMultiplier = Mathf.Clamp(slimeResonanceDamageMultiplier, 0.05f, 1f);
            OverdriveAttackInterval = Mathf.Max(0, overdriveAttackInterval);
            OverdriveDuration = Mathf.Max(0f, overdriveDuration);
            OverdriveAttackSpeedMultiplier = Mathf.Max(1f, overdriveAttackSpeedMultiplier);
            OverdriveProjectileBonus = Mathf.Max(0, overdriveProjectileBonus);
        }

        public static SummonerCombatBuildProfile Default => new(
            0, 1f, 0, 0.45f, 0, 0.85f, 0, 0.45f, 0, 0.8f,
            0f, 0.6f, 0f, 0.4f, 0f, 0.5f, 0, 0f, 1f, 0);
    }

    /// <summary>소환사 레벨업으로 획득하며 도전마다 초기화되는 투사체 빌드 3택을 관리한다.</summary>
    public sealed class SummonerCombatBuildProgression
    {
        readonly RunRewardCatalog _catalog;
        readonly int _randomSeed;
        readonly Dictionary<string, int> _levels = new();
        readonly List<RunTraitChoice> _currentChoices = new(3);
        int _summonerLevel;
        int _pendingChoiceCount;
        int _choiceSequence;
        bool _choicePending;

        public int SummonerLevel => _summonerLevel;
        public int PendingChoiceCount => _pendingChoiceCount;
        public bool IsChoicePending => _choicePending;
        public SummonerCombatBuildSnapshot Snapshot => BuildSnapshot();

        public event Action<SummonerCombatBuildSnapshot> Changed;

        public SummonerCombatBuildProgression(
            RunRewardCatalog catalog,
            int randomSeed,
            int initialSummonerLevel = 1)
        {
            _catalog = catalog != null ? catalog : RunRewardCatalog.CreateRuntimeDefault();
            _randomSeed = randomSeed;
            _summonerLevel = Mathf.Max(1, initialSummonerLevel);
        }

        public int GetLevel(string rewardId) =>
            !string.IsNullOrWhiteSpace(rewardId) && _levels.TryGetValue(rewardId, out int level)
                ? Mathf.Max(0, level)
                : 0;

        public int GetLevel(RunRewardEffect effect)
        {
            IReadOnlyList<RunRewardDefinition> rewards =
                _catalog.GetRewards(RunRewardTrigger.SummonerLevel);
            for (int i = 0; i < rewards.Count; i++)
                if (rewards[i].Effect == effect)
                    return GetLevel(rewards[i].RewardId);
            return 0;
        }

        public IReadOnlyList<RunRewardDefinition> GetAcquiredRewards()
        {
            var acquired = new List<RunRewardDefinition>();
            IReadOnlyList<RunRewardDefinition> rewards =
                _catalog.GetRewards(RunRewardTrigger.SummonerLevel);
            for (int i = 0; i < rewards.Count; i++)
                if (GetLevel(rewards[i].RewardId) > 0)
                    acquired.Add(rewards[i]);
            return acquired;
        }

        public void NotifySummonerLevelChanged(int previousLevel, int currentLevel)
        {
            previousLevel = Mathf.Max(1, previousLevel);
            currentLevel = Mathf.Max(previousLevel, currentLevel);
            _summonerLevel = currentLevel;
            int gainedLevels = currentLevel - previousLevel;
            if (gainedLevels <= 0)
                return;

            _pendingChoiceCount += gainedLevels;
            if (!_choicePending && _pendingChoiceCount > 0)
                BuildNextChoice();
            Changed?.Invoke(BuildSnapshot());
        }

        public IReadOnlyList<RunTraitChoice> GetCurrentChoices() =>
            _choicePending ? _currentChoices : Array.Empty<RunTraitChoice>();

        public bool TryChoose(string rewardId)
        {
            if (!_choicePending || string.IsNullOrWhiteSpace(rewardId))
                return false;

            RunRewardDefinition selected = null;
            for (int i = 0; i < _currentChoices.Count; i++)
            {
                if (_currentChoices[i].RewardId != rewardId) continue;
                selected = _currentChoices[i].Reward;
                break;
            }
            if (selected == null)
                return false;

            _levels[selected.RewardId] = GetLevel(selected.RewardId) + 1;
            _pendingChoiceCount = Mathf.Max(0, _pendingChoiceCount - 1);
            _choiceSequence++;
            _choicePending = false;
            _currentChoices.Clear();
            if (_pendingChoiceCount > 0)
                BuildNextChoice();
            Changed?.Invoke(BuildSnapshot());
            return true;
        }

        public string GetCurrentEffect(RunRewardDefinition reward)
        {
            if (reward == null)
                return string.Empty;
            int level = GetLevel(reward.RewardId);
            return reward.Effect switch
            {
                RunRewardEffect.Multicast => $"연속 투사체 +{reward.Count * level:N0}",
                RunRewardEffect.RapidCast =>
                    $"기본 공격 간격 -{reward.PrimaryValue * level * 100f:0.#}%",
                RunRewardEffect.ManaSpread =>
                    $"부채꼴 보조탄 +{reward.Count * level:N0} · 피해 {reward.PrimaryValue * 100f:0.#}%",
                RunRewardEffect.PierceEngraving => $"추가 관통 +{reward.Count * level:N0}",
                RunRewardEffect.ManaSplit =>
                    $"처치 시 자식탄 {reward.Count:N0}발 · 피해 {reward.PrimaryValue * 100f:0.#}%",
                RunRewardEffect.Ricochet =>
                    $"반사 횟수 +{reward.Count * level:N0} · 유지 피해 {reward.PrimaryValue * 100f:0.#}%",
                RunRewardEffect.Afterimage =>
                    $"복제 확률 {(reward.PrimaryValue + reward.SecondaryValue * Mathf.Max(0, level - 1)) * 100f:0.#}%",
                RunRewardEffect.CriticalBurst =>
                    $"치명타 폭발 반경 {reward.PrimaryValue:0.#} · 피해 {reward.SecondaryValue * 100f:0.#}%",
                RunRewardEffect.SlimeResonance =>
                    $"공명탄 확률 {(reward.PrimaryValue + reward.SecondaryValue * Mathf.Max(0, level - 1)) * 100f:0.#}%",
                RunRewardEffect.ManaOverdrive =>
                    $"{reward.Count:N0}회 공격마다 {reward.SecondaryValue:0.#}초 과부하",
                _ => reward.Description,
            };
        }

        public SummonerCombatBuildProfile BuildProfile()
        {
            RunRewardDefinition multicast = Find(RunRewardEffect.Multicast);
            RunRewardDefinition rapid = Find(RunRewardEffect.RapidCast);
            RunRewardDefinition spread = Find(RunRewardEffect.ManaSpread);
            RunRewardDefinition pierce = Find(RunRewardEffect.PierceEngraving);
            RunRewardDefinition split = Find(RunRewardEffect.ManaSplit);
            RunRewardDefinition ricochet = Find(RunRewardEffect.Ricochet);
            RunRewardDefinition afterimage = Find(RunRewardEffect.Afterimage);
            RunRewardDefinition critical = Find(RunRewardEffect.CriticalBurst);
            RunRewardDefinition resonance = Find(RunRewardEffect.SlimeResonance);
            RunRewardDefinition overdrive = Find(RunRewardEffect.ManaOverdrive);

            int multicastLevel = LevelOf(multicast);
            int rapidLevel = LevelOf(rapid);
            int spreadLevel = LevelOf(spread);
            int pierceLevel = LevelOf(pierce);
            int splitLevel = LevelOf(split);
            int ricochetLevel = LevelOf(ricochet);
            int afterimageLevel = LevelOf(afterimage);
            int criticalLevel = LevelOf(critical);
            int resonanceLevel = LevelOf(resonance);
            int overdriveLevel = LevelOf(overdrive);

            float attackIntervalFactor = Mathf.Max(
                0.6f,
                1f - (rapid?.PrimaryValue ?? 0f) * rapidLevel);
            return new SummonerCombatBuildProfile(
                (multicast?.Count ?? 0) * multicastLevel,
                1f / attackIntervalFactor,
                (spread?.Count ?? 0) * spreadLevel,
                spread?.PrimaryValue ?? 0.45f,
                (pierce?.Count ?? 0) * pierceLevel,
                pierce?.PrimaryValue ?? 0.85f,
                splitLevel > 0 ? split?.Count ?? 0 : 0,
                split?.PrimaryValue ?? 0.45f,
                (ricochet?.Count ?? 0) * ricochetLevel,
                ricochet?.PrimaryValue ?? 0.8f,
                afterimageLevel > 0
                    ? (afterimage?.PrimaryValue ?? 0f) +
                      (afterimage?.SecondaryValue ?? 0f) * (afterimageLevel - 1)
                    : 0f,
                afterimage?.TertiaryValue ?? 0.6f,
                criticalLevel > 0 ? critical?.PrimaryValue ?? 0f : 0f,
                critical?.SecondaryValue ?? 0.4f,
                resonanceLevel > 0
                    ? (resonance?.PrimaryValue ?? 0f) +
                      (resonance?.SecondaryValue ?? 0f) * (resonanceLevel - 1)
                    : 0f,
                resonance?.TertiaryValue ?? 0.5f,
                overdriveLevel > 0 ? overdrive?.Count ?? 0 : 0,
                overdrive?.SecondaryValue ?? 0f,
                1f + (overdrive?.PrimaryValue ?? 0f) * overdriveLevel,
                overdriveLevel);
        }

        void BuildNextChoice()
        {
            _currentChoices.Clear();
            List<RunRewardDefinition> eligible = BuildEligible();
            if (eligible.Count < 3)
            {
                Debug.LogError($"[CrossDefense] 소환사 레벨업 빌드 선택지 생성 실패: {eligible.Count}/3");
                return;
            }

            var random = new System.Random(unchecked(
                _randomSeed + _summonerLevel * 3571 + _choiceSequence * 7919));
            if (_choiceSequence < 2)
            {
                var starters = eligible.FindAll(reward =>
                    reward.Effect is RunRewardEffect.Multicast or
                        RunRewardEffect.RapidCast or
                        RunRewardEffect.ManaSpread);
                if (starters.Count > 0)
                {
                    RunRewardDefinition starter = starters[random.Next(starters.Count)];
                    AddChoice(starter);
                    eligible.Remove(starter);
                }
            }

            while (_currentChoices.Count < 3 && eligible.Count > 0)
            {
                int index = WeightedIndex(eligible, random);
                RunRewardDefinition reward = eligible[index];
                AddChoice(reward);
                eligible.RemoveAt(index);
            }
            _choicePending = _currentChoices.Count == 3;
        }

        List<RunRewardDefinition> BuildEligible()
        {
            var eligible = new List<RunRewardDefinition>();
            IReadOnlyList<RunRewardDefinition> rewards =
                _catalog.GetRewards(RunRewardTrigger.SummonerLevel);
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward == null || reward.Trigger != RunRewardTrigger.SummonerLevel)
                    continue;
                if (GetLevel(reward.RewardId) >= reward.MaxLevel)
                    continue;
                eligible.Add(reward);
            }
            return eligible;
        }

        void AddChoice(RunRewardDefinition reward)
        {
            int currentLevel = GetLevel(reward.RewardId);
            _currentChoices.Add(new RunTraitChoice(
                reward,
                reward.Description,
                currentLevel == 0 ? "NEW" : $"Lv.{currentLevel:N0} → Lv.{currentLevel + 1:N0}",
                currentLevel));
        }

        RunRewardDefinition Find(RunRewardEffect effect)
        {
            IReadOnlyList<RunRewardDefinition> rewards =
                _catalog.GetRewards(RunRewardTrigger.SummonerLevel);
            for (int i = 0; i < rewards.Count; i++)
                if (rewards[i].Effect == effect)
                    return rewards[i];
            return null;
        }

        int LevelOf(RunRewardDefinition reward) =>
            reward == null ? 0 : GetLevel(reward.RewardId);

        static int WeightedIndex(IReadOnlyList<RunRewardDefinition> rewards, System.Random random)
        {
            int totalWeight = 0;
            for (int i = 0; i < rewards.Count; i++)
                totalWeight += rewards[i].Weight;
            int roll = random.Next(Mathf.Max(1, totalWeight));
            for (int i = 0; i < rewards.Count; i++)
            {
                roll -= rewards[i].Weight;
                if (roll < 0)
                    return i;
            }
            return rewards.Count - 1;
        }

        SummonerCombatBuildSnapshot BuildSnapshot() => new(
            _summonerLevel,
            _pendingChoiceCount,
            _choicePending);
    }
}
