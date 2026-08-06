using System;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum SummonerSkillId
    {
        Meteor,
        IceWall,
        Aegis,
        ArcaneBurst,
        LightningStrike,
        WaterBurst,
        Gale,
    }

    public enum SummonerSkillTargeting
    {
        Point,
        Directional,
        Instant,
    }

    public readonly struct SummonerSkillDefinition
    {
        public SummonerSkillId Id { get; }
        public string DisplayName { get; }
        public int UnlockLevel { get; }
        public SummonerSkillTargeting Targeting { get; }
        public float Cooldown { get; }
        public float DamageMultiplier { get; }
        public float Radius { get; }
        public float Duration { get; }
        public string Description { get; }

        public SummonerSkillDefinition(
            SummonerSkillId id,
            string displayName,
            int unlockLevel,
            SummonerSkillTargeting targeting,
            float cooldown,
            float damageMultiplier,
            float radius,
            float duration,
            string description)
        {
            Id = id;
            DisplayName = displayName;
            UnlockLevel = Mathf.Max(1, unlockLevel);
            Targeting = targeting;
            Cooldown = Mathf.Max(0.1f, cooldown);
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            Radius = Mathf.Max(0f, radius);
            Duration = Mathf.Max(0f, duration);
            Description = description ?? string.Empty;
        }
    }

}
