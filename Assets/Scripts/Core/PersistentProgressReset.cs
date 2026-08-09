using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>플레이테스트용 영구 진행 초기화 진입점.</summary>
    public static class PersistentProgressReset
    {
        public const string PendingResetPlayerPrefsKey = "CrossDefense.ProgressReset.Pending.v1";

        static readonly string[] Keys =
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
        };

        public static IReadOnlyList<string> PersistentKeys => Keys;
        public static bool IsPending => PlayerPrefs.GetInt(PendingResetPlayerPrefsKey, 0) == 1;

        public static void RequestOnNextSceneLoad()
        {
            PlayerPrefs.SetInt(PendingResetPlayerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        public static bool ConsumePendingReset()
        {
            if (!IsPending)
                return false;

            ResetNow();
            return true;
        }

        public static int ResetNow()
        {
            int deletedCount = 0;
            foreach (string key in Keys)
            {
                if (!PlayerPrefs.HasKey(key))
                    continue;
                PlayerPrefs.DeleteKey(key);
                deletedCount++;
            }

            PlayerPrefs.DeleteKey(PendingResetPlayerPrefsKey);
            PlayerPrefs.Save();
            return deletedCount;
        }
    }
}
