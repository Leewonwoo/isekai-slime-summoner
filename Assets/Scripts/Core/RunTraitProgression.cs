using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct RunTraitChoice
    {
        public RunTraitType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int CurrentLevel { get; }
        public int NextLevel => CurrentLevel + 1;

        public RunTraitChoice(
            RunTraitType type,
            string displayName,
            string description,
            int currentLevel)
        {
            Type = type;
            DisplayName = displayName;
            Description = description;
            CurrentLevel = Mathf.Max(0, currentLevel);
        }
    }

    public readonly struct RunTraitSnapshot
    {
        public bool IsChoicePending { get; }
        public int ClearedWave { get; }
        public int TotalChoiceCount { get; }
        public float AllDamageMultiplier { get; }
        public float AllAttackSpeedMultiplier { get; }
        public float SummonerDamageMultiplier { get; }
        public float SlimeDamageMultiplier { get; }
        public float CoreMaxHpMultiplier { get; }
        public float CriticalChanceBonus { get; }

        public RunTraitSnapshot(
            bool isChoicePending,
            int clearedWave,
            int totalChoiceCount,
            float allDamageMultiplier,
            float allAttackSpeedMultiplier,
            float summonerDamageMultiplier,
            float slimeDamageMultiplier,
            float coreMaxHpMultiplier,
            float criticalChanceBonus)
        {
            IsChoicePending = isChoicePending;
            ClearedWave = clearedWave;
            TotalChoiceCount = totalChoiceCount;
            AllDamageMultiplier = allDamageMultiplier;
            AllAttackSpeedMultiplier = allAttackSpeedMultiplier;
            SummonerDamageMultiplier = summonerDamageMultiplier;
            SlimeDamageMultiplier = slimeDamageMultiplier;
            CoreMaxHpMultiplier = coreMaxHpMultiplier;
            CriticalChanceBonus = criticalChanceBonus;
        }
    }

    /// <summary>5웨이브 클리어마다 선택하고 현재 런에서만 유지되는 로그라이크 특성.</summary>
    public sealed class RunTraitProgression
    {
        static readonly RunTraitType[] AllTypes =
        {
            RunTraitType.AllDamage,
            RunTraitType.AllAttackSpeed,
            RunTraitType.SummonerPower,
            RunTraitType.SlimePower,
            RunTraitType.CoreVitality,
            RunTraitType.CriticalFocus,
        };

        readonly GrowthBalanceData _balance;
        readonly int _randomSeed;
        readonly Dictionary<RunTraitType, int> _levels = new();
        bool _choicePending;
        int _clearedWave;

        public RunTraitSnapshot Snapshot => BuildSnapshot();
        public bool IsChoicePending => _choicePending;
        public int ClearedWave => _clearedWave;
        public int TotalChoiceCount
        {
            get
            {
                int total = 0;
                foreach (int level in _levels.Values)
                    total += Mathf.Max(0, level);
                return total;
            }
        }

        public event Action<RunTraitSnapshot> Changed;

        public RunTraitProgression(GrowthBalanceData balance, int randomSeed)
        {
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _randomSeed = randomSeed;
        }

        public int GetLevel(RunTraitType type) =>
            _levels.TryGetValue(type, out int level) ? Mathf.Max(0, level) : 0;

        public bool BeginChoice(int clearedWave)
        {
            if (_choicePending || clearedWave <= 0)
                return false;
            _choicePending = true;
            _clearedWave = clearedWave;
            Changed?.Invoke(BuildSnapshot());
            return true;
        }

        public IReadOnlyList<RunTraitChoice> GetCurrentChoices()
        {
            if (!_choicePending)
                return Array.Empty<RunTraitChoice>();

            var shuffled = (RunTraitType[])AllTypes.Clone();
            var random = new System.Random(unchecked(
                _randomSeed + _clearedWave * 3571 + TotalChoiceCount * 7919));
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
            }

            var choices = new RunTraitChoice[3];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = BuildChoice(shuffled[i]);
            return choices;
        }

        public bool TryChoose(RunTraitType type)
        {
            if (!_choicePending)
                return false;

            IReadOnlyList<RunTraitChoice> choices = GetCurrentChoices();
            bool offered = false;
            for (int i = 0; i < choices.Count; i++)
            {
                if (choices[i].Type != type) continue;
                offered = true;
                break;
            }
            if (!offered)
                return false;

            _levels[type] = GetLevel(type) + 1;
            _choicePending = false;
            Changed?.Invoke(BuildSnapshot());
            return true;
        }

        public string GetDisplayName(RunTraitType type)
        {
            return type switch
            {
                RunTraitType.AllDamage => "전투 본능",
                RunTraitType.AllAttackSpeed => "광전사의 박자",
                RunTraitType.SummonerPower => "소환사의 집중",
                RunTraitType.SlimePower => "용병단 돌격",
                RunTraitType.CoreVitality => "불굴의 핵",
                RunTraitType.CriticalFocus => "치명적 약점",
                _ => "알 수 없는 특성",
            };
        }

        public string GetCurrentEffect(RunTraitType type)
        {
            int level = GetLevel(type);
            float total = _balance.RunTraitValuePerLevel(type) * Mathf.Max(0, level) * 100f;
            return type == RunTraitType.CriticalFocus
                ? $"치명타 확률 +{total:0.#}%p"
                : $"{EffectTarget(type)} +{total:0.#}%";
        }

        RunTraitChoice BuildChoice(RunTraitType type)
        {
            int currentLevel = GetLevel(type);
            float increase = _balance.RunTraitValuePerLevel(type) * 100f;
            string increaseText = type == RunTraitType.CriticalFocus
                ? $"+{increase:0.#}%p"
                : $"+{increase:0.#}%";
            return new RunTraitChoice(
                type,
                GetDisplayName(type),
                $"{EffectTarget(type)} {increaseText} · 선택 후 Lv.{currentLevel + 1:N0}",
                currentLevel);
        }

        static string EffectTarget(RunTraitType type)
        {
            return type switch
            {
                RunTraitType.AllDamage => "전체 공격력",
                RunTraitType.AllAttackSpeed => "전체 공격속도",
                RunTraitType.SummonerPower => "소환사 공격력",
                RunTraitType.SlimePower => "모든 슬라임 공격력",
                RunTraitType.CoreVitality => "소환사 최대 HP",
                RunTraitType.CriticalFocus => "치명타 확률",
                _ => "효과",
            };
        }

        RunTraitSnapshot BuildSnapshot()
        {
            return new RunTraitSnapshot(
                _choicePending,
                _clearedWave,
                TotalChoiceCount,
                _balance.RunTraitMultiplier(RunTraitType.AllDamage, GetLevel(RunTraitType.AllDamage)),
                _balance.RunTraitMultiplier(
                    RunTraitType.AllAttackSpeed,
                    GetLevel(RunTraitType.AllAttackSpeed)),
                _balance.RunTraitMultiplier(
                    RunTraitType.SummonerPower,
                    GetLevel(RunTraitType.SummonerPower)),
                _balance.RunTraitMultiplier(RunTraitType.SlimePower, GetLevel(RunTraitType.SlimePower)),
                _balance.RunTraitMultiplier(RunTraitType.CoreVitality, GetLevel(RunTraitType.CoreVitality)),
                _balance.RunTraitChanceBonus(
                    RunTraitType.CriticalFocus,
                    GetLevel(RunTraitType.CriticalFocus)));
        }
    }
}
