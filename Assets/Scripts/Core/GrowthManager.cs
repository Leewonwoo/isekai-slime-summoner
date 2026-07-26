using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>런 전용 전체 강화와 unitId별 즉시 슬라임 레벨업을 관리한다.</summary>
    [Serializable]
    public sealed class GrowthProgressionSaveData
    {
        public int version = 1;
        public List<RunUpgradeLevelSaveData> runUpgrades = new();
        public List<SlimeUpgradeLevelSaveData> slimeUpgrades = new();
    }

    [Serializable]
    public sealed class RunUpgradeLevelSaveData
    {
        public int type;
        public int level;
    }

    [Serializable]
    public sealed class SlimeUpgradeLevelSaveData
    {
        public string unitId;
        public int level;
    }

    public sealed class GrowthManager
    {
        public const string DefaultPlayerPrefsKey = "CrossDefense.Growth.v1";

        readonly GameManager _gameManager;
        readonly SummonManager _summonManager;
        readonly GrowthBalanceData _balance;
        readonly int[] _runUpgradeLevels = new int[Enum.GetValues(typeof(RunUpgradeType)).Length];
        readonly System.Random _random;
        readonly Action<string> _saveJson;
        readonly Action _flushSaves;

        public GrowthBalanceData Balance => _balance;
        public float RunDamageMultiplier =>
            _balance.RunAttackPowerMultiplier(GetRunUpgradeLevel(RunUpgradeType.AttackPower));
        public float RunAttackSpeedMultiplier =>
            _balance.RunAttackSpeedMultiplier(GetRunUpgradeLevel(RunUpgradeType.AttackSpeed));
        public float CriticalChance =>
            _balance.RunCriticalChance(GetRunUpgradeLevel(RunUpgradeType.CriticalChance));
        public int SummonCapacityBonus =>
            _balance.RunSummonCapacityBonus(GetRunUpgradeLevel(RunUpgradeType.SummonCapacity));

        public event Action Changed;

        public GrowthManager(
            GameManager gameManager,
            SummonManager summonManager,
            GrowthBalanceData balance,
            int randomSeed = 0,
            Func<string> loadJson = null,
            Action<string> saveJson = null,
            Action flushSaves = null)
        {
            _gameManager = gameManager;
            _summonManager = summonManager;
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
            _saveJson = saveJson;
            _flushSaves = flushSaves;
            Load(loadJson?.Invoke());
        }

        public static GrowthManager CreatePersistent(
            GameManager gameManager,
            SummonManager summonManager,
            GrowthBalanceData balance,
            int randomSeed = 0,
            string playerPrefsKey = DefaultPlayerPrefsKey)
        {
            string safeKey = string.IsNullOrWhiteSpace(playerPrefsKey)
                ? DefaultPlayerPrefsKey
                : playerPrefsKey;
            return new GrowthManager(
                gameManager,
                summonManager,
                balance,
                randomSeed,
                () => PlayerPrefs.GetString(safeKey, string.Empty),
                json => PlayerPrefs.SetString(safeKey, json),
                PlayerPrefs.Save);
        }

        public int GetRunUpgradeLevel(RunUpgradeType type) =>
            _runUpgradeLevels[Mathf.Clamp((int)type, 0, _runUpgradeLevels.Length - 1)];

        public int GetRunUpgradeCost(RunUpgradeType type) =>
            _balance.RunUpgradeCost(type, GetRunUpgradeLevel(type));

        public bool IsRunUpgradeMaxed(RunUpgradeType type)
        {
            if (GetRunUpgradeLevel(type) >= _balance.RunUpgradeMaxLevelFor(type))
                return true;
            return type == RunUpgradeType.SummonCapacity &&
                   _gameManager != null &&
                   _gameManager.SummonSlotCapacity >= _gameManager.MaxSummonSlotCapacity;
        }

        public bool CanPurchaseRunUpgrade(RunUpgradeType type)
        {
            if (_gameManager == null || IsRunUpgradeMaxed(type) ||
                _gameManager.Gold < GetRunUpgradeCost(type))
                return false;
            return type != RunUpgradeType.CoreRecovery ||
                   _gameManager.CoreHp < _gameManager.MaxCoreHp;
        }

        public bool TryPurchaseRunUpgrade(RunUpgradeType type)
        {
            if (!CanPurchaseRunUpgrade(type)) return false;
            int cost = GetRunUpgradeCost(type);
            if (!_gameManager.TrySpendGold(cost)) return false;

            int index = Mathf.Clamp((int)type, 0, _runUpgradeLevels.Length - 1);
            _runUpgradeLevels[index]++;
            if (type == RunUpgradeType.CoreRecovery)
                _gameManager.HealCore(_balance.CoreRecoveryAmount(_runUpgradeLevels[index]));
            Save();
            Changed?.Invoke();
            return true;
        }

        public int GetSlimeLevelUpCost(string unitId)
        {
            int level = _summonManager?.GetUnitUpgradeState(unitId).Level ?? 1;
            return _balance.SlimeLevelUpCost(level);
        }

        public bool CanLevelUpSlime(string unitId)
        {
            if (_gameManager == null || _summonManager == null ||
                string.IsNullOrWhiteSpace(unitId) || !_summonManager.IsUnitOwned(unitId))
                return false;
            var state = _summonManager.GetUnitUpgradeState(unitId);
            return state.Level < _balance.SlimeMaxLevel &&
                   _gameManager.Gold >= _balance.SlimeLevelUpCost(state.Level);
        }

        public bool TryLevelUpSlime(string unitId)
        {
            if (!CanLevelUpSlime(unitId)) return false;
            var state = _summonManager.GetUnitUpgradeState(unitId);
            int cost = _balance.SlimeLevelUpCost(state.Level);
            if (!_gameManager.TrySpendGold(cost)) return false;

            int nextLevel = state.Level + 1;
            bool applied = _summonManager.ApplyUnitUpgrade(
                unitId,
                nextLevel,
                _balance.SlimeDamageMultiplier(nextLevel),
                _balance.SlimeAttackSpeedMultiplier(nextLevel));
            if (!applied)
            {
                _gameManager.AddGold(cost);
                return false;
            }

            Save();
            Changed?.Invoke();
            return true;
        }

        public float ModifyPlayerDamage(float baseDamage, float bonusCriticalChance = 0f)
        {
            return ModifyPlayerDamage(baseDamage, bonusCriticalChance, out _);
        }

        public float ModifyPlayerDamage(
            float baseDamage,
            float bonusCriticalChance,
            out bool critical)
        {
            float damage = Mathf.Max(0f, baseDamage) * RunDamageMultiplier;
            float criticalChance = Mathf.Clamp01(CriticalChance + Mathf.Max(0f, bonusCriticalChance));
            critical = criticalChance > 0f && _random.NextDouble() < criticalChance;
            if (critical)
                damage *= _balance.CriticalDamageMultiplier;
            return damage;
        }

        public string ToJson()
        {
            var data = new GrowthProgressionSaveData();
            foreach (RunUpgradeType type in Enum.GetValues(typeof(RunUpgradeType)))
            {
                int level = GetRunUpgradeLevel(type);
                if (level <= 0)
                    continue;

                data.runUpgrades.Add(new RunUpgradeLevelSaveData
                {
                    type = (int)type,
                    level = level,
                });
            }

            if (_summonManager != null)
            {
                foreach (SummonUnitUpgradeState state in _summonManager.EnumerateUnitUpgradeStates())
                {
                    if (state == null || string.IsNullOrWhiteSpace(state.UnitId) || state.Level <= 1)
                        continue;

                    data.slimeUpgrades.Add(new SlimeUpgradeLevelSaveData
                    {
                        unitId = state.UnitId,
                        level = state.Level,
                    });
                }
            }

            return JsonUtility.ToJson(data);
        }

        public void Flush()
        {
            Save();
        }

        void Save()
        {
            _saveJson?.Invoke(ToJson());
            _flushSaves?.Invoke();
        }

        void Load(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                var data = JsonUtility.FromJson<GrowthProgressionSaveData>(json);
                if (data == null || data.version != 1)
                    return;

                if (data.runUpgrades != null)
                {
                    for (int i = 0; i < data.runUpgrades.Count; i++)
                    {
                        RunUpgradeLevelSaveData entry = data.runUpgrades[i];
                        if (entry == null || !Enum.IsDefined(typeof(RunUpgradeType), entry.type))
                            continue;

                        var type = (RunUpgradeType)entry.type;
                        int index = Mathf.Clamp(entry.type, 0, _runUpgradeLevels.Length - 1);
                        _runUpgradeLevels[index] = Mathf.Clamp(
                            entry.level,
                            0,
                            _balance.RunUpgradeMaxLevelFor(type));
                    }
                }

                if (_summonManager == null || data.slimeUpgrades == null)
                    return;

                for (int i = 0; i < data.slimeUpgrades.Count; i++)
                {
                    SlimeUpgradeLevelSaveData entry = data.slimeUpgrades[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.unitId))
                        continue;

                    int level = Mathf.Clamp(entry.level, 1, _balance.SlimeMaxLevel);
                    _summonManager.ApplyUnitUpgrade(
                        entry.unitId,
                        level,
                        _balance.SlimeDamageMultiplier(level),
                        _balance.SlimeAttackSpeedMultiplier(level));
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[CrossDefense] Failed to load growth progression: {exception.Message}");
            }
        }
    }
}
