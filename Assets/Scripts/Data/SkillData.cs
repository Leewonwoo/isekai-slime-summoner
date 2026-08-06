using System;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum SkillCategory
    {
        BasicAttack,
        Active,
    }

    public enum SkillExecutionMode
    {
        BasicProjectile,
        Meteor,
        IceWall,
        ElementBurst,
        Shield,
    }

    [Serializable]
    public sealed class SkillRankProfile
    {
        [Min(0f)] [SerializeField] float damageMultiplier = 1f;
        [Min(0f)] [SerializeField] float radiusMultiplier = 1f;
        [Min(0f)] [SerializeField] float durationMultiplier = 1f;
        [Min(1)] [SerializeField] int strikeCount = 1;
        [Min(0.01f)] [SerializeField] float strikeInterval = 0.24f;
        [Min(0f)] [SerializeField] float perStrikeDamageMultiplier = 1f;
        [Min(0f)] [SerializeField] float perStrikeRadiusMultiplier = 1f;
        [SerializeField] bool battlefieldWide;
        [Range(0f, 0.95f)] [SerializeField] float slowPercent;
        [Min(0f)] [SerializeField] float slowDuration;
        [Min(0f)] [SerializeField] float damageOverTimeMultiplier;
        [Min(0f)] [SerializeField] float damageOverTimeDuration;
        [Min(0f)] [SerializeField] float strength;
        [Min(0.1f)] [SerializeField] float visualScale = 1f;

        public float DamageMultiplier => Mathf.Max(0f, damageMultiplier);
        public float RadiusMultiplier => Mathf.Max(0f, radiusMultiplier);
        public float DurationMultiplier => Mathf.Max(0f, durationMultiplier);
        public int StrikeCount => Mathf.Max(1, strikeCount);
        public float StrikeInterval => Mathf.Max(0.01f, strikeInterval);
        public float PerStrikeDamageMultiplier => Mathf.Max(0f, perStrikeDamageMultiplier);
        public float PerStrikeRadiusMultiplier => Mathf.Max(0f, perStrikeRadiusMultiplier);
        public bool BattlefieldWide => battlefieldWide;
        public float SlowPercent => Mathf.Clamp(slowPercent, 0f, 0.95f);
        public float SlowDuration => Mathf.Max(0f, slowDuration);
        public float DamageOverTimeMultiplier => Mathf.Max(0f, damageOverTimeMultiplier);
        public float DamageOverTimeDuration => Mathf.Max(0f, damageOverTimeDuration);
        public float Strength => Mathf.Max(0f, strength);
        public float VisualScale => Mathf.Max(0.1f, visualScale);
    }

    [CreateAssetMenu(
        fileName = "SkillData",
        menuName = "Isekai Slime Summoner/Data/Skill",
        order = 22)]
    public sealed class SkillData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string skillId;
        [SerializeField] string displayName;
        [TextArea] [SerializeField] string description;
        [SerializeField] SkillCategory category;
        [SerializeField] SkillExecutionMode executionMode;

        [Header("Basic Attack")]
        [SerializeField] SummonerAttackArchetype attackArchetype;
        [SerializeField] Sprite projectileSprite;
        [Min(0.1f)] [SerializeField] float baseDamage = 12f;
        [Min(0.1f)] [SerializeField] float attacksPerSecond = 1.25f;
        [Min(0.1f)] [SerializeField] float attackRange = 4.5f;
        [Min(0.1f)] [SerializeField] float projectileSpeed = 10f;
        [Min(0.01f)] [SerializeField] float projectileScale = 0.65f;
        [Min(0.1f)] [SerializeField] float clickDamage = 18f;
        [Min(0.1f)] [SerializeField] float clickAttacksPerSecond = 2f;
        [Min(0.1f)] [SerializeField] float clickHitRadius = 0.65f;
        [Min(0.01f)] [SerializeField] float volleyShotDelay = 0.09f;
        [Min(1)] [SerializeField] int projectileCount = 1;
        [Range(0.05f, 1f)] [SerializeField] float additionalProjectileDamageMultiplier = 0.65f;
        [Min(0f)] [SerializeField] float areaRadius;
        [Min(1)] [SerializeField] int pierceCount = 1;
        [Range(0.05f, 1f)] [SerializeField] float chainDamageMultiplier = 1f;
        [Range(0f, 0.95f)] [SerializeField] float slowPercent;
        [Min(0f)] [SerializeField] float slowDuration;
        [Min(0f)] [SerializeField] float damageOverTime;
        [Min(0f)] [SerializeField] float damageOverTimeDuration;

        [Header("Active Skill")]
        [SerializeField] SummonerSkillId activeSkillId;
        [SerializeField] SummonerSkillTargeting targeting;
        [Min(1)] [SerializeField] int unlockLevel = 1;
        [Min(0.1f)] [SerializeField] float cooldown = 10f;
        [Min(0f)] [SerializeField] float activeDamageMultiplier = 1f;
        [Min(0f)] [SerializeField] float activeRadius = 1f;
        [Min(0f)] [SerializeField] float activeDuration;
        [SerializeField] MonsterAttribute attribute;
        [SerializeField] SkillRankProfile[] rankProfiles = new SkillRankProfile[3];

        public string SkillId => skillId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description ?? string.Empty;
        public SkillCategory Category => category;
        public SkillExecutionMode ExecutionMode => executionMode;
        public SummonerAttackArchetype AttackArchetype => attackArchetype;
        public Sprite ProjectileSprite => projectileSprite;
        public float BaseDamage => Mathf.Max(0.1f, baseDamage);
        public float AttacksPerSecond => Mathf.Max(0.1f, attacksPerSecond);
        public float AttackRange => Mathf.Max(0.1f, attackRange);
        public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
        public float ProjectileScale => Mathf.Max(0.01f, projectileScale);
        public float ClickDamage => Mathf.Max(0.1f, clickDamage);
        public float ClickAttacksPerSecond => Mathf.Max(0.1f, clickAttacksPerSecond);
        public float ClickHitRadius => Mathf.Max(0.1f, clickHitRadius);
        public float VolleyShotDelay => Mathf.Max(0.01f, volleyShotDelay);
        public int ProjectileCount => Mathf.Max(1, projectileCount);
        public float AdditionalProjectileDamageMultiplier =>
            Mathf.Clamp(additionalProjectileDamageMultiplier, 0.05f, 1f);
        public float AreaRadius => Mathf.Max(0f, areaRadius);
        public int PierceCount => Mathf.Max(1, pierceCount);
        public float ChainDamageMultiplier => Mathf.Clamp(chainDamageMultiplier, 0.05f, 1f);
        public float SlowPercent => Mathf.Clamp(slowPercent, 0f, 0.95f);
        public float SlowDuration => Mathf.Max(0f, slowDuration);
        public float DamageOverTime => Mathf.Max(0f, damageOverTime);
        public float DamageOverTimeDuration => Mathf.Max(0f, damageOverTimeDuration);
        public SummonerSkillId ActiveSkillId => activeSkillId;
        public SummonerSkillTargeting Targeting => targeting;
        public int UnlockLevel => Mathf.Max(1, unlockLevel);
        public float Cooldown => Mathf.Max(0.1f, cooldown);
        public float ActiveDamageMultiplier => Mathf.Max(0f, activeDamageMultiplier);
        public float ActiveRadius => Mathf.Max(0f, activeRadius);
        public float ActiveDuration => Mathf.Max(0f, activeDuration);
        public MonsterAttribute Attribute => attribute;

        public SkillRankProfile RankProfile(int rank)
        {
            if (rankProfiles == null || rankProfiles.Length == 0)
                return null;
            return rankProfiles[Mathf.Clamp(rank, 1, rankProfiles.Length) - 1];
        }

        public SummonerSkillDefinition BuildDefinition(
            int rank,
            string overrideName = null,
            string overrideDescription = null)
        {
            SkillRankProfile profile = RankProfile(rank);
            float damage = profile?.DamageMultiplier ?? 1f;
            float radius = profile?.RadiusMultiplier ?? 1f;
            float duration = profile?.DurationMultiplier ?? 1f;
            return new SummonerSkillDefinition(
                activeSkillId,
                string.IsNullOrWhiteSpace(overrideName) ? DisplayName : overrideName,
                UnlockLevel,
                targeting,
                Cooldown,
                ActiveDamageMultiplier * damage,
                ActiveRadius * radius,
                ActiveDuration * duration,
                string.IsNullOrWhiteSpace(overrideDescription)
                    ? Description
                    : overrideDescription);
        }
    }
}
