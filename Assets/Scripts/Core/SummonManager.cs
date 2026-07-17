using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>계약서 소비, 결과 판정, 벤치 등록을 담당한다. 룰렛 UI는 이 클래스의 결과를 연출만 한다.</summary>
    public sealed class SummonManager
    {
        readonly GameManager _gameManager;
        readonly List<SummonUnitData> _pool;
        readonly List<SummonUnitInstance> _bench = new();
        readonly Dictionary<string, SummonUnitUpgradeState> _unitUpgrades = new();
        readonly System.Random _random;
        readonly float _currencyChance;
        readonly float _directRankOneChance;
        readonly Func<float> _directRankOneChanceProvider;
        readonly int _currencyReward;
        readonly int _benchCapacity;

        int _nextResultId = 1;
        int _nextInstanceId = 1;
        bool _hasPendingResult;
        SummonResult _pendingResult;

        public IReadOnlyList<SummonUnitInstance> Bench => _bench;
        public int BenchCapacity => _benchCapacity;
        public int BenchStackCount => CountBenchStacks();
        public bool IsBenchFull => BenchStackCount >= _benchCapacity;
        public bool HasPendingResult => _hasPendingResult;

        public event Action<IReadOnlyList<SummonUnitInstance>> BenchChanged;
        public event Action<SummonResult> ResultCommitted;
        public event Action<SummonUnitInstance> UnitAdded;
        public event Action<SummonUnitUpgradeState> UnitUpgradeChanged;

        public SummonManager(
            GameManager gameManager,
            IEnumerable<SummonUnitData> pool,
            float currencyChance,
            float directRankOneChance,
            int currencyReward,
            int benchCapacity,
            int randomSeed = 0,
            Func<float> directRankOneChanceProvider = null)
        {
            _gameManager = gameManager;
            _pool = new List<SummonUnitData>();
            if (pool != null)
            {
                foreach (var unit in pool)
                {
                    if (unit != null && unit.UnlockedByDefault && unit.Weight > 0)
                        _pool.Add(unit);
                }
            }

            _currencyChance = Mathf.Clamp01(currencyChance);
            _directRankOneChance = Mathf.Clamp01(directRankOneChance);
            _directRankOneChanceProvider = directRankOneChanceProvider;
            _currencyReward = Mathf.Max(0, currencyReward);
            _benchCapacity = Mathf.Max(1, benchCapacity);
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public bool TryBeginSummon(out SummonResult result)
        {
            result = default;
            if (_gameManager == null || _hasPendingResult)
                return false;
            if (!_gameManager.TrySpendSummonContract())
                return false;

            int id = _nextResultId++;
            if (_pool.Count == 0 || _random.NextDouble() < _currencyChance)
            {
                result = SummonResult.CurrencyResult(id, _currencyReward);
            }
            else
            {
                float jackpotChance = Mathf.Clamp01(
                    _directRankOneChanceProvider?.Invoke() ?? _directRankOneChance);
                bool jackpot = _random.NextDouble() < jackpotChance;
                result = SummonResult.UnitResult(id, PickUnit(jackpot), jackpot);
            }

            // 벤치가 가득 찬 상태에서 계약서를 잃지 않도록 소환 결과를 재화 보상으로 전환한다.
            if (result.IsUnit && !CanAddToBench(result.Unit, result.Rank))
                result = SummonResult.CurrencyResult(id, _currencyReward);

            _pendingResult = result;
            _hasPendingResult = true;
            return true;
        }

        public bool CommitPending(SummonResult result)
        {
            if (!_hasPendingResult || result.Id != _pendingResult.Id)
                return false;

            _hasPendingResult = false;
            _pendingResult = default;

            if (result.Kind == SummonResultKind.Currency)
            {
                _gameManager.AddGold(result.CurrencyAmount);
            }
            else if (result.Unit != null && CanAddToBench(result.Unit, result.Rank))
            {
                var instance = new SummonUnitInstance(
                    _nextInstanceId++,
                    result.Unit,
                    result.Rank,
                    GetUnitUpgradeState(result.Unit.UnitId));
                _bench.Add(instance);
                BenchChanged?.Invoke(_bench);
                UnitAdded?.Invoke(instance);
            }

            ResultCommitted?.Invoke(result);
            return true;
        }

        public void CancelPendingAndRefund()
        {
            if (!_hasPendingResult) return;
            _hasPendingResult = false;
            _pendingResult = default;
            _gameManager.AddSummonContracts(1);
        }

        public bool TryTakeFromBench(int instanceId, out SummonUnitInstance instance)
        {
            for (int i = 0; i < _bench.Count; i++)
            {
                if (_bench[i].InstanceId != instanceId) continue;
                instance = _bench[i];
                _bench.RemoveAt(i);
                BenchChanged?.Invoke(_bench);
                return true;
            }

            instance = null;
            return false;
        }

        public bool TryRemoveBenchMatch(string unitId, int rank, out SummonUnitInstance instance)
        {
            for (int i = 0; i < _bench.Count; i++)
            {
                var candidate = _bench[i];
                if (candidate.Unit == null || candidate.Unit.UnitId != unitId || candidate.Rank != rank)
                    continue;
                instance = candidate;
                _bench.RemoveAt(i);
                BenchChanged?.Invoke(_bench);
                return true;
            }

            instance = null;
            return false;
        }

        public bool ReturnToBench(SummonUnitInstance instance)
        {
            if (instance == null || _bench.Contains(instance) || !CanAddToBench(instance.Unit, instance.Rank))
                return false;
            instance.BindUpgradeState(GetUnitUpgradeState(instance.Unit.UnitId));
            _bench.Add(instance);
            BenchChanged?.Invoke(_bench);
            return true;
        }

        public SummonUnitUpgradeState GetUnitUpgradeState(string unitId)
        {
            string safeUnitId = unitId ?? string.Empty;
            if (_unitUpgrades.TryGetValue(safeUnitId, out var state))
                return state;

            state = new SummonUnitUpgradeState(safeUnitId);
            _unitUpgrades.Add(safeUnitId, state);
            return state;
        }

        public bool ApplyUnitUpgrade(
            string unitId,
            int level,
            float damageMultiplier,
            float attackSpeedMultiplier)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            var state = GetUnitUpgradeState(unitId);
            if (!state.Apply(level, damageMultiplier, attackSpeedMultiplier)) return false;

            UnitUpgradeChanged?.Invoke(state);
            BenchChanged?.Invoke(_bench);
            return true;
        }

        public bool IsUnitOwned(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            foreach (var instance in _bench)
            {
                if (instance?.Unit != null && instance.Unit.UnitId == unitId)
                    return true;
            }

            var fieldUnits = _gameManager?.SummonedUnitManager?.Units;
            if (fieldUnits == null) return false;
            foreach (var fieldUnit in fieldUnits)
            {
                if (fieldUnit?.Data != null && fieldUnit.Data.UnitId == unitId)
                    return true;
            }
            return false;
        }

        bool CanAddToBench(SummonUnitData unit, int rank)
        {
            if (unit == null) return false;
            return HasBenchStack(unit.UnitId, rank) || BenchStackCount < _benchCapacity;
        }

        bool HasBenchStack(string unitId, int rank)
        {
            foreach (var instance in _bench)
            {
                if (instance?.Unit != null && instance.Unit.UnitId == unitId && instance.Rank == rank)
                    return true;
            }
            return false;
        }

        int CountBenchStacks()
        {
            var keys = new HashSet<string>();
            foreach (var instance in _bench)
            {
                if (instance?.Unit == null) continue;
                keys.Add($"{instance.Unit.UnitId}:{instance.Rank}");
            }
            return keys.Count;
        }

        SummonUnitData PickUnit(bool jackpot)
        {
            var candidates = new List<SummonUnitData>();
            if (jackpot)
            {
                foreach (var unit in _pool)
                {
                    if (unit.Rarity >= SummonUnitRarity.Rare && !IsOwned(unit))
                        candidates.Add(unit);
                }
            }

            if (candidates.Count == 0)
                candidates.AddRange(_pool);

            int totalWeight = 0;
            foreach (var unit in candidates)
                totalWeight += Mathf.Max(1, unit.Weight);

            int roll = _random.Next(0, totalWeight);
            foreach (var unit in candidates)
            {
                roll -= Mathf.Max(1, unit.Weight);
                if (roll < 0)
                    return unit;
            }

            return candidates[candidates.Count - 1];
        }

        bool IsOwned(SummonUnitData unit)
        {
            return unit != null && IsUnitOwned(unit.UnitId);
        }
    }
}
