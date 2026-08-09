#if UNITY_EDITOR
using System.Collections.Generic;
using CrossDefense.Core;
using NUnit.Framework;
using UnityEngine;

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
    }
}
#endif
