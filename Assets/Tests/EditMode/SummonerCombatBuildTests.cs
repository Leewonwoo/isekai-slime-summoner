using System.Linq;
using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class SummonerCombatBuildTests
    {
        RunRewardCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = RunRewardCatalog.CreateRuntimeDefault();
        }

        [TearDown]
        public void TearDown()
        {
            if (_catalog != null)
                Object.DestroyImmediate(_catalog);
        }

        [Test]
        public void DefaultCatalog_ContainsTenSummonerLevelBuilds()
        {
            var rewards = _catalog.GetRewards(RunRewardTrigger.SummonerLevel);

            Assert.That(rewards.Count, Is.EqualTo(10));
            Assert.That(rewards.Select(reward => reward.Effect), Is.EquivalentTo(new[]
            {
                RunRewardEffect.Multicast,
                RunRewardEffect.RapidCast,
                RunRewardEffect.ManaSpread,
                RunRewardEffect.PierceEngraving,
                RunRewardEffect.ManaSplit,
                RunRewardEffect.Ricochet,
                RunRewardEffect.Afterimage,
                RunRewardEffect.CriticalBurst,
                RunRewardEffect.SlimeResonance,
                RunRewardEffect.ManaOverdrive,
            }));
        }

        [Test]
        public void SummonerLevelUpCreatesThreeChoicesAndGuaranteesStarter()
        {
            var progression = new SummonerCombatBuildProgression(_catalog, 2026);

            progression.NotifySummonerLevelChanged(1, 2);

            Assert.That(progression.SummonerLevel, Is.EqualTo(2));
            Assert.That(progression.PendingChoiceCount, Is.EqualTo(1));
            Assert.That(progression.IsChoicePending, Is.True);
            Assert.That(progression.GetCurrentChoices(), Has.Count.EqualTo(3));
            Assert.That(progression.GetCurrentChoices().Any(choice =>
                choice.Reward.Effect is RunRewardEffect.Multicast or
                    RunRewardEffect.RapidCast or
                    RunRewardEffect.ManaSpread), Is.True);
        }

        [Test]
        public void ChoosingCombatBuildImmediatelyChangesAttackProfile()
        {
            var progression = new SummonerCombatBuildProgression(_catalog, 2026);
            progression.NotifySummonerLevelChanged(1, 2);
            RunTraitChoice selected = progression.GetCurrentChoices()[0];

            Assert.That(progression.TryChoose(selected.RewardId), Is.True);

            Assert.That(progression.GetLevel(selected.RewardId), Is.EqualTo(1));
            Assert.That(progression.PendingChoiceCount, Is.Zero);
            Assert.That(progression.IsChoicePending, Is.False);
            SummonerCombatBuildProfile profile = progression.BuildProfile();
            AssertProfileChanged(selected.Reward.Effect, profile);
        }

        [Test]
        public void MultipleSummonerLevelUpsQueueOneChoicePerLevel()
        {
            var progression = new SummonerCombatBuildProgression(_catalog, 77);

            progression.NotifySummonerLevelChanged(1, 4);

            Assert.That(progression.SummonerLevel, Is.EqualTo(4));
            Assert.That(progression.PendingChoiceCount, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                RunTraitChoice choice = progression.GetCurrentChoices()[0];
                Assert.That(progression.TryChoose(choice.RewardId), Is.True);
            }
            Assert.That(progression.PendingChoiceCount, Is.Zero);
            Assert.That(progression.IsChoicePending, Is.False);
        }

        [Test]
        public void CriticalDamageFlagSurvivesScaledChildPacket()
        {
            var packet = new DamagePacket(
                null,
                100f,
                MonsterAttribute.Fire,
                isCritical: true);

            DamagePacket child = packet.Scaled(0.4f);

            Assert.That(child.BaseDamage, Is.EqualTo(40f));
            Assert.That(child.IsCritical, Is.True);
        }

        static void AssertProfileChanged(
            RunRewardEffect effect,
            SummonerCombatBuildProfile profile)
        {
            switch (effect)
            {
                case RunRewardEffect.Multicast:
                    Assert.That(profile.AdditionalProjectileCount, Is.GreaterThan(0));
                    break;
                case RunRewardEffect.RapidCast:
                    Assert.That(profile.AttackSpeedMultiplier, Is.GreaterThan(1f));
                    break;
                case RunRewardEffect.ManaSpread:
                    Assert.That(profile.SpreadProjectileCount, Is.GreaterThan(0));
                    break;
                case RunRewardEffect.PierceEngraving:
                    Assert.That(profile.AdditionalPierceCount, Is.GreaterThan(0));
                    break;
                case RunRewardEffect.ManaSplit:
                    Assert.That(profile.SplitProjectileCount, Is.GreaterThan(0));
                    break;
                case RunRewardEffect.Ricochet:
                    Assert.That(profile.RicochetCount, Is.GreaterThan(0));
                    break;
                case RunRewardEffect.Afterimage:
                    Assert.That(profile.AfterimageChance, Is.GreaterThan(0f));
                    break;
                case RunRewardEffect.CriticalBurst:
                    Assert.That(profile.CriticalBurstRadius, Is.GreaterThan(0f));
                    break;
                case RunRewardEffect.SlimeResonance:
                    Assert.That(profile.SlimeResonanceChance, Is.GreaterThan(0f));
                    break;
                case RunRewardEffect.ManaOverdrive:
                    Assert.That(profile.OverdriveAttackInterval, Is.GreaterThan(0));
                    break;
                default:
                    Assert.Fail($"Unexpected combat build effect: {effect}");
                    break;
            }
        }
    }
}
