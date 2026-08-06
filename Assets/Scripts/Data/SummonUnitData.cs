using UnityEngine;

namespace CrossDefense.Data
{
    public enum SummonUnitRarity
    {
        Common,
        Rare,
        Legendary,
    }

    public enum SummonAttackStyle
    {
        Melee,
        Projectile,
        Area,
        Support,
        Piercing,
    }

    public enum SummonTargetPriority
    {
        Nearest,
        LowestHp,
        HighestHp,
        Farthest,
    }

    public enum Star3SkillMode
    {
        None,
        SelfArea,
        TargetArea,
        PiercingProjectile,
        AuraOverdrive,
    }

    /// <summary>
    /// 저장되는 내부 등급은 0부터 시작하지만 플레이어에게는 ★1부터 표시한다.
    /// </summary>
    public static class SummonRank
    {
        public const int MinInternalRank = 0;
        public const int MaxInternalRank = 2;
        public const int MergeMaterialCount = 2;

        public static int Clamp(int rank) => Mathf.Clamp(rank, MinInternalRank, MaxInternalRank);
        public static int ToStarCount(int rank) => Clamp(rank) + 1;
        public static string FormatStars(int rank) => $"★{ToStarCount(rank)}";
    }

    /// <summary>소환 확률과 필드 전투를 함께 정의하는 아군 슬라임 데이터.</summary>
    [CreateAssetMenu(fileName = "SummonUnitData", menuName = "Isekai Slime Summoner/Data/Summon Unit", order = 20)]
    public sealed class SummonUnitData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string unitId = "punch-slime";
        [SerializeField] string displayName = "주먹 슬라임";
        [SerializeField] SummonUnitRarity rarity = SummonUnitRarity.Common;
        [SerializeField] Sprite icon;
        [SerializeField] Sprite worldSprite;
        [SerializeField] Sprite projectileSprite;
        [SerializeField] Color tint = Color.white;

        [Header("Animation")]
        [SerializeField] Sprite[] moveFrames;
        [Min(1f)] [SerializeField] float moveAnimationFps = 9f;

        [Header("Summon Pool")]
        [Min(0)] [SerializeField] int weight = 100;
        [Min(1)] [SerializeField] int unlockLevel = 1;

        [Header("Combat")]
        [SerializeField] MonsterAttribute attribute = MonsterAttribute.None;
        [SerializeField] SummonAttackStyle attackStyle = SummonAttackStyle.Melee;
        [SerializeField] SummonTargetPriority targetPriority = SummonTargetPriority.Nearest;
        [Min(1f)] [SerializeField] float baseMaxHp = 100f;
        [Min(0.1f)] [SerializeField] float baseDamage = 8f;
        [Min(0.1f)] [SerializeField] float attacksPerSecond = 1f;
        [Min(0.1f)] [SerializeField] float attackRange = 0.85f;
        [Min(0f)] [SerializeField] float moveSpeed = 2.5f;
        [Min(0.1f)] [SerializeField] float projectileSpeed = 9f;
        [Min(0f)] [SerializeField] float areaRadius;
        [Range(0f, 0.95f)] [SerializeField] float slowPercent;
        [Min(0f)] [SerializeField] float slowDuration;
        [Min(0f)] [SerializeField] float damageOverTime;
        [Min(0f)] [SerializeField] float damageOverTimeDuration;
        [Min(0f)] [SerializeField] float stunDuration;
        [Min(1)] [SerializeField] int pierceCount = 1;
        [Range(0f, 1f)] [SerializeField] float supportAttackSpeedBonus;
        [Min(0f)] [SerializeField] float supportRadius = 2.5f;
        [Min(0.1f)] [SerializeField] float supportHealInterval = 2f;
        [SerializeField] float[] supportHealFractions = { 0.03f, 0.04f, 0.05f };

        [Header("Rank")]
        [SerializeField] Sprite[] rankWorldSprites;
        [SerializeField] float[] rankHpMultipliers = { 1f, 1.5f, 2.25f };
        [SerializeField] float[] rankDamageMultipliers = { 1f, 1.8f, 3.25f };
        [SerializeField] float[] rankAttackSpeedMultipliers = { 1f, 1.08f, 1.16f };
        [SerializeField] float[] rankScaleMultipliers = { 1f, 1.2f, 1.4f };
        [SerializeField] Sprite[] rankProjectileSprites;
        [SerializeField] float[] rankProjectileScaleMultipliers = { 1f, 1.15f, 1.3f };
        [SerializeField] string[] rankNames = { "★1", "★2", "★3" };

        [Header("Star 3 Skill")]
        [SerializeField] string star3SkillName;
        [SerializeField] Star3SkillMode star3SkillMode;
        [Min(0.1f)] [SerializeField] float star3SkillCooldown = 10f;
        [Min(0f)] [SerializeField] float star3SkillDamageMultiplier = 1f;
        [Min(0f)] [SerializeField] float star3SkillRadius = 1f;
        [Min(0f)] [SerializeField] float star3SkillDuration;
        [Min(0f)] [SerializeField] float star3SkillStrength = 1f;
        [Min(1)] [SerializeField] int star3SkillPierceCount = 1;
        [Range(0f, 0.95f)] [SerializeField] float star3SkillSlowPercent;
        [Min(0f)] [SerializeField] float star3SkillSlowDuration;
        [Min(0f)] [SerializeField] float star3SkillDotMultiplier;
        [Min(0f)] [SerializeField] float star3SkillDotDuration;
        [Min(0f)] [SerializeField] float star3SkillStunDuration;
        [Min(0.1f)] [SerializeField] float star3SkillVisualScale = 1f;
        [SerializeField] Sprite star3SkillEffectSprite;
        [SerializeField] Sprite[] star3SkillEffectFrames;

        public string UnitId => unitId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public SummonUnitRarity Rarity => rarity;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite != null ? worldSprite : icon;
        public Sprite ProjectileSprite => projectileSprite;
        public Color Tint => tint;
        public Sprite[] MoveFrames => moveFrames;
        public float MoveAnimationFps => moveAnimationFps;
        public int Weight => weight;
        public int UnlockLevel => Mathf.Max(1, unlockLevel);
        public bool IsUnlockedAtLevel(int summonerLevel) => summonerLevel >= UnlockLevel;
        public MonsterAttribute Attribute => attribute;
        public SummonAttackStyle AttackStyle => attackStyle;
        public SummonTargetPriority TargetPriority => targetPriority;
        public float BaseMaxHp => baseMaxHp;
        public float BaseDamage => baseDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
        public float ProjectileSpeed => projectileSpeed;
        public float AreaRadius => areaRadius;
        public float SlowPercent => slowPercent;
        public float SlowDuration => slowDuration;
        public float DamageOverTime => damageOverTime;
        public float DamageOverTimeDuration => damageOverTimeDuration;
        public float StunDuration => stunDuration;
        public int PierceCount => pierceCount;
        public float SupportAttackSpeedBonus => supportAttackSpeedBonus;
        public float SupportRadius => supportRadius;
        public float SupportHealInterval => Mathf.Max(0.1f, supportHealInterval);
        public string Star3SkillName => star3SkillName;
        public Star3SkillMode Star3SkillModeValue => star3SkillMode;
        public float Star3SkillCooldown => star3SkillCooldown;
        public float Star3SkillDamageMultiplier => star3SkillDamageMultiplier;
        public float Star3SkillRadius => star3SkillRadius;
        public float Star3SkillDuration => star3SkillDuration;
        public float Star3SkillStrength => star3SkillStrength;
        public int Star3SkillPierceCount => star3SkillPierceCount;
        public float Star3SkillSlowPercent => star3SkillSlowPercent;
        public float Star3SkillSlowDuration => star3SkillSlowDuration;
        public float Star3SkillDotMultiplier => star3SkillDotMultiplier;
        public float Star3SkillDotDuration => star3SkillDotDuration;
        public float Star3SkillStunDuration => star3SkillStunDuration;
        public float Star3SkillVisualScale => star3SkillVisualScale;
        public Sprite Star3SkillEffectSprite => star3SkillEffectSprite;
        public Sprite[] Star3SkillEffectFrames => star3SkillEffectFrames;
        public bool HasStar3Skill => star3SkillMode != Star3SkillMode.None;

        public Sprite WorldSpriteAtRank(int rank)
        {
            int clamped = SummonRank.Clamp(rank);
            if (rankWorldSprites != null &&
                clamped < rankWorldSprites.Length &&
                rankWorldSprites[clamped] != null)
            {
                return rankWorldSprites[clamped];
            }

            return WorldSprite;
        }

        public Sprite ProjectileSpriteAtRank(int rank)
        {
            int clamped = SummonRank.Clamp(rank);
            if (rankProjectileSprites != null &&
                clamped < rankProjectileSprites.Length &&
                rankProjectileSprites[clamped] != null)
            {
                return rankProjectileSprites[clamped];
            }

            return projectileSprite;
        }

        public float MaxHpAtRank(int rank) => baseMaxHp * RankValue(rankHpMultipliers, rank, 1f);
        public float DamageAtRank(int rank) => baseDamage * RankValue(rankDamageMultipliers, rank, 1f);
        public float AttacksPerSecondAtRank(int rank) => attacksPerSecond * RankValue(rankAttackSpeedMultipliers, rank, 1f);
        public float ScaleAtRank(int rank) => RankValue(rankScaleMultipliers, rank, 1f);
        public float ProjectileScaleAtRank(int rank) =>
            RankValue(rankProjectileScaleMultipliers, rank, 1f);
        public float SupportHealFractionAtRank(int rank) =>
            Mathf.Max(0f, RankValue(supportHealFractions, rank, 0f));
        public string NameAtRank(int rank)
        {
            int clamped = SummonRank.Clamp(rank);
            string suffix = rankNames != null && clamped < rankNames.Length
                ? rankNames[clamped]
                : SummonRank.FormatStars(clamped);
            return $"{DisplayName} {suffix}";
        }

        public static SummonUnitData CreatePrototype(
            string id,
            string displayName,
            SummonUnitRarity rarity,
            int weight = 100,
            Sprite icon = null,
            MonsterAttribute attribute = MonsterAttribute.None,
            SummonAttackStyle attackStyle = SummonAttackStyle.Melee,
            float damage = 8f,
            float attacksPerSecond = 1f,
            float range = 0.85f,
            float moveSpeed = 2.5f,
            Color? tint = null,
            float maxHp = 100f)
        {
            var data = CreateInstance<SummonUnitData>();
            data.unitId = id;
            data.displayName = displayName;
            data.rarity = rarity;
            data.weight = weight;
            data.icon = icon;
            data.worldSprite = icon;
            data.attribute = attribute;
            data.attackStyle = attackStyle;
            data.baseMaxHp = Mathf.Max(1f, maxHp);
            data.baseDamage = Mathf.Max(0.1f, damage);
            data.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
            data.attackRange = Mathf.Max(0.1f, range);
            data.moveSpeed = Mathf.Max(0f, moveSpeed);
            data.tint = tint ?? Color.white;
            data.unlockLevel = 1;
            data.hideFlags = HideFlags.HideAndDontSave;
            return data;
        }

        public void ConfigurePrototypeEffects(
            Sprite projectile,
            float area,
            float slow,
            float slowSeconds,
            float dot,
            float dotSeconds,
            int pierce,
            float supportBonus = 0f,
            float supportRange = 2.5f,
            float supportHealSeconds = 2f,
            float[] supportHealByRank = null,
            float stunSeconds = 0f)
        {
            projectileSprite = projectile;
            areaRadius = Mathf.Max(0f, area);
            slowPercent = Mathf.Clamp(slow, 0f, 0.95f);
            slowDuration = Mathf.Max(0f, slowSeconds);
            damageOverTime = Mathf.Max(0f, dot);
            damageOverTimeDuration = Mathf.Max(0f, dotSeconds);
            stunDuration = Mathf.Max(0f, stunSeconds);
            pierceCount = Mathf.Max(1, pierce);
            supportAttackSpeedBonus = Mathf.Clamp01(supportBonus);
            supportRadius = Mathf.Max(0f, supportRange);
            supportHealInterval = Mathf.Max(0.1f, supportHealSeconds);
            if (supportHealByRank != null && supportHealByRank.Length > 0)
                supportHealFractions = supportHealByRank;
        }

        public void ConfigurePrototypeAnimation(Sprite[] frames, float framesPerSecond = 9f)
        {
            moveFrames = frames;
            moveAnimationFps = Mathf.Max(1f, framesPerSecond);
        }

        public void ConfigurePrototypeRankSprites(Sprite star2Sprite, Sprite star3Sprite)
        {
            rankWorldSprites = new[]
            {
                WorldSprite,
                star2Sprite,
                star3Sprite,
            };
        }

        public void ConfigurePrototypeRankProjectiles(Sprite[] projectiles)
        {
            rankProjectileSprites = new Sprite[SummonRank.MaxInternalRank + 1];
            for (int rank = SummonRank.MinInternalRank; rank <= SummonRank.MaxInternalRank; rank++)
            {
                rankProjectileSprites[rank] =
                    projectiles != null && rank < projectiles.Length && projectiles[rank] != null
                        ? projectiles[rank]
                        : projectileSprite;
            }
        }

        public void ConfigurePrototypeUnlockLevel(int level) =>
            unlockLevel = Mathf.Max(1, level);

        public void ConfigurePrototypeStar3Skill(
            string skillName,
            Star3SkillMode mode,
            float cooldown,
            float damageMultiplier,
            float radius,
            float duration,
            float strength,
            int pierce,
            Sprite effectSprite,
            float visualScale = 1f,
            float skillSlowPercent = 0f,
            float skillSlowDuration = 0f,
            float skillDotMultiplier = 0f,
            float skillDotDuration = 0f,
            float skillStunDuration = 0f)
        {
            star3SkillName = skillName ?? string.Empty;
            star3SkillMode = mode;
            star3SkillCooldown = Mathf.Max(0.1f, cooldown);
            star3SkillDamageMultiplier = Mathf.Max(0f, damageMultiplier);
            star3SkillRadius = Mathf.Max(0f, radius);
            star3SkillDuration = Mathf.Max(0f, duration);
            star3SkillStrength = Mathf.Max(0f, strength);
            star3SkillPierceCount = Mathf.Max(1, pierce);
            star3SkillEffectSprite = effectSprite;
            star3SkillVisualScale = Mathf.Max(0.1f, visualScale);
            star3SkillSlowPercent = Mathf.Clamp(skillSlowPercent, 0f, 0.95f);
            star3SkillSlowDuration = Mathf.Max(0f, skillSlowDuration);
            star3SkillDotMultiplier = Mathf.Max(0f, skillDotMultiplier);
            star3SkillDotDuration = Mathf.Max(0f, skillDotDuration);
            star3SkillStunDuration = Mathf.Max(0f, skillStunDuration);
        }

        public void ConfigurePrototypeStar3SkillFrames(Sprite[] frames) =>
            star3SkillEffectFrames = frames;

        static float RankValue(float[] values, int rank, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(rank, SummonRank.MinInternalRank,
                Mathf.Min(SummonRank.MaxInternalRank, values.Length - 1))];
        }
    }
}
