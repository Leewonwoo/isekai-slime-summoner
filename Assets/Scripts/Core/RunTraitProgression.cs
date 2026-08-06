using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    [Serializable]
    public sealed class RunTraitLevelSaveData
    {
        public string rewardId;
        public int level;
    }

    [Serializable]
    public sealed class RunTraitProgressionSaveData
    {
        public int totalChoiceCount;
        public int clearedWave;
        public int attackArchetype;
        public List<RunTraitLevelSaveData> levels = new();
    }

    public readonly struct RunTraitChoice
    {
        public RunRewardDefinition Reward { get; }
        public string RewardId => Reward?.RewardId ?? string.Empty;
        public RunRewardCategory Category => Reward?.Category ?? RunRewardCategory.Summon;
        public string DisplayName => Reward?.DisplayName ?? "알 수 없는 보상";
        public string Description { get; }
        public string StatusLabel { get; }
        public int CurrentLevel { get; }
        public int NextLevel => CurrentLevel + 1;
        public bool IsImmediate => Reward?.IsImmediate ?? false;

        public RunTraitChoice(
            RunRewardDefinition reward,
            string description,
            string statusLabel,
            int currentLevel)
        {
            Reward = reward;
            Description = description ?? string.Empty;
            StatusLabel = statusLabel ?? string.Empty;
            CurrentLevel = Mathf.Max(0, currentLevel);
        }
    }

    public readonly struct RunTraitSnapshot
    {
        public bool IsChoicePending { get; }
        public int ClearedWave { get; }
        public int TotalChoiceCount { get; }
        public SummonerAttackArchetype AttackArchetype { get; }

        public RunTraitSnapshot(
            bool isChoicePending,
            int clearedWave,
            int totalChoiceCount,
            SummonerAttackArchetype attackArchetype)
        {
            IsChoicePending = isChoicePending;
            ClearedWave = clearedWave;
            TotalChoiceCount = totalChoiceCount;
            AttackArchetype = attackArchetype;
        }
    }

    public readonly struct SummonerRunAttackProfile
    {
        public SummonerAttackArchetype Archetype { get; }
        public MonsterAttribute Attribute { get; }
        public int ProjectileCount { get; }
        public float AdditionalProjectileDamageMultiplier { get; }
        public float AreaRadius { get; }
        public int PierceCount { get; }
        public float ChainDamageMultiplier { get; }
        public float SlowPercent { get; }
        public float SlowDuration { get; }
        public float DamageOverTime { get; }
        public float DamageOverTimeDuration { get; }
        public int EmpoweredShotInterval { get; }
        public float EmpoweredAreaRadius { get; }
        public float EmpoweredDamageMultiplier { get; }

        public SummonerRunAttackProfile(
            SummonerAttackArchetype archetype,
            MonsterAttribute attribute,
            int projectileCount,
            float additionalProjectileDamageMultiplier,
            float areaRadius,
            int pierceCount,
            float chainDamageMultiplier,
            float slowPercent,
            float slowDuration,
            float damageOverTime,
            float damageOverTimeDuration,
            int empoweredShotInterval,
            float empoweredAreaRadius,
            float empoweredDamageMultiplier)
        {
            Archetype = archetype;
            Attribute = attribute;
            ProjectileCount = Mathf.Max(1, projectileCount);
            AdditionalProjectileDamageMultiplier = Mathf.Clamp(additionalProjectileDamageMultiplier, 0.05f, 1f);
            AreaRadius = Mathf.Max(0f, areaRadius);
            PierceCount = Mathf.Max(1, pierceCount);
            ChainDamageMultiplier = Mathf.Clamp(chainDamageMultiplier, 0.05f, 1f);
            SlowPercent = Mathf.Clamp(slowPercent, 0f, 0.95f);
            SlowDuration = Mathf.Max(0f, slowDuration);
            DamageOverTime = Mathf.Max(0f, damageOverTime);
            DamageOverTimeDuration = Mathf.Max(0f, damageOverTimeDuration);
            EmpoweredShotInterval = Mathf.Max(0, empoweredShotInterval);
            EmpoweredAreaRadius = Mathf.Max(0f, empoweredAreaRadius);
            EmpoweredDamageMultiplier = Mathf.Max(1f, empoweredDamageMultiplier);
        }
    }

    /// <summary>RunRewardCatalog에서 5웨이브 3택을 생성하고 현재 런의 보상 레벨을 관리한다.</summary>
    public sealed class RunTraitProgression
    {
        readonly RunRewardCatalog _catalog;
        readonly int _randomSeed;
        readonly Dictionary<string, int> _levels = new();
        readonly List<RunTraitChoice> _currentChoices = new(3);
        bool _choicePending;
        int _clearedWave;
        int _totalChoiceCount;
        SummonerAttackArchetype _attackArchetype = SummonerAttackArchetype.EnergyBolt;

        public RunTraitSnapshot Snapshot => BuildSnapshot();
        public bool IsChoicePending => _choicePending;
        public int ClearedWave => _clearedWave;
        public int TotalChoiceCount => _totalChoiceCount;
        public SummonerAttackArchetype AttackArchetype => _attackArchetype;
        public RunRewardCatalog Catalog => _catalog;

        public float SlimeReviveFraction => ResolveLevelValue(
            RunRewardEffect.SlimeRevive, reward => reward.PrimaryValue +
                reward.SecondaryValue * Mathf.Max(0, GetLevel(reward.RewardId) - 1));
        public float SlimeShieldFraction => ResolveLevelValue(
            RunRewardEffect.SlimeShield, reward => reward.PrimaryValue +
                reward.SecondaryValue * Mathf.Max(0, GetLevel(reward.RewardId) - 1));
        public float MergeFrenzyDuration => ResolveLevelValue(
            RunRewardEffect.MergeFrenzy, reward => reward.PrimaryValue);
        public float MergeFrenzyAttackSpeedMultiplier => 1f + ResolveLevelValue(
            RunRewardEffect.MergeFrenzy, reward => reward.SecondaryValue * GetLevel(reward.RewardId));
        public float MergeFrenzyHealFraction => ResolveLevelValue(
            RunRewardEffect.MergeFrenzy,
            reward => reward.TertiaryValue * Mathf.Max(0, GetLevel(reward.RewardId) - 1));

        public event Action<RunTraitSnapshot> Changed;

        public RunTraitProgression(RunRewardCatalog catalog, int randomSeed)
        {
            _catalog = catalog != null ? catalog : RunRewardCatalog.CreateRuntimeDefault();
            _randomSeed = randomSeed;
        }

        public int GetLevel(string rewardId) =>
            !string.IsNullOrWhiteSpace(rewardId) && _levels.TryGetValue(rewardId, out int level)
                ? Mathf.Max(0, level)
                : 0;

        public int GetLevel(RunRewardEffect effect)
        {
            RunRewardDefinition reward = _catalog.Find(effect);
            return reward == null ? 0 : GetLevel(reward.RewardId);
        }

        public IReadOnlyList<RunRewardDefinition> GetAcquiredRewards()
        {
            var acquired = new List<RunRewardDefinition>();
            IReadOnlyList<RunRewardDefinition> rewards = _catalog.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward != null && !reward.IsImmediate && GetLevel(reward.RewardId) > 0)
                    acquired.Add(reward);
            }
            return acquired;
        }

        public RunTraitProgressionSaveData CaptureSaveData()
        {
            var data = new RunTraitProgressionSaveData
            {
                totalChoiceCount = Mathf.Max(0, _totalChoiceCount),
                clearedWave = Mathf.Max(0, _clearedWave),
                attackArchetype = (int)_attackArchetype,
            };
            foreach (var pair in _levels)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                    continue;
                data.levels.Add(new RunTraitLevelSaveData
                {
                    rewardId = pair.Key,
                    level = pair.Value,
                });
            }
            return data;
        }

        public void Restore(RunTraitProgressionSaveData data)
        {
            _levels.Clear();
            _currentChoices.Clear();
            _choicePending = false;
            _clearedWave = Mathf.Max(0, data?.clearedWave ?? 0);
            _totalChoiceCount = Mathf.Max(0, data?.totalChoiceCount ?? 0);
            _attackArchetype = data != null &&
                               Enum.IsDefined(typeof(SummonerAttackArchetype), data.attackArchetype)
                ? (SummonerAttackArchetype)data.attackArchetype
                : SummonerAttackArchetype.EnergyBolt;

            if (data?.levels == null)
                return;

            for (int i = 0; i < data.levels.Count; i++)
            {
                RunTraitLevelSaveData entry = data.levels[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.rewardId))
                    continue;
                RunRewardDefinition reward = _catalog.Find(entry.rewardId);
                if (reward == null || reward.IsImmediate)
                    continue;
                int level = Mathf.Clamp(entry.level, 0, reward.MaxLevel);
                if (level > 0)
                    _levels[reward.RewardId] = level;
            }
        }

        public bool BeginChoice(int clearedWave)
        {
            if (_choicePending || clearedWave <= 0)
                return false;

            _clearedWave = clearedWave;
            BuildCurrentChoices();
            if (_currentChoices.Count != 3)
            {
                Debug.LogError($"[CrossDefense] 런 보상 선택지 생성 실패: {_currentChoices.Count}/3");
                _currentChoices.Clear();
                return false;
            }

            _choicePending = true;
            Changed?.Invoke(BuildSnapshot());
            return true;
        }

        public IReadOnlyList<RunTraitChoice> GetCurrentChoices() =>
            _choicePending ? _currentChoices : Array.Empty<RunTraitChoice>();

        public bool TryChoose(string rewardId, out RunRewardDefinition selected)
        {
            selected = null;
            if (!_choicePending || string.IsNullOrWhiteSpace(rewardId))
                return false;

            for (int i = 0; i < _currentChoices.Count; i++)
            {
                if (_currentChoices[i].RewardId != rewardId) continue;
                selected = _currentChoices[i].Reward;
                break;
            }
            if (selected == null)
                return false;

            if (!selected.IsImmediate)
                _levels[selected.RewardId] = GetLevel(selected.RewardId) + 1;
            ApplyAwakening(selected.Effect);
            _totalChoiceCount++;
            _choicePending = false;
            _currentChoices.Clear();
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
                RunRewardEffect.AwakenFireball => "화염 속성 · 범위 폭발 · 연소",
                RunRewardEffect.AwakenIceLance => "빙결 속성 · 관통 · 둔화",
                RunRewardEffect.AwakenThunderSlash => "자연 속성 · 연쇄 뇌격",
                RunRewardEffect.Multicast => $"추가 투사체 +{reward.Count * level:N0}",
                RunRewardEffect.FireBurn =>
                    $"연소 DPS +{reward.PrimaryValue * level:0.#} · 지속 +{reward.SecondaryValue * level:0.#}초",
                RunRewardEffect.FireBurst =>
                    $"매 {Mathf.Max(2, reward.Count - Mathf.Max(0, level - 1)):N0}번째 공격 강화",
                RunRewardEffect.IcePierce => $"관통 대상 +{reward.Count * level:N0}",
                RunRewardEffect.IceFrost =>
                    $"둔화 +{reward.PrimaryValue * level * 100f:0.#}%p · 지속 +{reward.SecondaryValue * level:0.#}초",
                RunRewardEffect.ThunderChain => $"전이 대상 +{reward.Count * level:N0}",
                RunRewardEffect.ThunderOverload =>
                    $"매 {Mathf.Max(2, reward.Count - Mathf.Max(0, level - 1)):N0}번째 공격 과부하",
                RunRewardEffect.SlimeRevive => $"웨이브당 1회 · HP {SlimeReviveFraction * 100f:0.#}% 부활",
                RunRewardEffect.MergeFrenzy =>
                    $"{MergeFrenzyDuration:0.#}초간 슬라임 공속 +{(MergeFrenzyAttackSpeedMultiplier - 1f) * 100f:0.#}%",
                RunRewardEffect.SlimeShield => $"웨이브 시작 보호막 {SlimeShieldFraction * 100f:0.#}%",
                _ => reward.Description,
            };
        }

        public SummonerRunAttackProfile BuildAttackProfile(SkillData baseSkill = null)
        {
            MonsterAttribute attribute = baseSkill?.Attribute ?? MonsterAttribute.None;
            int projectileCount = baseSkill?.ProjectileCount ?? 1;
            float additionalDamage =
                baseSkill?.AdditionalProjectileDamageMultiplier ?? 0.65f;
            float areaRadius = baseSkill?.AreaRadius ?? 0f;
            int pierceCount = baseSkill?.PierceCount ?? 1;
            float chainDamage = baseSkill?.ChainDamageMultiplier ?? 1f;
            float slowPercent = baseSkill?.SlowPercent ?? 0f;
            float slowDuration = baseSkill?.SlowDuration ?? 0f;
            float dotDamage = baseSkill?.DamageOverTime ?? 0f;
            float dotDuration = baseSkill?.DamageOverTimeDuration ?? 0f;
            int empoweredInterval = 0;
            float empoweredArea = 0f;
            float empoweredDamage = 1f;

            RunRewardDefinition awakening;
            switch (_attackArchetype)
            {
                case SummonerAttackArchetype.Fireball:
                    awakening = _catalog.Find(RunRewardEffect.AwakenFireball);
                    attribute = baseSkill?.Attribute ?? MonsterAttribute.Fire;
                    areaRadius = awakening?.PrimaryValue ?? areaRadius;
                    dotDamage = awakening?.SecondaryValue ?? dotDamage;
                    dotDuration = awakening?.TertiaryValue ?? dotDuration;
                    RunRewardDefinition burn = _catalog.Find(RunRewardEffect.FireBurn);
                    int burnLevel = GetLevel(RunRewardEffect.FireBurn);
                    if (burn != null && burnLevel > 0)
                    {
                        dotDamage += burn.PrimaryValue * burnLevel;
                        dotDuration += burn.SecondaryValue * burnLevel;
                    }
                    ApplyEmpoweredShot(RunRewardEffect.FireBurst, ref empoweredInterval,
                        ref empoweredArea, ref empoweredDamage);
                    break;
                case SummonerAttackArchetype.IceLance:
                    awakening = _catalog.Find(RunRewardEffect.AwakenIceLance);
                    attribute = baseSkill?.Attribute ?? MonsterAttribute.Ice;
                    slowPercent = awakening?.PrimaryValue ?? slowPercent;
                    slowDuration = awakening?.SecondaryValue ?? slowDuration;
                    pierceCount = Mathf.Max(1, awakening?.Count ?? pierceCount);
                    RunRewardDefinition pierce = _catalog.Find(RunRewardEffect.IcePierce);
                    pierceCount += (pierce?.Count ?? 0) * GetLevel(RunRewardEffect.IcePierce);
                    RunRewardDefinition frost = _catalog.Find(RunRewardEffect.IceFrost);
                    int frostLevel = GetLevel(RunRewardEffect.IceFrost);
                    if (frost != null && frostLevel > 0)
                    {
                        slowPercent += frost.PrimaryValue * frostLevel;
                        slowDuration += frost.SecondaryValue * frostLevel;
                    }
                    break;
                case SummonerAttackArchetype.ThunderSlash:
                    awakening = _catalog.Find(RunRewardEffect.AwakenThunderSlash);
                    attribute = baseSkill?.Attribute ?? MonsterAttribute.Lightning;
                    pierceCount = awakening != null
                        ? 1 + Mathf.Max(0, awakening.Count)
                        : pierceCount;
                    chainDamage = awakening?.PrimaryValue ?? chainDamage;
                    RunRewardDefinition chain = _catalog.Find(RunRewardEffect.ThunderChain);
                    int chainLevel = GetLevel(RunRewardEffect.ThunderChain);
                    if (chain != null && chainLevel > 0)
                    {
                        pierceCount += chain.Count * chainLevel;
                        chainDamage = Mathf.Min(0.9f, chainDamage + chain.PrimaryValue * chainLevel);
                    }
                    ApplyEmpoweredShot(RunRewardEffect.ThunderOverload, ref empoweredInterval,
                        ref empoweredArea, ref empoweredDamage);
                    break;
            }

            RunRewardDefinition multicast = _catalog.Find(RunRewardEffect.Multicast);
            int multicastLevel = GetLevel(RunRewardEffect.Multicast);
            if (multicast != null && multicastLevel > 0)
            {
                projectileCount += multicast.Count * multicastLevel;
                additionalDamage = multicast.PrimaryValue;
            }

            return new SummonerRunAttackProfile(
                _attackArchetype,
                attribute,
                projectileCount,
                additionalDamage,
                areaRadius,
                pierceCount,
                chainDamage,
                slowPercent,
                slowDuration,
                dotDamage,
                dotDuration,
                empoweredInterval,
                empoweredArea,
                empoweredDamage);
        }

        void BuildCurrentChoices()
        {
            _currentChoices.Clear();
            if (_attackArchetype == SummonerAttackArchetype.EnergyBolt)
            {
                AddAllAwakenings();
                return;
            }

            var random = new System.Random(unchecked(
                _randomSeed + _clearedWave * 3571 + _totalChoiceCount * 7919));
            AddWeightedChoice(RunRewardCategory.SummonerEvolution, random);
            AddWeightedChoice(RunRewardCategory.SlimeArmy, random);
            AddWeightedChoice(RunRewardCategory.Summon, random);

            if (_currentChoices.Count >= 3)
                return;
            var fallback = BuildEligible(null);
            while (_currentChoices.Count < 3 && fallback.Count > 0)
            {
                int index = random.Next(fallback.Count);
                AddChoice(fallback[index]);
                fallback.RemoveAt(index);
            }
        }

        void AddAllAwakenings()
        {
            IReadOnlyList<RunRewardDefinition> rewards = _catalog.Rewards;
            for (int i = 0; i < rewards.Count && _currentChoices.Count < 3; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward?.Category == RunRewardCategory.Awakening &&
                    reward.Trigger == RunRewardTrigger.Milestone)
                    AddChoice(reward);
            }
        }

        void AddWeightedChoice(RunRewardCategory category, System.Random random)
        {
            List<RunRewardDefinition> eligible = BuildEligible(category);
            if (eligible.Count == 0)
                return;

            int totalWeight = 0;
            for (int i = 0; i < eligible.Count; i++)
                totalWeight += eligible[i].Weight;
            int roll = random.Next(Mathf.Max(1, totalWeight));
            for (int i = 0; i < eligible.Count; i++)
            {
                roll -= eligible[i].Weight;
                if (roll >= 0) continue;
                AddChoice(eligible[i]);
                return;
            }
            AddChoice(eligible[eligible.Count - 1]);
        }

        List<RunRewardDefinition> BuildEligible(RunRewardCategory? category)
        {
            var eligible = new List<RunRewardDefinition>();
            IReadOnlyList<RunRewardDefinition> rewards = _catalog.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition reward = rewards[i];
                if (reward == null || reward.Trigger != RunRewardTrigger.Milestone ||
                    reward.Category == RunRewardCategory.Awakening)
                    continue;
                if (category.HasValue && reward.Category != category.Value)
                    continue;
                if (!reward.SupportsAttack(_attackArchetype) || reward.MinimumSelection > _totalChoiceCount)
                    continue;
                if (!reward.IsImmediate && GetLevel(reward.RewardId) >= reward.MaxLevel)
                    continue;
                if (HasChoice(reward.RewardId) || ConflictsWithAcquired(reward))
                    continue;
                eligible.Add(reward);
            }
            return eligible;
        }

        bool ConflictsWithAcquired(RunRewardDefinition reward)
        {
            if (string.IsNullOrWhiteSpace(reward.ExclusiveGroup))
                return false;
            IReadOnlyList<RunRewardDefinition> rewards = _catalog.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                RunRewardDefinition acquired = rewards[i];
                if (acquired == null || acquired.RewardId == reward.RewardId ||
                    acquired.ExclusiveGroup != reward.ExclusiveGroup)
                    continue;
                if (GetLevel(acquired.RewardId) > 0)
                    return true;
            }
            return false;
        }

        bool HasChoice(string rewardId)
        {
            for (int i = 0; i < _currentChoices.Count; i++)
                if (_currentChoices[i].RewardId == rewardId)
                    return true;
            return false;
        }

        void AddChoice(RunRewardDefinition reward)
        {
            if (reward == null || HasChoice(reward.RewardId))
                return;
            int currentLevel = GetLevel(reward.RewardId);
            string status = reward.IsImmediate
                ? "즉시 보상"
                : currentLevel == 0
                    ? "NEW"
                    : $"Lv.{currentLevel:N0} → Lv.{currentLevel + 1:N0}";
            _currentChoices.Add(new RunTraitChoice(
                reward,
                reward.Description,
                status,
                currentLevel));
        }

        void ApplyAwakening(RunRewardEffect effect)
        {
            _attackArchetype = effect switch
            {
                RunRewardEffect.AwakenFireball => SummonerAttackArchetype.Fireball,
                RunRewardEffect.AwakenIceLance => SummonerAttackArchetype.IceLance,
                RunRewardEffect.AwakenThunderSlash => SummonerAttackArchetype.ThunderSlash,
                _ => _attackArchetype,
            };
        }

        void ApplyEmpoweredShot(
            RunRewardEffect effect,
            ref int interval,
            ref float radius,
            ref float damageMultiplier)
        {
            RunRewardDefinition reward = _catalog.Find(effect);
            int level = GetLevel(effect);
            if (reward == null || level <= 0)
                return;
            interval = Mathf.Max(2, reward.Count - Mathf.Max(0, level - 1));
            radius = reward.PrimaryValue;
            damageMultiplier = reward.SecondaryValue;
        }

        float ResolveLevelValue(RunRewardEffect effect, Func<RunRewardDefinition, float> selector)
        {
            RunRewardDefinition reward = _catalog.Find(effect);
            return reward == null || GetLevel(reward.RewardId) <= 0
                ? 0f
                : Mathf.Max(0f, selector(reward));
        }

        RunTraitSnapshot BuildSnapshot() =>
            new(_choicePending, _clearedWave, _totalChoiceCount, _attackArchetype);
    }
}
