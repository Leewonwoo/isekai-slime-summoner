using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct PermanentTraitChoice
    {
        public PermanentTraitType Type { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int CurrentLevel { get; }
        public int NextLevel => CurrentLevel + 1;

        public PermanentTraitChoice(
            PermanentTraitType type,
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

    public readonly struct PermanentTraitSnapshot
    {
        public int TotalChoiceCount { get; }
        public int PendingChoiceCount { get; }
        public float SummonerDamageMultiplier { get; }
        public float SummonerAttackSpeedMultiplier { get; }
        public float CoreMaxHpMultiplier { get; }
        public float SlimeDamageMultiplier { get; }
        public float SlimeAttackSpeedMultiplier { get; }
        public float JackpotChanceBonus { get; }

        public PermanentTraitSnapshot(
            int totalChoiceCount,
            int pendingChoiceCount,
            float summonerDamageMultiplier,
            float summonerAttackSpeedMultiplier,
            float coreMaxHpMultiplier,
            float slimeDamageMultiplier,
            float slimeAttackSpeedMultiplier,
            float jackpotChanceBonus)
        {
            TotalChoiceCount = totalChoiceCount;
            PendingChoiceCount = pendingChoiceCount;
            SummonerDamageMultiplier = summonerDamageMultiplier;
            SummonerAttackSpeedMultiplier = summonerAttackSpeedMultiplier;
            CoreMaxHpMultiplier = coreMaxHpMultiplier;
            SlimeDamageMultiplier = slimeDamageMultiplier;
            SlimeAttackSpeedMultiplier = slimeAttackSpeedMultiplier;
            JackpotChanceBonus = jackpotChanceBonus;
        }
    }

    [Serializable]
    public sealed class PermanentTraitLevelSaveData
    {
        public int type;
        public int level;
    }

    [Serializable]
    public sealed class PermanentTraitSaveData
    {
        public int version = 1;
        public List<PermanentTraitLevelSaveData> traits = new();
    }

    /// <summary>
    /// 소환사 레벨업으로 얻는 3택 특성의 영구 레벨과 미선택 권리를 관리한다.
    /// </summary>
    public sealed class PermanentTraitProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.PermanentTraits.v1";

        static readonly PermanentTraitType[] AllTypes =
        {
            PermanentTraitType.SummonerPower,
            PermanentTraitType.SummonerHaste,
            PermanentTraitType.CoreVitality,
            PermanentTraitType.SlimePower,
            PermanentTraitType.SlimeHaste,
            PermanentTraitType.LuckySummon,
        };

        readonly GrowthBalanceData _balance;
        readonly Func<int> _summonerLevelProvider;
        readonly Action<string> _saveJson;
        readonly Action _flush;
        readonly Dictionary<PermanentTraitType, int> _levels = new();

        public PermanentTraitSnapshot Snapshot => BuildSnapshot();
        public int PendingChoiceCount => Mathf.Max(0, CurrentEntitlement - TotalChoiceCount);
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

        int CurrentEntitlement => Mathf.Max(0, (_summonerLevelProvider?.Invoke() ?? 1) - 1);

        public event Action<PermanentTraitSnapshot> Changed;

        public PermanentTraitProgression(
            GrowthBalanceData balance,
            Func<int> summonerLevelProvider,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _summonerLevelProvider = summonerLevelProvider;
            _saveJson = saveJson;
            _flush = flush;
            Load(loadJson?.Invoke());
        }

        public static PermanentTraitProgression CreatePersistent(
            GrowthBalanceData balance,
            Func<int> summonerLevelProvider,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new PermanentTraitProgression(
                balance,
                summonerLevelProvider,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json),
                PlayerPrefs.Save);
        }

        public int GetLevel(PermanentTraitType type) =>
            _levels.TryGetValue(type, out int level) ? Mathf.Max(0, level) : 0;

        public IReadOnlyList<PermanentTraitChoice> GetCurrentChoices()
        {
            if (PendingChoiceCount <= 0)
                return Array.Empty<PermanentTraitChoice>();

            var shuffled = (PermanentTraitType[])AllTypes.Clone();
            var random = new System.Random(unchecked(0x5F3759DF + TotalChoiceCount * 7919));
            for (int i = shuffled.Length - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (shuffled[i], shuffled[swapIndex]) = (shuffled[swapIndex], shuffled[i]);
            }

            var choices = new PermanentTraitChoice[3];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = BuildChoice(shuffled[i]);
            return choices;
        }

        public bool TryChoose(PermanentTraitType type)
        {
            if (PendingChoiceCount <= 0)
                return false;

            IReadOnlyList<PermanentTraitChoice> choices = GetCurrentChoices();
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
            Persist(true);
            Changed?.Invoke(BuildSnapshot());
            return true;
        }

        public string GetDisplayName(PermanentTraitType type)
        {
            return type switch
            {
                PermanentTraitType.SummonerPower => "집중 사격",
                PermanentTraitType.SummonerHaste => "속사 훈련",
                PermanentTraitType.CoreVitality => "강인한 생명력",
                PermanentTraitType.SlimePower => "용병단 화력",
                PermanentTraitType.SlimeHaste => "용병단 기민함",
                PermanentTraitType.LuckySummon => "계약의 행운",
                _ => "알 수 없는 특성",
            };
        }

        public string GetCurrentEffect(PermanentTraitType type)
        {
            int level = GetLevel(type);
            float total = _balance.PermanentTraitValuePerLevel(type) * Mathf.Max(0, level) * 100f;
            return type == PermanentTraitType.LuckySummon
                ? $"★1 직행 +{total:0.#}%p"
                : $"{EffectTarget(type)} +{total:0.#}%";
        }

        public void Flush()
        {
            _saveJson?.Invoke(ToJson());
            _flush?.Invoke();
        }

        public string ToJson()
        {
            var data = new PermanentTraitSaveData();
            foreach (PermanentTraitType type in AllTypes)
            {
                int level = GetLevel(type);
                if (level <= 0) continue;
                data.traits.Add(new PermanentTraitLevelSaveData
                {
                    type = (int)type,
                    level = level,
                });
            }
            return JsonUtility.ToJson(data);
        }

        PermanentTraitChoice BuildChoice(PermanentTraitType type)
        {
            int currentLevel = GetLevel(type);
            float increase = _balance.PermanentTraitValuePerLevel(type) * 100f;
            string increaseText = type == PermanentTraitType.LuckySummon
                ? $"+{increase:0.#}%p"
                : $"+{increase:0.#}%";
            return new PermanentTraitChoice(
                type,
                GetDisplayName(type),
                $"{EffectTarget(type)} {increaseText} · 선택 후 Lv.{currentLevel + 1:N0}",
                currentLevel);
        }

        static string EffectTarget(PermanentTraitType type)
        {
            return type switch
            {
                PermanentTraitType.SummonerPower => "소환사 공격력",
                PermanentTraitType.SummonerHaste => "소환사 공격속도",
                PermanentTraitType.CoreVitality => "소환사 최대 HP",
                PermanentTraitType.SlimePower => "모든 슬라임 공격력",
                PermanentTraitType.SlimeHaste => "모든 슬라임 공격속도",
                PermanentTraitType.LuckySummon => "★1 직행 확률",
                _ => "효과",
            };
        }

        void Load(string json)
        {
            _levels.Clear();
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var data = JsonUtility.FromJson<PermanentTraitSaveData>(json);
                if (data == null || data.version != 1 || data.traits == null)
                    return;

                for (int i = 0; i < data.traits.Count; i++)
                {
                    PermanentTraitLevelSaveData entry = data.traits[i];
                    if (entry == null || entry.level <= 0 ||
                        !Enum.IsDefined(typeof(PermanentTraitType), entry.type))
                        continue;
                    var type = (PermanentTraitType)entry.type;
                    _levels[type] = GetLevel(type) + entry.level;
                }
            }
            catch (ArgumentException)
            {
                _levels.Clear();
            }
        }

        void Persist(bool flush)
        {
            _saveJson?.Invoke(ToJson());
            if (flush)
                _flush?.Invoke();
        }

        PermanentTraitSnapshot BuildSnapshot()
        {
            return new PermanentTraitSnapshot(
                TotalChoiceCount,
                PendingChoiceCount,
                _balance.PermanentTraitMultiplier(
                    PermanentTraitType.SummonerPower,
                    GetLevel(PermanentTraitType.SummonerPower)),
                _balance.PermanentTraitMultiplier(
                    PermanentTraitType.SummonerHaste,
                    GetLevel(PermanentTraitType.SummonerHaste)),
                _balance.PermanentTraitMultiplier(
                    PermanentTraitType.CoreVitality,
                    GetLevel(PermanentTraitType.CoreVitality)),
                _balance.PermanentTraitMultiplier(
                    PermanentTraitType.SlimePower,
                    GetLevel(PermanentTraitType.SlimePower)),
                _balance.PermanentTraitMultiplier(
                    PermanentTraitType.SlimeHaste,
                    GetLevel(PermanentTraitType.SlimeHaste)),
                _balance.PermanentTraitChanceBonus(
                    PermanentTraitType.LuckySummon,
                    GetLevel(PermanentTraitType.LuckySummon)));
        }
    }
}
