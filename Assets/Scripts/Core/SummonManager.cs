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
        readonly System.Random _random;
        readonly float _currencyChance;
        readonly float _directRankOneChance;
        readonly int _currencyReward;
        readonly int _benchCapacity;

        int _nextResultId = 1;
        int _nextInstanceId = 1;
        bool _hasPendingResult;
        SummonResult _pendingResult;

        public IReadOnlyList<SummonUnitInstance> Bench => _bench;
        public int BenchCapacity => _benchCapacity;
        public bool IsBenchFull => _bench.Count >= _benchCapacity;
        public bool HasPendingResult => _hasPendingResult;

        public event Action<IReadOnlyList<SummonUnitInstance>> BenchChanged;
        public event Action<SummonResult> ResultCommitted;
        public event Action<SummonUnitInstance> UnitAdded;

        public SummonManager(
            GameManager gameManager,
            IEnumerable<SummonUnitData> pool,
            float currencyChance,
            float directRankOneChance,
            int currencyReward,
            int benchCapacity,
            int randomSeed = 0)
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
                bool jackpot = _random.NextDouble() < _directRankOneChance;
                result = SummonResult.UnitResult(id, PickUnit(jackpot), jackpot);
            }

            // 벤치가 가득 찬 상태에서 계약서를 잃지 않도록 소환 결과를 재화 보상으로 전환한다.
            if (result.IsUnit && IsBenchFull)
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
            else if (result.Unit != null && _bench.Count < _benchCapacity)
            {
                var instance = new SummonUnitInstance(_nextInstanceId++, result.Unit, result.Rank);
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
            if (instance == null || IsBenchFull || _bench.Contains(instance)) return false;
            _bench.Add(instance);
            BenchChanged?.Invoke(_bench);
            return true;
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
            foreach (var instance in _bench)
            {
                if (instance.Unit != null && instance.Unit.UnitId == unit.UnitId)
                    return true;
            }

            var fieldUnits = _gameManager?.SummonedUnitManager?.Units;
            if (fieldUnits != null)
            {
                foreach (var fieldUnit in fieldUnits)
                {
                    if (fieldUnit?.Data != null && fieldUnit.Data.UnitId == unit.UnitId)
                        return true;
                }
            }

            return false;
        }
    }
}
