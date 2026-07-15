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

        public static SummonResult CurrencyResult(int id, int amount) =>
            new(id, SummonResultKind.Currency, null, 0, amount);
    }

    public sealed class SummonUnitInstance
    {
        public int InstanceId { get; }
        public SummonUnitData Unit { get; }
        public int Rank { get; private set; }

        public SummonUnitInstance(int instanceId, SummonUnitData unit, int rank)
        {
            InstanceId = instanceId;
            Unit = unit;
            Rank = rank;
        }

        public bool TryPromote()
        {
            if (Rank >= 3) return false;
            Rank++;
            return true;
        }
    }
}
