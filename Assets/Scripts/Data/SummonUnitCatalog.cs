using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    [CreateAssetMenu(
        fileName = "SummonUnitCatalog",
        menuName = "Isekai Slime Summoner/Data/Summon Unit Catalog",
        order = 21)]
    public sealed class SummonUnitCatalog : ScriptableObject
    {
        [SerializeField] List<SummonUnitData> units = new();

        public IReadOnlyList<SummonUnitData> Units => units;

        public SummonUnitData Find(string unitId)
        {
            if (string.IsNullOrWhiteSpace(unitId)) return null;

            for (int i = 0; i < units.Count; i++)
            {
                SummonUnitData unit = units[i];
                if (unit != null && unit.UnitId == unitId)
                    return unit;
            }

            return null;
        }

        public bool Validate(out string error)
        {
            var ids = new HashSet<string>();
            for (int i = 0; i < units.Count; i++)
            {
                SummonUnitData unit = units[i];
                if (unit == null)
                {
                    error = $"소환수 카탈로그 {i + 1}번 항목이 비어 있습니다.";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(unit.UnitId))
                {
                    error = $"{unit.name}의 unitId가 비어 있습니다.";
                    return false;
                }

                if (!ids.Add(unit.UnitId))
                {
                    error = $"중복된 소환수 unitId가 있습니다: {unit.UnitId}";
                    return false;
                }

                if (unit.WorldSprite == null)
                {
                    error = $"{unit.UnitId}의 월드 스프라이트가 비어 있습니다.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
