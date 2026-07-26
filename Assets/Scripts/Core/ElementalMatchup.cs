using System;
using System.Collections.Generic;
using CrossDefense.Data;

namespace CrossDefense.Core
{
    public enum ElementalDamageRelation
    {
        Neutral,
        SameAttribute,
        Resisted,
        Weakness,
    }

    public readonly struct ElementalMatchupRule
    {
        public MonsterAttribute Attack { get; }
        public MonsterAttribute Defense { get; }

        public ElementalMatchupRule(
            MonsterAttribute attack,
            MonsterAttribute defense)
        {
            Attack = attack;
            Defense = defense;
        }
    }

    /// <summary>
    /// Single source of truth for elemental strengths, weaknesses and resistance.
    /// Adding a strong pair automatically makes the reverse attack resisted.
    /// </summary>
    public static class ElementalMatchup
    {
        public const float WeaknessMultiplier = 1.5f;
        public const float ResistanceMultiplier = 0.65f;
        public const float SameAttributeMultiplier = 0.75f;
        public const float NeutralMultiplier = 1f;

        // Attack -> defense pairs. The reverse direction is resistance.
        static readonly ElementalMatchupRule[] StrongRules =
        {
            new(MonsterAttribute.Fire, MonsterAttribute.Nature),
            new(MonsterAttribute.Nature, MonsterAttribute.Ice),
            new(MonsterAttribute.Nature, MonsterAttribute.Water),
            new(MonsterAttribute.Ice, MonsterAttribute.Fire),
            new(MonsterAttribute.Ice, MonsterAttribute.Wind),
            new(MonsterAttribute.Water, MonsterAttribute.Fire),
            new(MonsterAttribute.Lightning, MonsterAttribute.Water),
            new(MonsterAttribute.Lightning, MonsterAttribute.Ice),
            new(MonsterAttribute.Wind, MonsterAttribute.Lightning),
            new(MonsterAttribute.Wind, MonsterAttribute.Nature),
        };

        static readonly HashSet<(MonsterAttribute attack, MonsterAttribute defense)>
            StrongLookup = BuildLookup();

        public static IReadOnlyList<ElementalMatchupRule> Rules => StrongRules;

        public static float GetDamageMultiplier(
            MonsterAttribute attack,
            MonsterAttribute defense)
        {
            return GetRelation(attack, defense) switch
            {
                ElementalDamageRelation.Weakness => WeaknessMultiplier,
                ElementalDamageRelation.Resisted => ResistanceMultiplier,
                ElementalDamageRelation.SameAttribute => SameAttributeMultiplier,
                _ => NeutralMultiplier,
            };
        }

        public static ElementalDamageRelation GetRelation(
            MonsterAttribute attack,
            MonsterAttribute defense)
        {
            if (attack == MonsterAttribute.None || defense == MonsterAttribute.None)
                return ElementalDamageRelation.Neutral;
            if (attack == defense)
                return ElementalDamageRelation.SameAttribute;
            if (StrongLookup.Contains((attack, defense)))
                return ElementalDamageRelation.Weakness;
            if (StrongLookup.Contains((defense, attack)))
                return ElementalDamageRelation.Resisted;
            return ElementalDamageRelation.Neutral;
        }

        public static IReadOnlyList<MonsterAttribute> GetStrongAgainst(
            MonsterAttribute attack)
        {
            var result = new List<MonsterAttribute>();
            for (int i = 0; i < StrongRules.Length; i++)
                if (StrongRules[i].Attack == attack)
                    result.Add(StrongRules[i].Defense);
            return result;
        }

        public static IReadOnlyList<MonsterAttribute> GetWeakAgainst(
            MonsterAttribute defense)
        {
            var result = new List<MonsterAttribute>();
            for (int i = 0; i < StrongRules.Length; i++)
                if (StrongRules[i].Defense == defense)
                    result.Add(StrongRules[i].Attack);
            return result;
        }

        static HashSet<(MonsterAttribute attack, MonsterAttribute defense)> BuildLookup()
        {
            var lookup =
                new HashSet<(MonsterAttribute attack, MonsterAttribute defense)>();
            for (int i = 0; i < StrongRules.Length; i++)
                lookup.Add((StrongRules[i].Attack, StrongRules[i].Defense));
            return lookup;
        }
    }
}
