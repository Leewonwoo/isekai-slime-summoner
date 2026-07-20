#if UNITY_EDITOR
using CrossDefense.Core;
using CrossDefense.Data;
using CrossDefense.Units;
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
            Assert.That(_data.DamageAtRank(3), Is.EqualTo(_data.DamageAtRank(2)).Within(0.001f));
            Assert.That(_data.AttacksPerSecondAtRank(2), Is.GreaterThan(_data.AttacksPerSecondAtRank(0)));
            Assert.That(_data.ScaleAtRank(2), Is.GreaterThan(_data.ScaleAtRank(0)));
            Assert.That(_data.MaxHpAtRank(0), Is.EqualTo(100f).Within(0.001f));
            Assert.That(_data.MaxHpAtRank(2), Is.EqualTo(225f).Within(0.001f));
            Assert.That(SummonRank.FormatStars(0), Is.EqualTo("★1"));
            Assert.That(SummonRank.FormatStars(2), Is.EqualTo("★3"));
        }

        [TestCase(50f, 100f, 0.5f)]
        [TestCase(-10f, 100f, 0f)]
        [TestCase(150f, 100f, 1f)]
        [TestCase(10f, 0f, 0f)]
        public void WorldHealthBar_NormalizesHealthSafely(float current, float maximum, float expected)
        {
            Assert.That(WorldHealthBar.GetNormalizedHealth(current, maximum), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void Promotion_StopsAtVisibleStarThree()
        {
            var instance = new SummonUnitInstance(1, _data, 0);

            Assert.That(instance.TryPromote(), Is.True);
            Assert.That(instance.TryPromote(), Is.True);
            Assert.That(instance.TryPromote(), Is.False);
            Assert.That(instance.Rank, Is.EqualTo(2));
            Assert.That(SummonRank.FormatStars(instance.Rank), Is.EqualTo("★3"));
        }

        [Test]
        public void SameUnitInstances_ShareUpgradeState()
        {
            var shared = new SummonUnitUpgradeState(_data.UnitId);
            var first = new SummonUnitInstance(1, _data, 0, shared);
            var second = new SummonUnitInstance(2, _data, 1, shared);

            Assert.That(shared.Apply(4, 1.6f, 1.2f), Is.True);
            Assert.That(first.Level, Is.EqualTo(4));
            Assert.That(second.Level, Is.EqualTo(4));
            Assert.That(first.DamageMultiplier, Is.EqualTo(1.6f));
            Assert.That(second.AttackSpeedMultiplier, Is.EqualTo(1.2f));
        }

        [Test]
        public void BenchCapacity_CountsSameUnitAndRankAsOneStack()
        {
            var manager = new SummonManager(null, new[] { _data }, 0f, 0f, 0, 1, 123);

            Assert.That(manager.ReturnToBench(new SummonUnitInstance(1, _data, 0)), Is.True);
            Assert.That(manager.ReturnToBench(new SummonUnitInstance(2, _data, 0)), Is.True);
            Assert.That(manager.ReturnToBench(new SummonUnitInstance(3, _data, 1)), Is.False);
            Assert.That(manager.Bench.Count, Is.EqualTo(2));
            Assert.That(manager.BenchStackCount, Is.EqualTo(1));
            Assert.That(manager.IsBenchFull, Is.True);
            Assert.That(manager.Bench[0].UpgradeState, Is.SameAs(manager.Bench[1].UpgradeState));
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

        [Test]
        public void PrototypeAnimation_StoresFramesAndClampsFps()
        {
            var frames = new Sprite[9];

            _data.ConfigurePrototypeAnimation(frames, 0f);

            Assert.That(_data.MoveFrames, Is.SameAs(frames));
            Assert.That(_data.MoveFrames.Length, Is.EqualTo(9));
            Assert.That(_data.MoveAnimationFps, Is.EqualTo(1f));
        }

        [Test]
        public void PrototypeStar3Skill_StoresModeAndClampsUnsafeValues()
        {
            _data.ConfigurePrototypeStar3Skill(
                "테스트 스킬",
                Star3SkillMode.TargetArea,
                cooldown: -2f,
                damageMultiplier: -1f,
                radius: -3f,
                duration: -4f,
                strength: -5f,
                pierce: 0,
                effectSprite: null,
                visualScale: 0f,
                skillSlowPercent: 2f,
                skillSlowDuration: -1f,
                skillDotMultiplier: -2f,
                skillDotDuration: -3f);

            Assert.That(_data.HasStar3Skill, Is.True);
            Assert.That(_data.Star3SkillName, Is.EqualTo("테스트 스킬"));
            Assert.That(_data.Star3SkillModeValue, Is.EqualTo(Star3SkillMode.TargetArea));
            Assert.That(_data.Star3SkillCooldown, Is.EqualTo(0.1f));
            Assert.That(_data.Star3SkillDamageMultiplier, Is.Zero);
            Assert.That(_data.Star3SkillRadius, Is.Zero);
            Assert.That(_data.Star3SkillDuration, Is.Zero);
            Assert.That(_data.Star3SkillStrength, Is.Zero);
            Assert.That(_data.Star3SkillPierceCount, Is.EqualTo(1));
            Assert.That(_data.Star3SkillVisualScale, Is.EqualTo(0.1f));
            Assert.That(_data.Star3SkillSlowPercent, Is.EqualTo(0.95f));
            Assert.That(_data.Star3SkillSlowDuration, Is.Zero);
            Assert.That(_data.Star3SkillDotMultiplier, Is.Zero);
            Assert.That(_data.Star3SkillDotDuration, Is.Zero);
        }
    }
}
#endif
