using UnityEngine;

namespace CrossDefense.Data
{
    /// <summary>Enemy archetype and reusable balance profile.</summary>
    public enum MonsterShape
    {
        Grunt,
        Scout,
        Bruiser,
        Raider,
        Boss,
    }

    /// <summary>Monster attribute used by the elemental matchup rules.</summary>
    public enum MonsterAttribute
    {
        None = 0,
        Fire = 1,
        Ice = 2,
        Nature = 3,
        Lightning = 4,
        Water = 5,
        Wind = 6,
    }

    public enum MonsterAttackStyle
    {
        Melee,
        Projectile,
    }

    [CreateAssetMenu(fileName = "MonsterData", menuName = "Isekai Slime Summoner/Data/Monster Profile", order = 10)]
    public sealed class MonsterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string monsterId = "monster-basic";
        [SerializeField] string displayName = "Goblin Grunt";
        [SerializeField] MonsterShape shape = MonsterShape.Grunt;
        [SerializeField] MonsterAttribute attribute = MonsterAttribute.None;
        [SerializeField] Sprite sprite;
        [SerializeField] Sprite[] moveFrames;
        [Min(1f)] [SerializeField] float moveAnimationFps = 12f;
        [SerializeField] Sprite[] attackFrames;
        [Min(1f)] [SerializeField] float attackAnimationFps = 18f;

        [Header("Base Balance")]
        [Min(1)] [SerializeField] int baseHp = 100;
        [Min(0.1f)] [SerializeField] float moveSpeed = 1f;
        [Min(0)] [SerializeField] int contactDamage = 1;
        [Min(0.1f)] [SerializeField] float attacksPerSecond = 1f;
        [Min(0.1f)] [SerializeField] float attackRange = 0.55f;
        [SerializeField] MonsterAttackStyle attackStyle;
        [SerializeField] Sprite projectileSprite;
        [Min(0.1f)] [SerializeField] float projectileSpeed = 5f;
        [Min(0.1f)] [SerializeField] float projectileScale = 0.45f;
        [Min(0)] [SerializeField] int rewardGold = 1;
        [Min(0.1f)] [SerializeField] float sizeMultiplier = 1f;

        public string MonsterId => monsterId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public MonsterShape Shape => shape;
        public MonsterAttribute Attribute => attribute;
        public Sprite Sprite => sprite;
        public Sprite[] MoveFrames => moveFrames;
        public float MoveAnimationFps => moveAnimationFps;
        public Sprite[] AttackFrames => attackFrames;
        public float AttackAnimationFps => attackAnimationFps;
        public int BaseHp => baseHp;
        public float MoveSpeed => moveSpeed;
        public int ContactDamage => contactDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackRange => attackRange;
        public MonsterAttackStyle AttackStyle => attackStyle;
        public Sprite ProjectileSprite => projectileSprite;
        public float ProjectileSpeed => Mathf.Max(0.1f, projectileSpeed);
        public float ProjectileScale => Mathf.Max(0.1f, projectileScale);
        public int RewardGold => rewardGold;
        public float SizeMultiplier => sizeMultiplier;

        public static MonsterData CreatePrototype(string id, string displayName, MonsterShape shape, MonsterAttribute attribute,
            int hp, float speed, int contactDamage, int rewardGold, Sprite sprite = null,
            Sprite[] moveFrames = null, float moveAnimationFps = 12f,
            float attacksPerSecond = 1f, float attackRange = 0.55f,
            Sprite[] attackFrames = null, float attackAnimationFps = 18f,
            float sizeMultiplier = 1f,
            MonsterAttackStyle attackStyle = MonsterAttackStyle.Melee,
            Sprite projectileSprite = null,
            float projectileSpeed = 5f,
            float projectileScale = 0.45f)
        {
            var data = CreateInstance<MonsterData>();
            data.monsterId = id;
            data.displayName = displayName;
            data.shape = shape;
            data.attribute = attribute;
            data.sprite = sprite;
            data.moveFrames = moveFrames;
            data.moveAnimationFps = Mathf.Max(1f, moveAnimationFps);
            data.attackFrames = attackFrames;
            data.attackAnimationFps = Mathf.Max(1f, attackAnimationFps);
            data.baseHp = hp;
            data.moveSpeed = speed;
            data.contactDamage = contactDamage;
            data.attacksPerSecond = Mathf.Max(0.1f, attacksPerSecond);
            data.attackRange = Mathf.Max(0.1f, attackRange);
            data.attackStyle = attackStyle;
            data.projectileSprite = projectileSprite;
            data.projectileSpeed = Mathf.Max(0.1f, projectileSpeed);
            data.projectileScale = Mathf.Max(0.1f, projectileScale);
            data.rewardGold = rewardGold;
            data.sizeMultiplier = Mathf.Max(0.1f, sizeMultiplier);
            return data;
        }
    }
}
