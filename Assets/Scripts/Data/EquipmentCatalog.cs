using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    [CreateAssetMenu(fileName = "EquipmentCatalog", menuName = "Isekai Slime Summoner/Data/Equipment Catalog", order = 41)]
    public sealed class EquipmentCatalog : ScriptableObject
    {
        [SerializeField] List<EquipmentData> equipment = new();
        public IReadOnlyList<EquipmentData> Equipment => equipment;

        public EquipmentData Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < equipment.Count; i++)
                if (equipment[i] != null && equipment[i].EquipmentId == id) return equipment[i];
            return null;
        }

        public static EquipmentCatalog CreateRuntimeDefault()
        {
            var catalog = CreateInstance<EquipmentCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.equipment = new List<EquipmentData>
            {
                EquipmentData.CreateRuntime("weapon-oak", "참나무 지팡이", EquipmentSlot.Weapon, EquipmentEffect.SummonerDamage, 0.08f, 80, "소환사 공격력 +8%"),
                EquipmentData.CreateRuntime("weapon-ember", "불씨 지팡이", EquipmentSlot.Weapon, EquipmentEffect.SummonerDamage, 0.14f, 120, "소환사 공격력 +14%"),
                EquipmentData.CreateRuntime("weapon-arcane", "비전 지팡이", EquipmentSlot.Weapon, EquipmentEffect.SummonerDamage, 0.22f, 180, "소환사 공격력 +22%"),
                EquipmentData.CreateRuntime("armor-leather", "가죽 외투", EquipmentSlot.Armor, EquipmentEffect.SummonerMaxHp, 0.10f, 80, "소환사 최대 HP +10%"),
                EquipmentData.CreateRuntime("armor-bark", "나무껍질 갑옷", EquipmentSlot.Armor, EquipmentEffect.SummonerMaxHp, 0.18f, 120, "소환사 최대 HP +18%"),
                EquipmentData.CreateRuntime("armor-guardian", "수호자 판금", EquipmentSlot.Armor, EquipmentEffect.SummonerMaxHp, 0.28f, 180, "소환사 최대 HP +28%"),
                EquipmentData.CreateRuntime("accessory-swift", "신속의 반지", EquipmentSlot.Accessory, EquipmentEffect.SummonerAttackSpeed, 0.08f, 80, "소환사 공격속도 +8%"),
                EquipmentData.CreateRuntime("accessory-hunter", "사냥꾼 문장", EquipmentSlot.Accessory, EquipmentEffect.CriticalChance, 0.05f, 120, "치명타 확률 +5%"),
                EquipmentData.CreateRuntime("accessory-lucky", "행운의 부적", EquipmentSlot.Accessory, EquipmentEffect.JackpotChance, 0.03f, 160, "★2 직행 확률 +3%"),
            };
            return catalog;
        }
    }
}
