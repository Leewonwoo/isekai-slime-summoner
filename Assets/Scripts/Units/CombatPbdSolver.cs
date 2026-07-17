using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Units
{
    public enum CombatPbdTeam
    {
        SummonedUnit,
        Monster,
    }

    /// <summary>전투 개체 한 명의 위치 제약 입력. Transform과 분리해 EditMode에서 계산만 검증한다.</summary>
    public struct CombatPbdBody
    {
        public Vector2 Position;
        public Vector2 ReferencePosition;
        public float Radius;
        public float InverseMass;
        public float OpposingContactDistance;
        public CombatPbdTeam Team;

        public CombatPbdBody(
            Vector2 position,
            float radius,
            float inverseMass,
            CombatPbdTeam team,
            float opposingContactDistance = 0f)
        {
            Position = position;
            ReferencePosition = position;
            Radius = Mathf.Max(0f, radius);
            InverseMass = Mathf.Max(0f, inverseMass);
            Team = team;
            OpposingContactDistance = Mathf.Max(0f, opposingContactDistance);
        }
    }

    /// <summary>모바일 전투용 PBD 접촉 제약 설정. 전투 수치와 독립적으로 Inspector에서 조정한다.</summary>
    [Serializable]
    public sealed class CombatPbdSettings
    {
        [SerializeField] bool enabled = true;
        [Range(1, 4)] [SerializeField] int solverIterations = 1;
        [Range(0.1f, 1f)] [SerializeField] float correctionStrength = 0.38f;
        [Range(0.1f, 1.25f)] [SerializeField] float summonedUnitSpacingScale = 0.82f;
        [Range(0.1f, 1.25f)] [SerializeField] float monsterSpacingScale = 0.78f;
        [Range(0.1f, 1f)] [SerializeField] float opposingSpacingScale = 0.52f;
        [Min(0f)] [SerializeField] float penetrationSlop = 0.06f;
        [Min(0.01f)] [SerializeField] float maxCorrectionSpeed = 0.9f;
        [Min(0.01f)] [SerializeField] float summonedUnitInverseMass = 0.65f;
        [Min(0.01f)] [SerializeField] float monsterInverseMass = 1f;

        public bool Enabled => enabled;
        public int SolverIterations => Mathf.Clamp(solverIterations, 1, 4);
        public float CorrectionStrength => Mathf.Clamp(correctionStrength, 0.1f, 1f);
        public float SummonedUnitSpacingScale => Mathf.Max(0.1f, summonedUnitSpacingScale);
        public float MonsterSpacingScale => Mathf.Max(0.1f, monsterSpacingScale);
        public float OpposingSpacingScale => Mathf.Clamp(opposingSpacingScale, 0.1f, 1f);
        public float PenetrationSlop => Mathf.Max(0f, penetrationSlop);
        public float MaxCorrectionSpeed => Mathf.Max(0.01f, maxCorrectionSpeed);
        public float SummonedUnitInverseMass => Mathf.Max(0.01f, summonedUnitInverseMass);
        public float MonsterInverseMass => Mathf.Max(0.01f, monsterInverseMass);
    }

    /// <summary>
    /// 속도/힘을 누적하지 않고 침투한 위치만 보정하는 결정론적 2D PBD 솔버.
    /// 개체 수가 작은 현재 전투 규모에서는 O(n²) 쌍 검사가 공간 해시보다 단순하고 충분히 저렴하다.
    /// </summary>
    public static class CombatPbdSolver
    {
        const float MinimumDistance = 0.0001f;

        public static void Solve(IList<CombatPbdBody> bodies, CombatPbdSettings settings)
            => Solve(bodies, settings, 1f / 60f);

        public static void Solve(
            IList<CombatPbdBody> bodies,
            CombatPbdSettings settings,
            float deltaTime)
        {
            if (settings == null || !settings.Enabled) return;
            Solve(
                bodies,
                settings.SolverIterations,
                settings.CorrectionStrength,
                settings.SummonedUnitSpacingScale,
                settings.MonsterSpacingScale,
                settings.OpposingSpacingScale,
                settings.PenetrationSlop,
                settings.MaxCorrectionSpeed * Mathf.Clamp(deltaTime, 0f, 0.05f));
        }

        public static void Solve(
            IList<CombatPbdBody> bodies,
            int iterations,
            float correctionStrength,
            float summonedUnitSpacingScale,
            float monsterSpacingScale,
            float opposingSpacingScale)
            => Solve(
                bodies,
                iterations,
                correctionStrength,
                summonedUnitSpacingScale,
                monsterSpacingScale,
                opposingSpacingScale,
                0f,
                float.PositiveInfinity);

        public static void Solve(
            IList<CombatPbdBody> bodies,
            int iterations,
            float correctionStrength,
            float summonedUnitSpacingScale,
            float monsterSpacingScale,
            float opposingSpacingScale,
            float penetrationSlop,
            float maxCorrectionDistance)
        {
            if (bodies == null || bodies.Count < 2) return;

            int safeIterations = Mathf.Clamp(iterations, 1, 4);
            float strength = Mathf.Clamp01(correctionStrength);
            float safeSlop = Mathf.Max(0f, penetrationSlop);
            float safeMaxCorrection = Mathf.Max(0f, maxCorrectionDistance);
            for (int iteration = 0; iteration < safeIterations; iteration++)
            {
                for (int firstIndex = 0; firstIndex < bodies.Count - 1; firstIndex++)
                {
                    for (int secondIndex = firstIndex + 1; secondIndex < bodies.Count; secondIndex++)
                        SolvePair(bodies, firstIndex, secondIndex, strength,
                            summonedUnitSpacingScale, monsterSpacingScale, opposingSpacingScale,
                            safeSlop, safeMaxCorrection);
                }
            }
        }

        static void SolvePair(
            IList<CombatPbdBody> bodies,
            int firstIndex,
            int secondIndex,
            float strength,
            float summonedUnitSpacingScale,
            float monsterSpacingScale,
            float opposingSpacingScale,
            float penetrationSlop,
            float maxCorrectionDistance)
        {
            CombatPbdBody first = bodies[firstIndex];
            CombatPbdBody second = bodies[secondIndex];
            float inverseMassSum = first.InverseMass + second.InverseMass;
            if (inverseMassSum <= Mathf.Epsilon) return;

            float targetDistance = GetTargetDistance(first, second,
                summonedUnitSpacingScale, monsterSpacingScale, opposingSpacingScale);
            if (targetDistance <= 0f) return;

            Vector2 delta = second.Position - first.Position;
            float distance = delta.magnitude;
            float penetration = targetDistance - distance - penetrationSlop;
            if (penetration <= 0f) return;

            Vector2 normal = distance > MinimumDistance
                ? delta / distance
                : GetDeterministicFallbackNormal(firstIndex, secondIndex);
            Vector2 correction = normal * (penetration * strength / inverseMassSum);
            first.Position -= correction * first.InverseMass;
            second.Position += correction * second.InverseMass;
            first.Position = ClampCorrection(first.Position, first.ReferencePosition, maxCorrectionDistance);
            second.Position = ClampCorrection(second.Position, second.ReferencePosition, maxCorrectionDistance);
            bodies[firstIndex] = first;
            bodies[secondIndex] = second;
        }

        static Vector2 ClampCorrection(Vector2 position, Vector2 reference, float maxDistance)
        {
            if (float.IsPositiveInfinity(maxDistance)) return position;
            Vector2 offset = position - reference;
            float max = Mathf.Max(0f, maxDistance);
            return offset.sqrMagnitude > max * max
                ? reference + offset.normalized * max
                : position;
        }

        static float GetTargetDistance(
            CombatPbdBody first,
            CombatPbdBody second,
            float summonedUnitSpacingScale,
            float monsterSpacingScale,
            float opposingSpacingScale)
        {
            float radiusSum = first.Radius + second.Radius;
            if (first.Team == second.Team)
            {
                float scale = first.Team == CombatPbdTeam.SummonedUnit
                    ? summonedUnitSpacingScale
                    : monsterSpacingScale;
                return radiusSum * Mathf.Max(0.1f, scale);
            }

            float target = radiusSum * Mathf.Clamp(opposingSpacingScale, 0.1f, 1f);
            float contactLimit = GetContactLimit(first.OpposingContactDistance, second.OpposingContactDistance);
            return contactLimit > 0f ? Mathf.Min(target, contactLimit * 0.95f) : target;
        }

        static float GetContactLimit(float first, float second)
        {
            if (first <= 0f) return second;
            if (second <= 0f) return first;
            return Mathf.Min(first, second);
        }

        static Vector2 GetDeterministicFallbackNormal(int firstIndex, int secondIndex)
        {
            int direction = (firstIndex * 3 + secondIndex) & 3;
            return direction switch
            {
                0 => Vector2.right,
                1 => Vector2.up,
                2 => Vector2.left,
                _ => Vector2.down,
            };
        }
    }
}
