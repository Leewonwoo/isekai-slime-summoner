using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>런 전용 전체 강화와 unitId별 즉시 슬라임 레벨업을 관리한다.</summary>
    public sealed class GrowthManager
    {
        readonly GameManager _gameManager;
        readonly SummonManager _summonManager;
        readonly GrowthBalanceData _balance;
        readonly int[] _runUpgradeLevels = new int[Enum.GetValues(typeof(RunUpgradeType)).Length];
        readonly System.Random _random;

        public GrowthBalanceData Balance => _balance;
        public float RunDamageMultiplier =>
            _balance.RunAttackPowerMultiplier(GetRunUpgradeLevel(RunUpgradeType.AttackPower));
        public float RunAttackSpeedMultiplier =>
            _balance.RunAttackSpeedMultiplier(GetRunUpgradeLevel(RunUpgradeType.AttackSpeed));
        public float CriticalChance =>
            _balance.RunCriticalChance(GetRunUpgradeLevel(RunUpgradeType.CriticalChance));

        public event Action Changed;

        public GrowthManager(
            GameManager gameManager,
            SummonManager summonManager,
            GrowthBalanceData balance,
            int randomSeed = 0)
        {
            _gameManager = gameManager;
            _summonManager = summonManager;
            _balance = balance != null ? balance : GrowthBalanceData.CreateRuntimeDefault();
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public int GetRunUpgradeLevel(RunUpgradeType type) =>
            _runUpgradeLevels[Mathf.Clamp((int)type, 0, _runUpgradeLevels.Length - 1)];

        public int GetRunUpgradeCost(RunUpgradeType type) =>
            _balance.RunUpgradeCost(type, GetRunUpgradeLevel(type));

        public bool CanPurchaseRunUpgrade(RunUpgradeType type)
        {
            int level = GetRunUpgradeLevel(type);
            if (_gameManager == null || level >= _balance.RunUpgradeMaxLevel ||
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

            Changed?.Invoke();
            return true;
        }

        public float ModifyPlayerDamage(float baseDamage, float bonusCriticalChance = 0f)
        {
            float damage = Mathf.Max(0f, baseDamage) * RunDamageMultiplier;
            float criticalChance = Mathf.Clamp01(CriticalChance + Mathf.Max(0f, bonusCriticalChance));
            if (criticalChance > 0f && _random.NextDouble() < criticalChance)
                damage *= _balance.CriticalDamageMultiplier;
            return damage;
        }
    }
}
