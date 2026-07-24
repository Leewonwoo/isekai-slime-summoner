using UnityEngine;

namespace CrossDefense.Data
{
    public enum EquipmentSlot { Weapon, Armor, Accessory }
    public enum EquipmentEffect { SummonerDamage, SummonerMaxHp, SummonerAttackSpeed, CriticalChance, JackpotChance }

    [CreateAssetMenu(fileName = "Equipment", menuName = "Isekai Slime Summoner/Data/Equipment", order = 40)]
    public sealed class EquipmentData : ScriptableObject
    {
        [SerializeField] string equipmentId = "equipment";
        [SerializeField] string displayName = "신물";
        [SerializeField] string description;
        [SerializeField] EquipmentSlot slot;
        [SerializeField] EquipmentEffect effect;
        [Min(0f)] [SerializeField] float value = 0.1f;
        [Min(1)] [SerializeField] int price = 80;
        [SerializeField] Sprite icon;

        public string EquipmentId => equipmentId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        public string Description => description;
        public EquipmentSlot Slot => slot;
        public EquipmentEffect Effect => effect;
        public float Value => Mathf.Max(0f, value);
        public int Price => Mathf.Max(1, price);
        public Sprite Icon => icon;

        public static EquipmentData CreateRuntime(string id, string title, EquipmentSlot itemSlot,
            EquipmentEffect itemEffect, float amount, int goldPrice, string detail)
        {
            var data = CreateInstance<EquipmentData>();
            data.hideFlags = HideFlags.HideAndDontSave;
            data.equipmentId = id;
            data.displayName = title;
            data.slot = itemSlot;
            data.effect = itemEffect;
            data.value = amount;
            data.price = goldPrice;
            data.description = detail;
            return data;
        }
    }

}
