using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct SummonerProgressionSnapshot
    {
        public int Level { get; }
        public int Experience { get; }
        public int ExperienceToNext { get; }
        public int MaxLevel { get; }
        public float DamageMultiplier { get; }
        public float NextDamageMultiplier { get; }
        public float MaxHpMultiplier { get; }
        public float NextMaxHpMultiplier { get; }
        public float JackpotChanceBonus { get; }
        public float NextJackpotChanceBonus { get; }

        public bool IsMaxLevel => Level >= MaxLevel;
        public float ExperienceProgress =>
            IsMaxLevel ? 1f : Mathf.Clamp01(ExperienceToNext > 0 ? Experience / (float)ExperienceToNext : 0f);

        public SummonerProgressionSnapshot(
            int level,
            int experience,
            int experienceToNext,
            int maxLevel,
            float damageMultiplier,
            float nextDamageMultiplier,
            float maxHpMultiplier,
            float nextMaxHpMultiplier,
            float jackpotChanceBonus,
            float nextJackpotChanceBonus)
        {
            Level = level;
            Experience = experience;
            ExperienceToNext = experienceToNext;
            MaxLevel = maxLevel;
            DamageMultiplier = damageMultiplier;
            NextDamageMultiplier = nextDamageMultiplier;
            MaxHpMultiplier = maxHpMultiplier;
            NextMaxHpMultiplier = nextMaxHpMultiplier;
            JackpotChanceBonus = jackpotChanceBonus;
            NextJackpotChanceBonus = nextJackpotChanceBonus;
        }
    }

    [Serializable]
    public sealed class SummonerProgressionSaveData
    {
        public int version = 1;
        public int level = 1;
        public int experience;
    }

    /// <summary>런을 넘어 유지되는 소환사 EXP와 자동 레벨업을 관리한다.</summary>
    public sealed class SummonerProgression
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.SummonerProgression.v1";

        readonly GrowthBalanceData _balance;
        readonly Action<string> _saveJson;
        readonly Action _flush;
        int _level = 1;
        int _experience;

        public SummonerProgressionSnapshot Snapshot => BuildSnapshot();

        public event Action<SummonerProgressionSnapshot> Changed;

        public SummonerProgression(
            GrowthBalanceData balance,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flush = null)
        {
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _saveJson = saveJson;
            _flush = flush;
            Load(loadJson?.Invoke());
        }

        public static SummonerProgression CreatePersistent(
            GrowthBalanceData balance,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new SummonerProgression(
                balance,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json),
                PlayerPrefs.Save);
        }

        public void AddExperience(int amount)
        {
            if (amount <= 0 || _level >= _balance.SummonerMaxLevel) return;
            _experience = Mathf.Max(0, _experience + amount);
            bool leveledUp = ApplyAvailableLevelUps();
            PersistAndNotify(leveledUp);
        }

        public void Flush()
        {
            _saveJson?.Invoke(ToJson());
            _flush?.Invoke();
        }

        public string ToJson() =>
            JsonUtility.ToJson(new SummonerProgressionSaveData
            {
                level = _level,
                experience = _experience,
            });

        void Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            try
            {
                var data = JsonUtility.FromJson<SummonerProgressionSaveData>(json);
                if (data == null || data.version != 1) return;
                _level = Mathf.Clamp(data.level, 1, _balance.SummonerMaxLevel);
                _experience = _level >= _balance.SummonerMaxLevel
                    ? 0
                    : Mathf.Max(0, data.experience);
                if (ApplyAvailableLevelUps())
                {
                    _saveJson?.Invoke(ToJson());
                    _flush?.Invoke();
                }
            }
            catch (ArgumentException)
            {
                _level = 1;
                _experience = 0;
            }
        }

        bool ApplyAvailableLevelUps()
        {
            bool leveledUp = false;
            while (_level < _balance.SummonerMaxLevel)
            {
                int required = _balance.ExperienceToNextSummonerLevel(_level);
                if (required <= 0 || _experience < required)
                    break;
                _experience -= required;
                _level++;
                leveledUp = true;
            }

            if (_level >= _balance.SummonerMaxLevel)
                _experience = 0;
            return leveledUp;
        }

        void PersistAndNotify(bool flush)
        {
            _saveJson?.Invoke(ToJson());
            if (flush)
                _flush?.Invoke();
            Changed?.Invoke(BuildSnapshot());
        }

        SummonerProgressionSnapshot BuildSnapshot()
        {
            int nextLevel = Mathf.Min(_balance.SummonerMaxLevel, _level + 1);
            return new SummonerProgressionSnapshot(
                _level,
                _experience,
                _balance.ExperienceToNextSummonerLevel(_level),
                _balance.SummonerMaxLevel,
                _balance.SummonerDamageMultiplier(_level),
                _balance.SummonerDamageMultiplier(nextLevel),
                _balance.SummonerMaxHpMultiplier(_level),
                _balance.SummonerMaxHpMultiplier(nextLevel),
                _balance.SummonerJackpotChanceBonus(_level),
                _balance.SummonerJackpotChanceBonus(nextLevel));
        }
    }
}
