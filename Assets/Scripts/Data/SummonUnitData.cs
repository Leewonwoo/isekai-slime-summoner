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

    /// <summary>소환 확률과 필드 전투를 함께 정의하는 아군 슬라임 데이터.</summary>
    [CreateAssetMenu(fileName = "SummonUnitData", menuName = "Cross Defense/Data/Summon Unit", order = 20)]
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

        [Header("Summon Pool")]
        [Min(0)] [SerializeField] int weight = 100;
        [SerializeField] bool unlockedByDefault = true;

        [Header("Combat")]
        [SerializeField] MonsterAttribute attribute = MonsterAttribute.None;
        [SerializeField] SummonAttackStyle attackStyle = SummonAttackStyle.Melee;
        [SerializeField] SummonTargetPriority targetPriority = SummonTargetPriority.Nearest;
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
        [Min(1)] [SerializeField] int pierceCount = 1;
        [Range(0f, 1f)] [SerializeField] float supportAttackSpeedBonus;
        [Min(0f)] [SerializeField] float supportRadius = 2.5f;

        [Header("Rank")]
        [SerializeField] float[] rankDamageMultipliers = { 1f, 1.8f, 3.25f, 5.5f };
        [SerializeField] float[] rankAttackSpeedMultipliers = { 1f, 1.08f, 1.16f, 1.25f };
        [SerializeField] float[] rankScaleMultipliers = { 1f, 1.08f, 1.16f, 1.25f };
        [SerializeField] string[] rankNames = { "기본", "★1", "★2", "★3" };

        public string UnitId => unitId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public SummonUnitRarity Rarity => rarity;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite != null ? worldSprite : icon;
        public Sprite ProjectileSprite => projectileSprite;
        public Color Tint => tint;
        public int Weight => weight;
        public bool UnlockedByDefault => unlockedByDefault;
        public MonsterAttribute Attribute => attribute;
        public SummonAttackStyle AttackStyle => attackStyle;
        public SummonTargetPriority TargetPriority => targetPriority;
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
        public int PierceCount => pierceCount;
        public float SupportAttackSpeedBonus => supportAttackSpeedBonus;
        public float SupportRadius => supportRadius;

        public float DamageAtRank(int rank) => baseDamage * RankValue(rankDamageMultipliers, rank, 1f);
        public float AttacksPerSecondAtRank(int rank) => attacksPerSecond * RankValue(rankAttackSpeedMultipliers, rank, 1f);
        public float ScaleAtRank(int rank) => RankValue(rankScaleMultipliers, rank, 1f);
        public string NameAtRank(int rank)
        {
            int clamped = Mathf.Clamp(rank, 0, 3);
            string suffix = rankNames != null && clamped < rankNames.Length ? rankNames[clamped] : $"★{clamped}";
            return clamped == 0 ? DisplayName : $"{DisplayName} {suffix}";
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
            Color? tint = null)
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
            data.baseDamage = Mathf.Max(0.1f, damage);
            data.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
            data.attackRange = Mathf.Max(0.1f, range);
            data.moveSpeed = Mathf.Max(0f, moveSpeed);
            data.tint = tint ?? Color.white;
            data.unlockedByDefault = true;
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
            float supportRange = 2.5f)
        {
            projectileSprite = projectile;
            areaRadius = Mathf.Max(0f, area);
            slowPercent = Mathf.Clamp(slow, 0f, 0.95f);
            slowDuration = Mathf.Max(0f, slowSeconds);
            damageOverTime = Mathf.Max(0f, dot);
            damageOverTimeDuration = Mathf.Max(0f, dotSeconds);
            pierceCount = Mathf.Max(1, pierce);
            supportAttackSpeedBonus = Mathf.Clamp01(supportBonus);
            supportRadius = Mathf.Max(0f, supportRange);
        }

        static float RankValue(float[] values, int rank, float fallback)
        {
            if (values == null || values.Length == 0) return fallback;
            return values[Mathf.Clamp(rank, 0, Mathf.Min(3, values.Length - 1))];
        }
    }
}
