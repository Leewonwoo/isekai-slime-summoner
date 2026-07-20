using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct DamagePacket
    {
        public readonly Object Source;
        public readonly float BaseDamage;
        public readonly MonsterAttribute Attribute;
        public readonly float SlowPercent;
        public readonly float SlowDuration;
        public readonly float DamageOverTime;
        public readonly float DamageOverTimeDuration;

        public DamagePacket(
            Object source,
            float baseDamage,
            MonsterAttribute attribute,
            float slowPercent = 0f,
            float slowDuration = 0f,
            float damageOverTime = 0f,
            float damageOverTimeDuration = 0f)
        {
            Source = source;
            BaseDamage = Mathf.Max(0f, baseDamage);
            Attribute = attribute;
            SlowPercent = Mathf.Clamp(slowPercent, 0f, 0.95f);
            SlowDuration = Mathf.Max(0f, slowDuration);
            DamageOverTime = Mathf.Max(0f, damageOverTime);
            DamageOverTimeDuration = Mathf.Max(0f, damageOverTimeDuration);
        }

        public float ResolveDamage(MonsterAttribute defense) =>
            BaseDamage * ElementalMatchup.GetDamageMultiplier(Attribute, defense);

        public DamagePacket Scaled(float multiplier) =>
            new(
                Source,
                BaseDamage * Mathf.Max(0f, multiplier),
                Attribute,
                SlowPercent,
                SlowDuration,
                DamageOverTime * Mathf.Max(0f, multiplier),
                DamageOverTimeDuration);
    }
}
