#if UNITY_EDITOR
using System.Reflection;
using CrossDefense.Core;
using NUnit.Framework;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class TutorialProgressTests
    {
        bool _hadValue;
        string _savedValue;
        bool _hadFeatureValue;
        string _savedFeatureValue;

        [SetUp]
        public void SetUp()
        {
            _hadValue = PlayerPrefs.HasKey(TutorialProgress.DefaultPlayerPrefsKey);
            _savedValue = _hadValue
                ? PlayerPrefs.GetString(TutorialProgress.DefaultPlayerPrefsKey)
                : null;
            _hadFeatureValue = PlayerPrefs.HasKey(FeatureTutorialProgress.DefaultPlayerPrefsKey);
            _savedFeatureValue = _hadFeatureValue
                ? PlayerPrefs.GetString(FeatureTutorialProgress.DefaultPlayerPrefsKey)
                : null;
            TutorialProgress.Reset();
            FeatureTutorialProgress.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            TutorialProgress.Reset();
            FeatureTutorialProgress.Reset();
            if (_hadValue)
                PlayerPrefs.SetString(TutorialProgress.DefaultPlayerPrefsKey, _savedValue);
            if (_hadFeatureValue)
                PlayerPrefs.SetString(
                    FeatureTutorialProgress.DefaultPlayerPrefsKey,
                    _savedFeatureValue);
            PlayerPrefs.Save();
        }

        [Test]
        public void SaveStep_RestoresLastIncompleteStep()
        {
            TutorialProgress.SaveStep(TutorialStep.Merge);

            TutorialProgressSnapshot snapshot = TutorialProgress.Load();

            Assert.That(snapshot.Version, Is.EqualTo(TutorialProgress.CurrentVersion));
            Assert.That(snapshot.Step, Is.EqualTo(TutorialStep.Merge));
            Assert.That(snapshot.Completed, Is.False);
            Assert.That(snapshot.Skipped, Is.False);
        }

        [Test]
        public void Skip_MarksTutorialCompletedAndSkipped()
        {
            TutorialProgress.Skip();

            TutorialProgressSnapshot snapshot = TutorialProgress.Load();

            Assert.That(snapshot.Step, Is.EqualTo(TutorialStep.Completed));
            Assert.That(snapshot.Completed, Is.True);
            Assert.That(snapshot.Skipped, Is.True);
        }

        [Test]
        public void ReplayRequest_IsConsumedAndRestartsFromIntro()
        {
            TutorialProgress.Complete();
            TutorialProgress.RequestReplay();

            Assert.That(TutorialProgress.ConsumeReplayRequest(), Is.True);
            TutorialProgressSnapshot snapshot = TutorialProgress.Load();
            Assert.That(snapshot.Step, Is.EqualTo(TutorialStep.Intro));
            Assert.That(snapshot.Completed, Is.False);
            Assert.That(snapshot.ReplayRequested, Is.False);
            Assert.That(TutorialProgress.ConsumeReplayRequest(), Is.False);
        }

        [TestCase(TutorialStep.Summon)]
        [TestCase(TutorialStep.Reposition)]
        [TestCase(TutorialStep.ObserveReward)]
        [TestCase(TutorialStep.SecondSummon)]
        [TestCase(TutorialStep.Merge)]
        [TestCase(TutorialStep.IndependentWave)]
        public void ActionStep_DoesNotBlockRequiredGameplayInput(TutorialStep step)
        {
            MethodInfo buildViewState = typeof(FirstRunTutorial).GetMethod(
                "BuildViewState",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(buildViewState, Is.Not.Null);
            var state = (TutorialViewState)buildViewState.Invoke(null, new object[] { step });

            Assert.That(state.Visible, Is.True);
            Assert.That(state.BlocksInput, Is.False);
        }

        [Test]
        public void MergeStep_ExplainsTwoMatchingSlimes()
        {
            MethodInfo buildViewState = typeof(FirstRunTutorial).GetMethod(
                "BuildViewState",
                BindingFlags.Static | BindingFlags.NonPublic);

            var state = (TutorialViewState)buildViewState.Invoke(
                null,
                new object[] { TutorialStep.Merge });

            StringAssert.Contains("같은 종류·같은 성급", state.Body);
            StringAssert.Contains("겹치면", state.Body);
        }

        [Test]
        public void FeatureTutorialProgress_CompletesKindsIndependently()
        {
            FeatureTutorialProgress.Complete(FeatureTutorialKind.SkillUse);

            Assert.That(
                FeatureTutorialProgress.IsCompleted(FeatureTutorialKind.SkillUse),
                Is.True);
            Assert.That(
                FeatureTutorialProgress.IsCompleted(FeatureTutorialKind.RelicEquip),
                Is.False);
        }

        [Test]
        public void RelicTutorial_ConnectsAcquireEquipAndUseInstructions()
        {
            TutorialViewState acquired = FeatureTutorialViewStates.Build(
                FeatureTutorialKind.RelicAcquired,
                "붉은 지팡이");
            TutorialViewState equip = FeatureTutorialViewStates.Build(
                FeatureTutorialKind.RelicEquip);
            TutorialViewState use = FeatureTutorialViewStates.Build(
                FeatureTutorialKind.RelicSkillUse);

            StringAssert.Contains("붉은 지팡이", acquired.Body);
            StringAssert.Contains("장착", acquired.Body);
            StringAssert.Contains("스킬 탭", equip.Body);
            StringAssert.Contains("사용", use.Title);
            Assert.That(acquired.BlocksInput, Is.True);
            Assert.That(equip.BlocksInput, Is.False);
            Assert.That(use.BlocksInput, Is.False);
        }

        [Test]
        public void FieldMenuTutorial_ExplainsIconsThenWaitsForSpeedToggle()
        {
            TutorialViewState menu = FeatureTutorialViewStates.Build(
                FeatureTutorialKind.FieldMenuIcons);
            TutorialViewState speed = FeatureTutorialViewStates.Build(
                FeatureTutorialKind.GameplaySpeed);

            StringAssert.Contains("×1.0", menu.Body);
            StringAssert.Contains("설정", menu.Body);
            StringAssert.Contains("도감", menu.Body);
            Assert.That(menu.ShowNext, Is.True);
            Assert.That(menu.BlocksInput, Is.True);
            StringAssert.Contains("×1.5", speed.Body);
            Assert.That(speed.ShowNext, Is.False);
            Assert.That(speed.BlocksInput, Is.False);
        }
    }
}
#endif
