#if UNITY_EDITOR
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.Tests.EditMode
{
    public sealed class PersistentProgressResetTests
    {
        readonly Dictionary<string, string> _savedValues = new();
        bool _pendingExisted;
        int _pendingValue;

        [SetUp]
        public void SetUp()
        {
            _savedValues.Clear();
            foreach (string key in PersistentProgressReset.PersistentKeys)
            {
                if (PlayerPrefs.HasKey(key))
                    _savedValues[key] = PlayerPrefs.GetString(key);
            }
            _pendingExisted = PlayerPrefs.HasKey(PersistentProgressReset.PendingResetPlayerPrefsKey);
            _pendingValue = PlayerPrefs.GetInt(PersistentProgressReset.PendingResetPlayerPrefsKey, 0);
        }

        [TearDown]
        public void TearDown()
        {
            PersistentProgressReset.ResetNow();
            foreach (KeyValuePair<string, string> entry in _savedValues)
                PlayerPrefs.SetString(entry.Key, entry.Value);
            if (_pendingExisted)
                PlayerPrefs.SetInt(PersistentProgressReset.PendingResetPlayerPrefsKey, _pendingValue);
            PlayerPrefs.Save();
        }

        [Test]
        public void ResetNow_DeletesEveryRegisteredPersistentSave()
        {
            foreach (string key in PersistentProgressReset.PersistentKeys)
                PlayerPrefs.SetString(key, "{\"test\":true}");
            PersistentProgressReset.RequestOnNextSceneLoad();

            Assert.That(PersistentProgressReset.ConsumePendingReset(), Is.True);
            Assert.That(PersistentProgressReset.IsPending, Is.False);
            foreach (string key in PersistentProgressReset.PersistentKeys)
                Assert.That(PlayerPrefs.HasKey(key), Is.False, key);
        }

        [Test]
        public void PersistentKeys_AreUniqueAndCoverAllProgressionStores()
        {
            CollectionAssert.AllItemsAreUnique(PersistentProgressReset.PersistentKeys);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    SummonerProgression.DefaultPlayerPrefsKey,
                    PermanentTraitProgression.DefaultPlayerPrefsKey,
                    SummonerSkillLoadout.DefaultPlayerPrefsKey,
                    SummonerBuffLoadout.DefaultPlayerPrefsKey,
                    GrowthManager.DefaultPlayerPrefsKey,
                    MonsterCodexProgression.DefaultPlayerPrefsKey,
                    EquipmentProgression.DefaultPlayerPrefsKey,
                    RelicProgression.DefaultPlayerPrefsKey,
                    RunSessionProgression.DefaultPlayerPrefsKey,
                    WalletProgression.DefaultPlayerPrefsKey,
                    TutorialProgress.DefaultPlayerPrefsKey,
                    FeatureTutorialProgress.DefaultPlayerPrefsKey,
                },
                PersistentProgressReset.PersistentKeys);
        }

        [Test]
        public void SettingsDataReset_RequiresTwoRequestsAndResetsConfirmationOnClose()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/UI/UXML/SettingsModal.uxml");
            Assert.That(tree, Is.Not.Null);
            TemplateContainer root = tree.CloneTree();
            var controller = new SettingsModalController(root);
            int requestCount = 0;
            controller.DataResetRequested += () => requestCount++;

            controller.Show();
            controller.RequestDataReset();

            Button button = root.Q<Button>("settings-data-reset-button");
            Label warning = root.Q<Label>("settings-data-reset-warning");
            Assert.That(requestCount, Is.Zero);
            Assert.That(button.text, Is.EqualTo("정말 초기화"));
            Assert.That(warning.ClassListContains("hidden"), Is.False);

            controller.RequestDataReset();
            Assert.That(requestCount, Is.EqualTo(1));

            controller.Hide();
            controller.Show();
            Assert.That(button.text, Is.EqualTo("데이터 초기화"));
            Assert.That(warning.ClassListContains("hidden"), Is.True);
            controller.Dispose();
        }
    }
}
#endif
