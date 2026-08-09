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
        SlimeUnlocked = 11,
        SkillUse = 12,
        RelicAcquired = 13,
        RelicEquip = 14,
        RelicSkillUse = 15,
    }

    public enum FeatureTutorialKind
    {
        SlimeUnlocked,
        SkillUse,
        RelicAcquired,
        RelicEquip,
        RelicSkillUse,
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

    [Serializable]
    sealed class FeatureTutorialSaveData
    {
        public int version = FeatureTutorialProgress.CurrentVersion;
        public int completedMask;
    }

    public static class FeatureTutorialProgress
    {
        public const int CurrentVersion = 1;
        public const string DefaultPlayerPrefsKey = "CrossDefense.FeatureTutorials.v1";

        public static bool IsCompleted(FeatureTutorialKind kind) =>
            (LoadMask() & Bit(kind)) != 0;

        public static void Complete(FeatureTutorialKind kind)
        {
            int mask = LoadMask() | Bit(kind);
            PlayerPrefs.SetString(
                DefaultPlayerPrefsKey,
                JsonUtility.ToJson(new FeatureTutorialSaveData { completedMask = mask }));
            PlayerPrefs.Save();
        }

        public static void Reset()
        {
            PlayerPrefs.DeleteKey(DefaultPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        static int LoadMask()
        {
            if (!PlayerPrefs.HasKey(DefaultPlayerPrefsKey))
                return 0;
            try
            {
                FeatureTutorialSaveData data = JsonUtility.FromJson<FeatureTutorialSaveData>(
                    PlayerPrefs.GetString(DefaultPlayerPrefsKey));
                return data != null && data.version == CurrentVersion
                    ? data.completedMask
                    : 0;
            }
            catch
            {
                return 0;
            }
        }

        static int Bit(FeatureTutorialKind kind) => 1 << (int)kind;
    }

    public static class FeatureTutorialViewStates
    {
        public static TutorialViewState Build(FeatureTutorialKind kind, string detail = null) =>
            kind switch
            {
                FeatureTutorialKind.SlimeUnlocked => State(
                    TutorialStep.SlimeUnlocked,
                    "NEW SLIME",
                    "새 슬라임 해금!",
                    $"소환 가능한 슬라임이 늘어났어요.{DetailLine(detail)}\n도감에서 능력과 속성을 확인할 수 있습니다.",
                    "확인",
                    true,
                    false,
                    true,
                    TutorialCardPlacement.Center),
                FeatureTutorialKind.SkillUse => State(
                    TutorialStep.SkillUse,
                    "BATTLE SKILL",
                    "소환사 스킬 사용",
                    "전투 중 오른쪽의 스킬 버튼을 누른 뒤\n필드의 목표 지점을 선택해 보세요.",
                    null,
                    false,
                    true,
                    false,
                    TutorialCardPlacement.Top),
                FeatureTutorialKind.RelicAcquired => State(
                    TutorialStep.RelicAcquired,
                    "NEW RELIC",
                    "첫 신물 획득!",
                    $"{Fallback(detail, "새로운 신물")}을 획득했어요.\n신물은 장착해야 전용 스킬을 사용할 수 있습니다.",
                    "장착하러 가기",
                    true,
                    true,
                    true,
                    TutorialCardPlacement.Center),
                FeatureTutorialKind.RelicEquip => State(
                    TutorialStep.RelicEquip,
                    "RELIC EQUIP",
                    "신물 스킬 장착",
                    "아래 스킬 탭의 신물 목록에서\n방금 얻은 신물의 ‘장착’ 버튼을 눌러 보세요.",
                    null,
                    false,
                    true,
                    false,
                    TutorialCardPlacement.Top),
                FeatureTutorialKind.RelicSkillUse => State(
                    TutorialStep.RelicSkillUse,
                    "RELIC SKILL",
                    "신물 스킬 사용",
                    "장착한 신물에 따라 오른쪽 스킬이 바뀝니다.\n버튼을 누르고 전장에 사용해 보세요.",
                    null,
                    false,
                    true,
                    false,
                    TutorialCardPlacement.Top),
                _ => default,
            };

        static TutorialViewState State(
            TutorialStep step,
            string eyebrow,
            string title,
            string body,
            string nextLabel,
            bool showNext,
            bool showSkip,
            bool blocksInput,
            TutorialCardPlacement placement) =>
            new(true, blocksInput, step, eyebrow, title, body, "TIP", nextLabel,
                showNext, showSkip, placement);

        static string DetailLine(string detail) =>
            string.IsNullOrWhiteSpace(detail) ? string.Empty : $"\n{detail}";

        static string Fallback(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
