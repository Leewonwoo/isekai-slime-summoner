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

        [SetUp]
        public void SetUp()
        {
            _hadValue = PlayerPrefs.HasKey(TutorialProgress.DefaultPlayerPrefsKey);
            _savedValue = _hadValue
                ? PlayerPrefs.GetString(TutorialProgress.DefaultPlayerPrefsKey)
                : null;
            TutorialProgress.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            TutorialProgress.Reset();
            if (_hadValue)
                PlayerPrefs.SetString(TutorialProgress.DefaultPlayerPrefsKey, _savedValue);
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
    }
}
#endif
