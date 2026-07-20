#if UNITY_EDITOR
using CrossDefense.Data;
using CrossDefense.Units;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonFormationPlannerTests
    {
        [Test]
        public void FirstRing_UsesEvenlySpacedSlotsAtInnerRadius()
        {
            const float radius = 1.15f;
            Vector2 center = new(2f, -1f);
            Vector2 first = SummonFormationPlanner.GetSlot(center, 0, 6, radius, 0.7f, 6, 90f);
            Vector2 second = SummonFormationPlanner.GetSlot(center, 1, 6, radius, 0.7f, 6, 90f);

            Assert.That(Vector2.Distance(center, first), Is.EqualTo(radius).Within(0.001f));
            Assert.That(Vector2.Distance(center, second), Is.EqualTo(radius).Within(0.001f));
            Assert.That(Vector2.Distance(first, second), Is.GreaterThan(1f));
        }

        [Test]
        public void SeventhUnit_StartsStaggeredOuterRing()
        {
            Vector2 center = Vector2.zero;
            Vector2 seventh = SummonFormationPlanner.GetSlot(center, 6, 7, 1.15f, 0.7f, 6, 90f);

            Assert.That(seventh.magnitude, Is.EqualTo(1.85f).Within(0.001f));
        }

        [Test]
        public void RolePriority_PutsSupportAndRangedInsideMelee()
        {
            Assert.That(SummonFormationPlanner.GetRolePriority(SummonAttackStyle.Support), Is.LessThan(
                SummonFormationPlanner.GetRolePriority(SummonAttackStyle.Projectile)));
            Assert.That(SummonFormationPlanner.GetRolePriority(SummonAttackStyle.Projectile), Is.LessThan(
                SummonFormationPlanner.GetRolePriority(SummonAttackStyle.Melee)));
        }
    }
}
#endif
