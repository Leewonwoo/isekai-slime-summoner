using System;
using UnityEngine;

namespace CrossDefense.Core
{
    public enum TutorialStep
    {
        Intro = 0,
        Summon = 1,
        Reposition = 2,
        StartWave = 3,
        ObserveReward = 4,
        SecondSummon = 5,
        Merge = 6,
        MergeResult = 7,
        Elements = 8,
        IndependentWave = 9,
        Completed = 10,
    }

    [Serializable]
    sealed class TutorialSaveData
    {
        public int version = TutorialProgress.CurrentVersion;
        public int step;
        public bool completed;
        public bool skipped;
        public bool replayRequested;
    }

    public readonly struct TutorialProgressSnapshot
    {
        public int Version { get; }
        public TutorialStep Step { get; }
        public bool Completed { get; }
        public bool Skipped { get; }
        public bool ReplayRequested { get; }

        public TutorialProgressSnapshot(
            int version,
            TutorialStep step,
            bool completed,
            bool skipped,
            bool replayRequested)
        {
            Version = version;
            Step = step;
            Completed = completed;
            Skipped = skipped;
            ReplayRequested = replayRequested;
        }
    }

    public static class TutorialProgress
    {
        public const int CurrentVersion = 1;
        public const string DefaultPlayerPrefsKey = "CrossDefense.Tutorial.v1";

        public static TutorialProgressSnapshot Load()
        {
            if (!PlayerPrefs.HasKey(DefaultPlayerPrefsKey))
                return DefaultSnapshot();

            try
            {
                TutorialSaveData data =
                    JsonUtility.FromJson<TutorialSaveData>(PlayerPrefs.GetString(DefaultPlayerPrefsKey));
                if (data == null || data.version != CurrentVersion)
                    return DefaultSnapshot();

                TutorialStep step = Enum.IsDefined(typeof(TutorialStep), data.step)
                    ? (TutorialStep)data.step
                    : TutorialStep.Intro;
                return new TutorialProgressSnapshot(
                    data.version,
                    step,
                    data.completed,
                    data.skipped,
                    data.replayRequested);
            }
            catch
            {
                return DefaultSnapshot();
            }
        }

        public static void SaveStep(TutorialStep step) =>
            Save(new TutorialSaveData
            {
                step = (int)step,
                completed = step == TutorialStep.Completed,
            });

        public static void Complete() =>
            Save(new TutorialSaveData
            {
                step = (int)TutorialStep.Completed,
                completed = true,
            });

        public static void Skip() =>
            Save(new TutorialSaveData
            {
                step = (int)TutorialStep.Completed,
                completed = true,
                skipped = true,
            });

        public static void RequestReplay()
        {
            TutorialProgressSnapshot current = Load();
            Save(new TutorialSaveData
            {
                step = (int)current.Step,
                completed = current.Completed,
                skipped = current.Skipped,
                replayRequested = true,
            });
        }

        public static bool ConsumeReplayRequest()
        {
            TutorialProgressSnapshot current = Load();
            if (!current.ReplayRequested)
                return false;

            Save(new TutorialSaveData
            {
                step = (int)TutorialStep.Intro,
                completed = false,
            });
            return true;
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(DefaultPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        static TutorialProgressSnapshot DefaultSnapshot() =>
            new(CurrentVersion, TutorialStep.Intro, false, false, false);

        static void Save(TutorialSaveData data)
        {
            PlayerPrefs.SetString(DefaultPlayerPrefsKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }

    public static class TutorialAnalytics
    {
        public static void Track(string eventName, TutorialStep step, string detail = null)
        {
            string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $", detail={detail}";
            Debug.Log($"[TutorialAnalytics] event={eventName}, version={TutorialProgress.CurrentVersion}, step={step}{suffix}");
        }
    }
}
