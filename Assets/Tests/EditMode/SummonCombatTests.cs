using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonCombatTests
    {
        SummonUnitData _data;

        [SetUp]
        public void SetUp()
        {
            _data = SummonUnitData.CreatePrototype(
                "test-slime",
                "테스트 슬라임",
                SummonUnitRarity.Common,
                damage: 10f,
                attacksPerSecond: 1f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void RankGrowth_IncreasesDamageAttackSpeedAndScale()
        {
            Assert.That(_data.DamageAtRank(0), Is.EqualTo(10f).Within(0.001f));
            Assert.That(_data.DamageAtRank(1), Is.EqualTo(18f).Within(0.001f));
            Assert.That(_data.DamageAtRank(2), Is.EqualTo(32.5f).Within(0.001f));
            Assert.That(_data.DamageAtRank(3), Is.EqualTo(55f).Within(0.001f));
            Assert.That(_data.AttacksPerSecondAtRank(3), Is.GreaterThan(_data.AttacksPerSecondAtRank(0)));
            Assert.That(_data.ScaleAtRank(3), Is.GreaterThan(_data.ScaleAtRank(0)));
        }

        [Test]
        public void Promotion_StopsAtRankThree()
        {
            var instance = new SummonUnitInstance(1, _data, 0);

            Assert.That(instance.TryPromote(), Is.True);
            Assert.That(instance.TryPromote(), Is.True);
            Assert.That(instance.TryPromote(), Is.True);
            Assert.That(instance.TryPromote(), Is.False);
            Assert.That(instance.Rank, Is.EqualTo(3));
        }

        [Test]
        public void PrototypeEffects_ClampToSafeCombatValues()
        {
            _data.ConfigurePrototypeEffects(
                null,
                area: -1f,
                slow: 2f,
                slowSeconds: -2f,
                dot: -3f,
                dotSeconds: -4f,
                pierce: 0,
                supportBonus: 2f,
                supportRange: -5f);

            Assert.That(_data.AreaRadius, Is.Zero);
            Assert.That(_data.SlowPercent, Is.EqualTo(0.95f));
            Assert.That(_data.SlowDuration, Is.Zero);
            Assert.That(_data.DamageOverTime, Is.Zero);
            Assert.That(_data.DamageOverTimeDuration, Is.Zero);
            Assert.That(_data.PierceCount, Is.EqualTo(1));
            Assert.That(_data.SupportAttackSpeedBonus, Is.EqualTo(1f));
            Assert.That(_data.SupportRadius, Is.Zero);
        }
    }
}
