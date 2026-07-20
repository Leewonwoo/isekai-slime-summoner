using CrossDefense.Data;

namespace CrossDefense.Core
{
    public enum SummonResultKind
    {
        Unit,
        DirectRankOneUnit,
        Currency,
    }

    public readonly struct SummonResult
    {
        public int Id { get; }
        public SummonResultKind Kind { get; }
        public SummonUnitData Unit { get; }
        public int Rank { get; }
        public int CurrencyAmount { get; }

        public bool IsUnit => Kind != SummonResultKind.Currency && Unit != null;
        public bool IsJackpot => Kind == SummonResultKind.DirectRankOneUnit;

        SummonResult(int id, SummonResultKind kind, SummonUnitData unit, int rank, int currencyAmount)
        {
            Id = id;
            Kind = kind;
            Unit = unit;
            Rank = rank;
            CurrencyAmount = currencyAmount;
        }

        public static SummonResult UnitResult(int id, SummonUnitData unit, bool jackpot) =>
            new(id, jackpot ? SummonResultKind.DirectRankOneUnit : SummonResultKind.Unit,
                unit, jackpot ? 1 : 0, 0);

        public static SummonResult RankedUnitResult(int id, SummonUnitData unit, int rank)
        {
            int safeRank = SummonRank.Clamp(rank);
            return new SummonResult(
                id,
                safeRank == 1 ? SummonResultKind.DirectRankOneUnit : SummonResultKind.Unit,
                unit,
                safeRank,
                0);
        }

        public static SummonResult CurrencyResult(int id, int amount) =>
            new(id, SummonResultKind.Currency, null, 0, amount);
    }

    public sealed class SummonUnitInstance
    {
        public int InstanceId { get; }
        public SummonUnitData Unit { get; }
        public int Rank { get; private set; }
        public SummonUnitUpgradeState UpgradeState { get; private set; }
        public int Level => UpgradeState?.Level ?? 1;
        public float DamageMultiplier => UpgradeState?.DamageMultiplier ?? 1f;
        public float AttackSpeedMultiplier => UpgradeState?.AttackSpeedMultiplier ?? 1f;

        public SummonUnitInstance(
            int instanceId,
            SummonUnitData unit,
            int rank,
            SummonUnitUpgradeState upgradeState = null)
        {
            InstanceId = instanceId;
            Unit = unit;
            Rank = SummonRank.Clamp(rank);
            UpgradeState = upgradeState != null && upgradeState.UnitId == unit?.UnitId
                ? upgradeState
                : new SummonUnitUpgradeState(unit?.UnitId);
        }

        internal void BindUpgradeState(SummonUnitUpgradeState upgradeState)
        {
            if (upgradeState != null && upgradeState.UnitId == Unit?.UnitId)
                UpgradeState = upgradeState;
        }

        public bool TryPromote()
        {
            if (Rank >= SummonRank.MaxInternalRank) return false;
            Rank++;
            return true;
        }
    }

    /// <summary>
    /// 같은 unitId의 모든 벤치/필드 인스턴스가 공유하는 인게임 강화 상태.
    /// 강화 비용과 배율 계산은 강화 시스템이 결정하고, 전투 인스턴스는 이 결과만 참조한다.
    /// </summary>
    public sealed class SummonUnitUpgradeState
    {
        public string UnitId { get; }
        public int Level { get; private set; } = 1;
        public float DamageMultiplier { get; private set; } = 1f;
        public float AttackSpeedMultiplier { get; private set; } = 1f;

        public SummonUnitUpgradeState(string unitId)
        {
            UnitId = unitId ?? string.Empty;
        }

        public bool Apply(int level, float damageMultiplier, float attackSpeedMultiplier)
        {
            int nextLevel = UnityEngine.Mathf.Max(1, level);
            float nextDamage = UnityEngine.Mathf.Max(0.01f, damageMultiplier);
            float nextAttackSpeed = UnityEngine.Mathf.Max(0.01f, attackSpeedMultiplier);
            if (Level == nextLevel &&
                UnityEngine.Mathf.Approximately(DamageMultiplier, nextDamage) &&
                UnityEngine.Mathf.Approximately(AttackSpeedMultiplier, nextAttackSpeed))
                return false;

            Level = nextLevel;
            DamageMultiplier = nextDamage;
            AttackSpeedMultiplier = nextAttackSpeed;
            return true;
        }
    }
}
