using CrossDefense.Data;

namespace CrossDefense.Core
{
    /// <summary>SPEC §2.8의 유닛 공격 속성 → 몬스터 속성 단방향 상성.</summary>
    public static class ElementalMatchup
    {
        public const float AdvantageMultiplier = 1.5f;
        public const float DisadvantageMultiplier = 0.75f;

        public static float GetDamageMultiplier(MonsterAttribute attack, MonsterAttribute defense)
        {
            if (attack == MonsterAttribute.None || defense == MonsterAttribute.None || attack == defense)
                return 1f;

            if (IsAdvantage(attack, defense))
                return AdvantageMultiplier;

            if (IsAdvantage(defense, attack))
                return DisadvantageMultiplier;

            return 1f;
        }

        static bool IsAdvantage(MonsterAttribute attack, MonsterAttribute defense)
        {
            return (attack == MonsterAttribute.Fire && defense == MonsterAttribute.Nature)
                || (attack == MonsterAttribute.Nature && defense == MonsterAttribute.Ice)
                || (attack == MonsterAttribute.Ice && defense == MonsterAttribute.Fire);
        }
    }
}
