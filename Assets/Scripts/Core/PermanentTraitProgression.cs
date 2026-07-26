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
        public int SummonCapacityBonus { get; }

        public PermanentTraitSnapshot(
            int totalChoiceCount,
            int pendingChoiceCount,
            float summonerDamageMultiplier,
            float summonerAttackSpeedMultiplier,
            float coreMaxHpMultiplier,
            float slimeDamageMultiplier,
            float slimeAttackSpeedMultiplier,
            float jackpotChanceBonus,
            int summonCapacityBonus)
        {
            TotalChoiceCount = totalChoiceCount;
            PendingChoiceCount = pendingChoiceCount;
            SummonerDamageMultiplier = summonerDamageMultiplier;
            SummonerAttackSpeedMultiplier = summonerAttackSpeedMultiplier;
            CoreMaxHpMultiplier = coreMaxHpMultiplier;
            SlimeDamageMultiplier = slimeDamageMultiplier;
            SlimeAttackSpeedMultiplier = slimeAttackSpeedMultiplier;
            JackpotChanceBonus = jackpotChanceBonus;
            SummonCapacityBonus = Mathf.Max(0, summonCapacityBonus);
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
            PermanentTraitType.SummonCapacity,
            PermanentTraitType.EquipmentSupply,
            PermanentTraitType.RelicDiscovery,
        };

        static readonly PermanentTraitType[][] ChoiceGroups =
        {
            new[]
            {
                PermanentTraitType.SummonerPower,
                PermanentTraitType.SummonerHaste,
            },
            new[]
            {
                PermanentTraitType.SlimePower,
                PermanentTraitType.SlimeHaste,
                PermanentTraitType.SummonCapacity,
            },
            new[]
            {
                PermanentTraitType.CoreVitality,
                PermanentTraitType.LuckySummon,
                PermanentTraitType.EquipmentSupply,
                PermanentTraitType.RelicDiscovery,
            },
        };

        readonly GrowthBalanceData _balance;
        readonly Func<int> _summonerLevelProvider;
        readonly Action<string> _saveJson;
        readonly Action _flush;
        readonly Func<PermanentTraitType, bool> _availabilityProvider;
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

        public int CurrentEntitlement =>
            Mathf.Max(0, ((_summonerLevelProvider?.Invoke() ?? 1) - 1) / 2);

        public event Action<PermanentTraitSnapshot> Changed;

        public PermanentTraitProgression(
            GrowthBalanceData balance,
            Func<int> summonerLevelProvider,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null,
            Func<PermanentTraitType, bool> availabilityProvider = null)
        {
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _summonerLevelProvider = summonerLevelProvider;
            _saveJson = saveJson;
            _flush = flush;
            _availabilityProvider = availabilityProvider;
            Load(loadJson?.Invoke());
        }

        public static PermanentTraitProgression CreatePersistent(
            GrowthBalanceData balance,
            Func<int> summonerLevelProvider,
            string playerPrefsKey = DefaultPlayerPrefsKey,
            Func<PermanentTraitType, bool> availabilityProvider = null)
        {
            string safeKey = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new PermanentTraitProgression(
                balance,
                summonerLevelProvider,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json),
                PlayerPrefs.Save,
                availabilityProvider);
        }

        public int GetLevel(PermanentTraitType type) =>
            _levels.TryGetValue(type, out int level) ? Mathf.Max(0, level) : 0;

        public IReadOnlyList<PermanentTraitChoice> GetCurrentChoices()
        {
            if (PendingChoiceCount <= 0)
                return Array.Empty<PermanentTraitChoice>();

            var offeredTypes = new List<PermanentTraitType>(ChoiceGroups.Length);
            var random = new System.Random(unchecked(0x5F3759DF + TotalChoiceCount * 7919));
            for (int groupIndex = 0; groupIndex < ChoiceGroups.Length; groupIndex++)
            {
                PermanentTraitType[] group = ChoiceGroups[groupIndex];
                var available = new List<PermanentTraitType>(group.Length);
                for (int i = 0; i < group.Length; i++)
                {
                    PermanentTraitType type = group[i];
                    if (GetLevel(type) < MaxLevel(type) && IsAvailable(type))
                        available.Add(type);
                }
                if (available.Count > 0)
                    offeredTypes.Add(available[random.Next(available.Count)]);
            }

            if (offeredTypes.Count < 3)
            {
                var fallback = new List<PermanentTraitType>(AllTypes.Length);
                for (int i = 0; i < AllTypes.Length; i++)
                {
                    PermanentTraitType type = AllTypes[i];
                    if (GetLevel(type) < MaxLevel(type) &&
                        IsAvailable(type) && !offeredTypes.Contains(type))
                        fallback.Add(type);
                }
                while (offeredTypes.Count < 3 && fallback.Count > 0)
                {
                    int index = random.Next(fallback.Count);
                    offeredTypes.Add(fallback[index]);
                    fallback.RemoveAt(index);
                }
            }
            if (offeredTypes.Count < 3)
                return Array.Empty<PermanentTraitChoice>();

            var choices = new PermanentTraitChoice[3];
            for (int i = 0; i < choices.Length; i++)
                choices[i] = BuildChoice(offeredTypes[i]);
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

        public int MaxLevel(PermanentTraitType type) =>
            type == PermanentTraitType.SummonCapacity
                ? _balance.PermanentSummonCapacityMaxLevel
                : type == PermanentTraitType.EquipmentSupply ||
                  type == PermanentTraitType.RelicDiscovery
                    ? 100
                : 10;

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
                PermanentTraitType.SummonCapacity => "지휘 확장",
                PermanentTraitType.EquipmentSupply => "장비 보급",
                PermanentTraitType.RelicDiscovery => "신물 발견",
                _ => "알 수 없는 특성",
            };
        }

        public string GetCurrentEffect(PermanentTraitType type)
        {
            int level = GetLevel(type);
            if (type == PermanentTraitType.EquipmentSupply)
                return $"영구 장비 획득 {level:N0}회";
            if (type == PermanentTraitType.RelicDiscovery)
                return $"신물 획득·승급 {level:N0}회";
            if (type == PermanentTraitType.SummonCapacity)
                return $"슬라임 슬롯 +{_balance.PermanentSummonCapacityBonus(level):N0}";
            float total = _balance.PermanentTraitValuePerLevel(type) * Mathf.Max(0, level) * 100f;
            return type == PermanentTraitType.LuckySummon
                ? $"★2 직행 +{total:0.#}%p"
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
            if (type == PermanentTraitType.EquipmentSupply)
                return new PermanentTraitChoice(
                    type,
                    GetDisplayName(type),
                    "미보유 장비 1개를 영구 획득합니다.",
                    currentLevel);
            if (type == PermanentTraitType.RelicDiscovery)
                return new PermanentTraitChoice(
                    type,
                    GetDisplayName(type),
                    "속성 신물을 획득하거나 보유 신물을 1단계 영구 승급합니다.",
                    currentLevel);
            if (type == PermanentTraitType.SummonCapacity)
            {
                int slotIncrease = _balance.PermanentSummonCapacityBonus(currentLevel + 1) -
                                   _balance.PermanentSummonCapacityBonus(currentLevel);
                return new PermanentTraitChoice(
                    type,
                    GetDisplayName(type),
                    $"슬라임 슬롯 +{slotIncrease:N0} · 선택 시 Lv.{currentLevel + 1:N0}",
                    currentLevel);
            }
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
                PermanentTraitType.LuckySummon => "★2 직행 확률",
                PermanentTraitType.SummonCapacity => "슬라임 슬롯",
                _ => "효과",
            };
        }

        bool IsAvailable(PermanentTraitType type)
        {
            if (type != PermanentTraitType.EquipmentSupply &&
                type != PermanentTraitType.RelicDiscovery)
                return true;
            return _availabilityProvider?.Invoke(type) ?? false;
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
                    GetLevel(PermanentTraitType.LuckySummon)),
                _balance.PermanentSummonCapacityBonus(
                    GetLevel(PermanentTraitType.SummonCapacity)));
        }
    }
}
