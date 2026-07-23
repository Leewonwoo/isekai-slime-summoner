using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>웨이브 사이 소환수 복귀 포메이션의 데이터 조정값.</summary>
    [Serializable]
    public sealed class SummonFormationSettings
    {
        [SerializeField] bool enabled = true;
        [Min(0.1f)] [SerializeField] float returnSpeed = 3.4f;
        [Min(0.01f)] [SerializeField] float stoppingDistance = 0.06f;
        [Min(0.5f)] [SerializeField] float innerRadius = 1.15f;
        [Min(0.1f)] [SerializeField] float ringSpacing = 0.7f;
        [Range(3, 8)] [SerializeField] int slotsPerRing = 6;
        [Range(-180f, 180f)] [SerializeField] float rotationOffsetDegrees = 90f;

        public bool Enabled => enabled;
        public float ReturnSpeed => Mathf.Max(0.1f, returnSpeed);
        public float StoppingDistance => Mathf.Max(0.01f, stoppingDistance);
        public float InnerRadius => Mathf.Max(0.5f, innerRadius);
        public float RingSpacing => Mathf.Max(0.1f, ringSpacing);
        public int SlotsPerRing => Mathf.Clamp(slotsPerRing, 3, 8);
        public float RotationOffsetDegrees => Mathf.Clamp(rotationOffsetDegrees, -180f, 180f);
    }

    /// <summary>역할 우선순위와 인덱스로 소환사 주변의 안정적인 동심원 슬롯을 계산한다.</summary>
    public static class SummonFormationPlanner
    {
        public static Vector2 GetSlot(
            Vector2 center,
            int slotIndex,
            int totalUnits,
            SummonFormationSettings settings)
        {
            if (settings == null) return center;
            return GetSlot(center, slotIndex, totalUnits, settings.InnerRadius, settings.RingSpacing,
                settings.SlotsPerRing, settings.RotationOffsetDegrees);
        }

        public static Vector2 GetSlot(
            Vector2 center,
            int slotIndex,
            int totalUnits,
            float innerRadius,
            float ringSpacing,
            int slotsPerRing,
            float rotationOffsetDegrees)
        {
            int safeTotal = Mathf.Max(1, totalUnits);
            int safeIndex = Mathf.Clamp(slotIndex, 0, safeTotal - 1);
            int safeSlotsPerRing = Mathf.Clamp(slotsPerRing, 3, 8);
            int ringIndex = safeIndex / safeSlotsPerRing;
            int indexInRing = safeIndex % safeSlotsPerRing;
            int unitsBeforeRing = ringIndex * safeSlotsPerRing;
            int unitsInRing = Mathf.Min(safeSlotsPerRing, safeTotal - unitsBeforeRing);
            float angleStep = 360f / Mathf.Max(1, unitsInRing);
            float stagger = (ringIndex & 1) == 0 ? 0f : angleStep * 0.5f;
            float angle = (rotationOffsetDegrees + stagger + angleStep * indexInRing) * Mathf.Deg2Rad;
            float radius = Mathf.Max(0.5f, innerRadius) + ringIndex * Mathf.Max(0.1f, ringSpacing);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }

        public static int GetRolePriority(SummonAttackStyle style)
        {
            return style switch
            {
                SummonAttackStyle.Support => 0,
                SummonAttackStyle.Projectile or SummonAttackStyle.Piercing => 1,
                SummonAttackStyle.Area => 2,
                _ => 3,
            };
        }
    }
}
