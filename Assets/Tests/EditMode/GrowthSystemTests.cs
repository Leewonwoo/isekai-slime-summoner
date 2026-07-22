#if UNITY_EDITOR
using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class GrowthSystemTests
    {
        GrowthBalanceData _balance;
        RunRewardCatalog _runRewards;

        [SetUp]
        public void SetUp()
        {
            _balance = GrowthBalanceData.CreateRuntimeDefault();
            _runRewards = RunRewardCatalog.CreateRuntimeDefault();
        }

        [TearDown]
        public void TearDown()
        {
            if (_balance != null)
                Object.DestroyImmediate(_balance);
            if (_runRewards != null)
                Object.DestroyImmediate(_runRewards);
        }

        [Test]
        public void SummonerProgression_ExperienceAutomaticallyLevelsAndPersistsRemainder()
        {
            string savedJson = null;
            var progression = new SummonerProgression(
                _balance,
                () => string.Empty,
                json => savedJson = json);
            int required = progression.Snapshot.ExperienceToNext;

            progression.AddExperience(required + 5);

            Assert.That(progression.Snapshot.Level, Is.EqualTo(2));
            Assert.That(progression.Snapshot.Experience, Is.EqualTo(5));
            Assert.That(savedJson, Is.Not.Empty);

            var restored = new SummonerProgression(_balance, () => savedJson);
            Assert.That(restored.Snapshot.Level, Is.EqualTo(2));
            Assert.That(restored.Snapshot.Experience, Is.EqualTo(5));
        }

        [Test]
        public void SummonerProgression_InvalidSaveFallsBackToSafeDefaults()
        {
            var progression = new SummonerProgression(
                _balance,
                () => "{\"version\":999,\"level\":80,\"experience\":9999}");

            Assert.That(progression.Snapshot.Level, Is.EqualTo(1));
            Assert.That(progression.Snapshot.Experience, Is.Zero);
        }

        [Test]
        public void SummonerLevel_IncreasesPermanentCombatMultipliers()
        {
            var progression = new SummonerProgression(_balance);
            int required = progression.Snapshot.ExperienceToNext;
            progression.AddExperience(required);

            SummonerProgressionSnapshot snapshot = progression.Snapshot;
            Assert.That(snapshot.DamageMultiplier, Is.GreaterThan(1f));
            Assert.That(snapshot.MaxHpMultiplier, Is.GreaterThan(1f));
            Assert.That(snapshot.JackpotChanceBonus, Is.GreaterThan(0f));
        }

        [Test]
        public void SlimeLevel_UsesIncreasingCostAndSharedDamageSpeedMultipliers()
        {
            int levelOneCost = _balance.SlimeLevelUpCost(1);
            int levelTwoCost = _balance.SlimeLevelUpCost(2);
            var sharedState = new SummonUnitUpgradeState("punch-slime");

            bool applied = sharedState.Apply(
                2,
                _balance.SlimeDamageMultiplier(2),
                _balance.SlimeAttackSpeedMultiplier(2));

            Assert.That(levelTwoCost, Is.GreaterThan(levelOneCost));
            Assert.That(applied, Is.True);
            Assert.That(sharedState.Level, Is.EqualTo(2));
            Assert.That(sharedState.DamageMultiplier, Is.GreaterThan(1f));
            Assert.That(sharedState.AttackSpeedMultiplier, Is.GreaterThan(1f));
        }

        [Test]
        public void RunUpgradeValues_ResetFromLevelZeroBaseline()
        {
            Assert.That(_balance.RunAttackPowerMultiplier(0), Is.EqualTo(1f));
            Assert.That(_balance.RunAttackSpeedMultiplier(0), Is.EqualTo(1f));
            Assert.That(_balance.RunCriticalChance(0), Is.Zero);
            Assert.That(
                _balance.RunUpgradeCost(RunUpgradeType.AttackPower, 1),
                Is.GreaterThan(_balance.RunUpgradeCost(RunUpgradeType.AttackPower, 0)));
        }

        [Test]
        public void PermanentTraits_SummonerLevelCreatesThreeDeterministicChoices()
        {
            int summonerLevel = 3;
            var traits = new PermanentTraitProgression(_balance, () => summonerLevel);

            var first = traits.GetCurrentChoices();
            var second = traits.GetCurrentChoices();

            Assert.That(traits.PendingChoiceCount, Is.EqualTo(1));
            Assert.That(first.Count, Is.EqualTo(3));
            Assert.That(second.Count, Is.EqualTo(3));
            for (int i = 0; i < first.Count; i++)
                Assert.That(second[i].Type, Is.EqualTo(first[i].Type));
            Assert.That(
                first[0].Type,
                Is.EqualTo(PermanentTraitType.SummonerPower)
                    .Or.EqualTo(PermanentTraitType.SummonerHaste));
            Assert.That(
                first[1].Type,
                Is.EqualTo(PermanentTraitType.SlimePower)
                    .Or.EqualTo(PermanentTraitType.SlimeHaste)
                    .Or.EqualTo(PermanentTraitType.SummonCapacity));
            Assert.That(
                first[2].Type,
                Is.EqualTo(PermanentTraitType.CoreVitality)
                    .Or.EqualTo(PermanentTraitType.LuckySummon));
        }

        [Test]
        public void PermanentTraits_ChoicePersistsAndAppliesPermanentMultiplier()
        {
            const int summonerLevel = 3;
            string savedJson = null;
            var traits = new PermanentTraitProgression(
                _balance,
                () => summonerLevel,
                () => string.Empty,
                json => savedJson = json);
            PermanentTraitType selected = traits.GetCurrentChoices()[0].Type;

            Assert.That(traits.TryChoose(selected), Is.True);
            Assert.That(traits.PendingChoiceCount, Is.Zero);
            Assert.That(traits.GetLevel(selected), Is.EqualTo(1));
            Assert.That(savedJson, Is.Not.Empty);

            var restored = new PermanentTraitProgression(
                _balance,
                () => summonerLevel,
                () => savedJson);
            Assert.That(restored.GetLevel(selected), Is.EqualTo(1));
            Assert.That(restored.PendingChoiceCount, Is.Zero);
            PermanentTraitSnapshot snapshot = restored.Snapshot;
            float appliedValue = selected switch
            {
                PermanentTraitType.SummonerPower => snapshot.SummonerDamageMultiplier - 1f,
                PermanentTraitType.SummonerHaste => snapshot.SummonerAttackSpeedMultiplier - 1f,
                PermanentTraitType.CoreVitality => snapshot.CoreMaxHpMultiplier - 1f,
                PermanentTraitType.SlimePower => snapshot.SlimeDamageMultiplier - 1f,
                PermanentTraitType.SlimeHaste => snapshot.SlimeAttackSpeedMultiplier - 1f,
                PermanentTraitType.LuckySummon => snapshot.JackpotChanceBonus,
                _ => 0f,
            };
            Assert.That(appliedValue, Is.GreaterThan(0f));
        }

        [Test]
        public void PermanentTraits_MissedLevelUpsRemainPendingUntilAllAreChosen()
        {
            const int summonerLevel = 7;
            var traits = new PermanentTraitProgression(_balance, () => summonerLevel);

            Assert.That(traits.PendingChoiceCount, Is.EqualTo(3));
            Assert.That(traits.TryChoose(traits.GetCurrentChoices()[0].Type), Is.True);
            Assert.That(traits.PendingChoiceCount, Is.EqualTo(2));
        }

        [Test]
        public void PermanentTraits_LegacyExcessLevelsAreRetainedWithoutCreatingDebt()
        {
            const string legacyJson =
                "{\"version\":1,\"traits\":[{\"type\":0,\"level\":8},{\"type\":3,\"level\":7}]}";
            var traits = new PermanentTraitProgression(
                _balance,
                () => 9,
                () => legacyJson);

            Assert.That(traits.GetLevel(PermanentTraitType.SummonerPower), Is.EqualTo(8));
            Assert.That(traits.GetLevel(PermanentTraitType.SlimePower), Is.EqualTo(7));
            Assert.That(traits.TotalChoiceCount, Is.EqualTo(15));
            Assert.That(traits.CurrentEntitlement, Is.EqualTo(4));
            Assert.That(traits.PendingChoiceCount, Is.Zero);
        }

        [Test]
        public void SummonerSkillLoadout_RejectsLockedSkillAndPersistsUnlockedEquip()
        {
            int level = 7;
            string saved = null;
            var loadout = new SummonerSkillLoadout(
                () => level,
                () => string.Empty,
                json => saved = json);

            Assert.That(loadout.TryEquip(SummonerSkillId.IceWall), Is.False);
            level = 8;
            Assert.That(loadout.TryEquip(SummonerSkillId.IceWall), Is.True);
            Assert.That(loadout.EquippedSkill, Is.EqualTo(SummonerSkillId.IceWall));
            Assert.That(saved, Is.Not.Empty);

            var restored = new SummonerSkillLoadout(() => level, () => saved);
            Assert.That(restored.EquippedSkill, Is.EqualTo(SummonerSkillId.IceWall));
        }

        [Test]
        public void SummonerSkillCatalog_UsesApprovedUnlocksAndCooldowns()
        {
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.Meteor).UnlockLevel, Is.EqualTo(1));
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.Meteor).Cooldown, Is.EqualTo(22f));
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.IceWall).UnlockLevel, Is.EqualTo(8));
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.IceWall).Cooldown, Is.EqualTo(26f));
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.Aegis).UnlockLevel, Is.EqualTo(15));
            Assert.That(SummonerSkillCatalog.Get(SummonerSkillId.Aegis).Cooldown, Is.EqualTo(32f));
        }

        [Test]
        public void RunTraits_AreOfferedOnEveryFifthClearedWave()
        {
            StageTimeline timeline = ScriptableObject.CreateInstance<StageTimeline>();
            try
            {
                Assert.That(timeline.ShouldOfferRunTrait(4), Is.False);
                Assert.That(timeline.ShouldOfferRunTrait(5), Is.True);
                Assert.That(timeline.ShouldOfferRunTrait(10), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(timeline);
            }
        }

        [Test]
        public void RunRewards_FirstChoiceOffersThreeAwakeningsAndAppliesOne()
        {
            var traits = new RunTraitProgression(_runRewards, 2026);

            Assert.That(traits.BeginChoice(5), Is.True);
            Assert.That(traits.IsChoicePending, Is.True);
            var choices = traits.GetCurrentChoices();
            Assert.That(choices.Count, Is.EqualTo(3));
            Assert.That(choices[0].Category, Is.EqualTo(RunRewardCategory.Awakening));

            string selected = choices[0].RewardId;
            Assert.That(traits.TryChoose(selected, out RunRewardDefinition reward), Is.True);
            Assert.That(traits.IsChoicePending, Is.False);
            Assert.That(traits.GetLevel(selected), Is.EqualTo(1));
            Assert.That(reward, Is.Not.Null);
            Assert.That(traits.AttackArchetype, Is.Not.EqualTo(SummonerAttackArchetype.EnergyBolt));
        }

        [Test]
        public void RunRewards_NewRunStartsWithoutPreviousChoices()
        {
            var firstRun = new RunTraitProgression(_runRewards, 2026);
            firstRun.BeginChoice(5);
            Assert.That(
                firstRun.TryChoose(firstRun.GetCurrentChoices()[0].RewardId, out _),
                Is.True);
            Assert.That(firstRun.TotalChoiceCount, Is.EqualTo(1));

            var nextRun = new RunTraitProgression(_runRewards, 2026);
            Assert.That(nextRun.TotalChoiceCount, Is.Zero);
            Assert.That(nextRun.IsChoicePending, Is.False);
            Assert.That(nextRun.AttackArchetype, Is.EqualTo(SummonerAttackArchetype.EnergyBolt));
        }
    }
}
#endif
