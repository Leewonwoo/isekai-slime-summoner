using UnityEngine;

namespace CrossDefense.Data
{
    /// <summary>Enemy archetype and reusable balance profile.</summary>
    public enum MonsterShape
    {
        BasicSlime,
        SpitterSlime,
        TankSlime,
        SplitSlime,
        Boss,
    }

    /// <summary>Monster attribute used by the elemental matchup rules.</summary>
    public enum MonsterAttribute
    {
        None,
        Fire,
        Ice,
        Nature,
    }

    [CreateAssetMenu(fileName = "MonsterData", menuName = "Cross Defense/Data/Monster Profile", order = 10)]
    public sealed class MonsterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] string monsterId = "monster-basic";
        [SerializeField] string displayName = "Basic Slime";
        [SerializeField] MonsterShape shape = MonsterShape.BasicSlime;
        [SerializeField] MonsterAttribute attribute = MonsterAttribute.None;
        [SerializeField] Sprite sprite;

        [Header("Base Balance")]
        [Min(1)] [SerializeField] int baseHp = 100;
        [Min(0.1f)] [SerializeField] float moveSpeed = 1f;
        [Min(0)] [SerializeField] int contactDamage = 1;
        [Min(0)] [SerializeField] int rewardGold = 1;
        [Min(0.1f)] [SerializeField] float sizeMultiplier = 1f;

        public string MonsterId => monsterId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public MonsterShape Shape => shape;
        public MonsterAttribute Attribute => attribute;
        public Sprite Sprite => sprite;
        public int BaseHp => baseHp;
        public float MoveSpeed => moveSpeed;
        public int ContactDamage => contactDamage;
        public int RewardGold => rewardGold;
        public float SizeMultiplier => sizeMultiplier;

        public static MonsterData CreatePrototype(string id, string displayName, MonsterShape shape, MonsterAttribute attribute,
            int hp, float speed, int contactDamage, int rewardGold, Sprite sprite = null)
        {
            var data = CreateInstance<MonsterData>();
            data.monsterId = id;
            data.displayName = displayName;
            data.shape = shape;
            data.attribute = attribute;
            data.sprite = sprite;
            data.baseHp = hp;
            data.moveSpeed = speed;
            data.contactDamage = contactDamage;
            data.rewardGold = rewardGold;
            return data;
        }
    }
}
