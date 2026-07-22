using System;
using System.Collections.Generic;
using CrossDefense.Data;
using CrossDefense.Units;
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
        readonly int _maxCapacity;
        readonly Func<int> _capacityProvider;
        readonly Func<int> _summonerLevelProvider;

        int _nextResultId = 1;
        int _nextInstanceId = 1;
        bool _hasPendingResult;
        SummonResult _pendingResult;

        public IReadOnlyList<SummonUnitInstance> Bench => _bench;
        public IReadOnlyList<SummonUnitData> Pool => _pool;
        public int BenchCapacity => Mathf.Clamp(
            _capacityProvider?.Invoke() ?? _maxCapacity,
            1,
            _maxCapacity);
        public int BenchStackCount => CountBenchStacks();
        public int TotalOwnedCount => CountTotalOwnedUnits();
        public bool IsBenchFull => TotalOwnedCount >= BenchCapacity;
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
            Func<float> directRankOneChanceProvider = null,
            Func<int> capacityProvider = null,
            Func<int> summonerLevelProvider = null)
        {
            _gameManager = gameManager;
            _pool = new List<SummonUnitData>();
            if (pool != null)
            {
                foreach (var unit in pool)
                {
                    if (unit != null && unit.Weight > 0)
                        _pool.Add(unit);
                }
            }

            _currencyChance = Mathf.Clamp01(currencyChance);
            _directRankOneChance = Mathf.Clamp01(directRankOneChance);
            _directRankOneChanceProvider = directRankOneChanceProvider;
            _currencyReward = Mathf.Max(0, currencyReward);
            _maxCapacity = Mathf.Max(1, benchCapacity);
            _capacityProvider = capacityProvider;
            _summonerLevelProvider = summonerLevelProvider;
            _random = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        }

        public bool TryBeginSummon(out SummonResult result)
        {
            result = default;
            if (_gameManager == null || _hasPendingResult || IsBenchFull)
                return false;
            if (!_gameManager.TrySpendSummonContract())
                return false;

            int id = _nextResultId++;
            if (!HasUnlockedUnit() || _random.NextDouble() < _currencyChance)
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

        public bool TryGrantRandomReward(int rank, out SummonResult result, bool preferUnowned = false)
        {
            result = default;
            SummonUnitData unit = PickRewardUnit(SummonRank.Clamp(rank), preferUnowned);
            return TryGrantRewardUnit(unit, rank, out result);
        }

        public bool TryGrantJackpotEgg(
            float rankOneChance,
            float rankTwoChance,
            out SummonResult result)
        {
            double roll = _random.NextDouble();
            float safeRankTwoChance = Mathf.Clamp01(rankTwoChance);
            float safeRankOneChance = Mathf.Clamp01(rankOneChance);
            int rank = roll < safeRankTwoChance
                ? 2
                : roll < safeRankTwoChance + safeRankOneChance
                    ? 1
                    : 0;
            return TryGrantRandomReward(rank, out result);
        }

        public bool TryGrantRewardUnit(SummonUnitData unit, int rank, out SummonResult result)
        {
            result = default;
            int safeRank = SummonRank.Clamp(rank);
            if (unit == null || !CanAddToBench(unit, safeRank))
                return false;

            int id = _nextResultId++;
            result = SummonResult.RankedUnitResult(id, unit, safeRank);
            var instance = new SummonUnitInstance(
                _nextInstanceId++,
                unit,
                safeRank,
                GetUnitUpgradeState(unit.UnitId));
            _bench.Add(instance);
            BenchChanged?.Invoke(_bench);
            UnitAdded?.Invoke(instance);
            ResultCommitted?.Invoke(result);
            return true;
        }

        public int GrantMergeSupport(int amount, List<SummonResult> results)
        {
            if (amount <= 0 || results == null)
                return 0;

            if (!TryFindMergeSupportTarget(out SummonUnitData unit, out int rank))
                return 0;

            int granted = 0;
            for (int i = 0; i < amount; i++)
            {
                if (!TryGrantRewardUnit(unit, rank, out SummonResult result))
                    break;
                results.Add(result);
                granted++;
            }
            return granted;
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
            return TotalOwnedCount < BenchCapacity;
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

        int CountTotalOwnedUnits()
        {
            int count = _bench.Count;
            var fieldUnits = _gameManager?.SummonedUnitManager?.Units;
            if (fieldUnits == null)
                return count;
            for (int i = 0; i < fieldUnits.Count; i++)
            {
                if (fieldUnits[i]?.Instance != null)
                    count++;
            }
            return count;
        }

        SummonUnitData PickUnit(bool jackpot)
        {
            var candidates = new List<SummonUnitData>();
            if (jackpot)
            {
                foreach (var unit in _pool)
                {
                    if (IsUnlocked(unit) && unit.Rarity >= SummonUnitRarity.Rare && !IsOwned(unit))
                        candidates.Add(unit);
                }
            }

            if (candidates.Count == 0)
            {
                foreach (var unit in _pool)
                    if (IsUnlocked(unit)) candidates.Add(unit);
            }

            if (candidates.Count == 0)
                return null;

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

        SummonUnitData PickRewardUnit(int rank, bool preferUnowned)
        {
            var candidates = new List<SummonUnitData>();
            for (int i = 0; i < _pool.Count; i++)
            {
                SummonUnitData unit = _pool[i];
                if (unit == null || !IsUnlocked(unit) || !CanAddToBench(unit, rank))
                    continue;
                if (preferUnowned && IsOwned(unit))
                    continue;
                candidates.Add(unit);
            }

            if (candidates.Count == 0 && preferUnowned)
            {
                for (int i = 0; i < _pool.Count; i++)
                {
                    SummonUnitData unit = _pool[i];
                    if (unit != null && IsUnlocked(unit) && CanAddToBench(unit, rank))
                        candidates.Add(unit);
                }
            }
            if (candidates.Count == 0)
                return null;

            int totalWeight = 0;
            for (int i = 0; i < candidates.Count; i++)
                totalWeight += Mathf.Max(1, candidates[i].Weight);
            int roll = _random.Next(Mathf.Max(1, totalWeight));
            for (int i = 0; i < candidates.Count; i++)
            {
                roll -= Mathf.Max(1, candidates[i].Weight);
                if (roll < 0)
                    return candidates[i];
            }
            return candidates[candidates.Count - 1];
        }

        bool TryFindMergeSupportTarget(out SummonUnitData unit, out int rank)
        {
            unit = null;
            rank = 0;
            var counts = new Dictionary<string, int>();
            var units = new Dictionary<string, SummonUnitData>();

            void Count(SummonUnitInstance instance)
            {
                if (instance?.Unit == null || instance.Rank >= SummonRank.MaxInternalRank)
                    return;
                string key = $"{instance.Unit.UnitId}:{instance.Rank}";
                counts[key] = counts.TryGetValue(key, out int current) ? current + 1 : 1;
                units[key] = instance.Unit;
            }

            for (int i = 0; i < _bench.Count; i++)
                Count(_bench[i]);
            IReadOnlyList<SummonedUnitController> fieldUnits = _gameManager?.SummonedUnitManager?.Units;
            if (fieldUnits != null)
            {
                for (int i = 0; i < fieldUnits.Count; i++)
                    Count(fieldUnits[i]?.Instance);
            }

            string bestKey = null;
            int bestNeed = int.MaxValue;
            int bestCount = -1;
            foreach (KeyValuePair<string, int> pair in counts)
            {
                int remainder = pair.Value % SummonRank.MergeMaterialCount;
                int need = remainder == 0
                    ? SummonRank.MergeMaterialCount
                    : SummonRank.MergeMaterialCount - remainder;
                if (need > bestNeed || need == bestNeed && pair.Value <= bestCount)
                    continue;
                bestNeed = need;
                bestCount = pair.Value;
                bestKey = pair.Key;
            }

            if (bestKey == null || !units.TryGetValue(bestKey, out unit))
                return false;
            int separator = bestKey.LastIndexOf(':');
            return separator >= 0 && int.TryParse(bestKey[(separator + 1)..], out rank);
        }

        bool IsOwned(SummonUnitData unit)
        {
            return unit != null && IsUnitOwned(unit.UnitId);
        }

        bool IsUnlocked(SummonUnitData unit) =>
            unit != null && unit.IsUnlockedAtLevel(_summonerLevelProvider?.Invoke() ?? 1);

        public bool IsUnitUnlocked(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return false;
            for (int i = 0; i < _pool.Count; i++)
            {
                SummonUnitData unit = _pool[i];
                if (unit != null && unit.UnitId == unitId) return IsUnlocked(unit);
            }
            return false;
        }

        bool HasUnlockedUnit()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (IsUnlocked(_pool[i])) return true;
            return false;
        }
    }
}
