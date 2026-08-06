using System;
using System.Collections.Generic;
using CrossDefense.Data;
using CrossDefense.Units;

namespace CrossDefense.Core
{
    public enum TutorialCardPlacement
    {
        Top,
        Center,
        Bottom,
    }

    public readonly struct TutorialViewState
    {
        public bool Visible { get; }
        public bool BlocksInput { get; }
        public TutorialStep Step { get; }
        public string Eyebrow { get; }
        public string Title { get; }
        public string Body { get; }
        public string Progress { get; }
        public string NextLabel { get; }
        public bool ShowNext { get; }
        public bool ShowSkip { get; }
        public TutorialCardPlacement Placement { get; }

        public TutorialViewState(
            bool visible,
            bool blocksInput,
            TutorialStep step,
            string eyebrow,
            string title,
            string body,
            string progress,
            string nextLabel,
            bool showNext,
            bool showSkip,
            TutorialCardPlacement placement)
        {
            Visible = visible;
            BlocksInput = blocksInput;
            Step = step;
            Eyebrow = eyebrow;
            Title = title;
            Body = body;
            Progress = progress;
            NextLabel = nextLabel;
            ShowNext = showNext;
            ShowSkip = showSkip;
            Placement = placement;
        }
    }

    public sealed class FirstRunTutorial : IDisposable
    {
        const int ActionStepCount = 10;

        readonly GameManager _gameManager;
        readonly SummonManager _summonManager;
        readonly SummonedUnitManager _unitManager;
        TutorialStep _step;
        SummonUnitData _mergeUnit;
        int _mergeRank;
        bool _active;
        bool _firstKillObserved;
        bool _startedWithOwnedUnit;

        public bool IsActive => _active;
        public TutorialStep Step => _step;
        public TutorialViewState CurrentViewState { get; private set; }

        public event Action<TutorialViewState> ViewStateChanged;

        public FirstRunTutorial(GameManager gameManager)
        {
            _gameManager = gameManager;
            _summonManager = gameManager?.SummonManager;
            _unitManager = gameManager?.SummonedUnitManager;
            if (_gameManager == null || _summonManager == null || _unitManager == null)
                return;

            bool replay = TutorialProgress.ConsumeReplayRequest();
            TutorialProgressSnapshot progress = TutorialProgress.Load();
            if (progress.Completed && !replay)
                return;

            _active = true;
            _step = replay ? TutorialStep.Intro : SanitizeResumeStep(progress.Step);
            _startedWithOwnedUnit = RestoreMergeReference();
            PrepareResumeStep();
            Subscribe();
            _gameManager.SetTutorialWaveHold(true);
            PrepareGuaranteedFirstSummon();
            EnterStep(_step, false);
            TutorialAnalytics.Track("tutorial_started", _step, replay ? "replay" : "first_run");
        }

        public void Dispose()
        {
            Unsubscribe();
            ReleaseTutorialLocks();
            _active = false;
        }

        public void Continue()
        {
            if (!_active)
                return;

            switch (_step)
            {
                case TutorialStep.Intro:
                    EnterStep(_startedWithOwnedUnit
                        ? TutorialStep.Reposition
                        : TutorialStep.Summon);
                    SetTutorialPause(false);
                    break;
                case TutorialStep.StartWave:
                    EnterStep(TutorialStep.ObserveReward);
                    _gameManager.SetTutorialWaveHold(false);
                    SetTutorialPause(false);
                    break;
                case TutorialStep.MergeResult:
                    EnterStep(TutorialStep.Elements);
                    break;
                case TutorialStep.Elements:
                    EnterStep(TutorialStep.IndependentWave);
                    _gameManager.SetTutorialWaveHold(false);
                    SetTutorialPause(false);
                    TryCompleteIndependentStep();
                    break;
                case TutorialStep.Completed:
                    HideCompletedCard();
                    break;
            }
        }

        public void Skip()
        {
            if (!_active)
                return;

            TutorialAnalytics.Track("tutorial_skipped", _step);
            TutorialProgress.Skip();
            _summonManager.ClearForcedNextUnit();
            _unitManager.SetTutorialMergeHint(null, 0, false);
            ReleaseTutorialLocks();
            _active = false;
            Publish(HiddenState());
        }

        void Subscribe()
        {
            _summonManager.UnitAdded += OnUnitAdded;
            _unitManager.FieldDragEnded += OnFieldDragEnded;
            _unitManager.UnitMerged += OnUnitMerged;
            _gameManager.MonsterResolved += OnMonsterResolved;
            _gameManager.WaveChanged += OnWaveChanged;
            _gameManager.PhaseChanged += OnPhaseChanged;
        }

        void Unsubscribe()
        {
            if (_summonManager != null)
                _summonManager.UnitAdded -= OnUnitAdded;
            if (_unitManager != null)
            {
                _unitManager.FieldDragEnded -= OnFieldDragEnded;
                _unitManager.UnitMerged -= OnUnitMerged;
            }
            if (_gameManager != null)
            {
                _gameManager.MonsterResolved -= OnMonsterResolved;
                _gameManager.WaveChanged -= OnWaveChanged;
                _gameManager.PhaseChanged -= OnPhaseChanged;
            }
        }

        void OnUnitAdded(SummonUnitInstance instance)
        {
            if (!_active || instance?.Unit == null)
                return;

            if (_mergeUnit == null)
            {
                _mergeUnit = instance.Unit;
                _mergeRank = instance.Rank;
                _summonManager.TryForceNextUnit(_mergeUnit, _mergeRank);
                if ((int)_step <= (int)TutorialStep.Summon)
                    EnterStep(TutorialStep.Reposition);
                return;
            }

            if (instance.Unit.UnitId != _mergeUnit.UnitId || instance.Rank != _mergeRank)
                return;

            if (_step is TutorialStep.SecondSummon or TutorialStep.Merge)
            {
                EnterStep(TutorialStep.Merge);
                SetTutorialPause(false);
                _unitManager.SetTutorialMergeHint(_mergeUnit.UnitId, _mergeRank, true);
            }
        }

        void OnFieldDragEnded(
            SummonedUnitController unit,
            bool merged,
            bool validPlacement)
        {
            if (!_active || _step != TutorialStep.Reposition || merged || !validPlacement)
                return;

            EnterStep(TutorialStep.StartWave);
            SetTutorialPause(true);
        }

        void OnUnitMerged(SummonedUnitController result, int previousRank, int resultRank)
        {
            if (!_active)
                return;

            TutorialAnalytics.Track(
                "tutorial_merge_completed",
                _step,
                $"unit={result?.Data?.UnitId},rank={previousRank}->{resultRank}");
            if (_step != TutorialStep.Merge)
                return;

            _unitManager.SetTutorialMergeHint(_mergeUnit?.UnitId, _mergeRank, false);
            EnterStep(TutorialStep.MergeResult);
            SetTutorialPause(true);
        }

        void OnMonsterResolved(MonsterController _)
        {
            if (!_active || _firstKillObserved)
                return;

            _firstKillObserved = true;
            if (_step != TutorialStep.ObserveReward)
                return;

            if (CountMatchingFieldUnits() >= SummonRank.MergeMaterialCount)
            {
                EnterStep(TutorialStep.Merge);
                _unitManager.SetTutorialMergeHint(_mergeUnit.UnitId, _mergeRank, true);
                return;
            }

            _summonManager.TryForceNextUnit(_mergeUnit, _mergeRank);
            EnterStep(TutorialStep.SecondSummon);
            SetTutorialPause(true);
        }

        void OnWaveChanged(int current, int _)
        {
            if (!_active || _step != TutorialStep.IndependentWave || current < 2)
                return;
            CompleteTutorial();
        }

        void OnPhaseChanged(RunPhase phase)
        {
            if (!_active || _step != TutorialStep.IndependentWave)
                return;
            if (phase == RunPhase.Victory)
                CompleteTutorial();
        }

        void TryCompleteIndependentStep()
        {
            if (_gameManager.CurrentWave >= 2 || _gameManager.Phase == RunPhase.Victory)
                CompleteTutorial();
        }

        void CompleteTutorial()
        {
            if (!_active || _step == TutorialStep.Completed)
                return;

            TutorialProgress.Complete();
            TutorialAnalytics.Track("tutorial_completed", TutorialStep.Completed);
            EnterStep(TutorialStep.Completed, false);
            SetTutorialPause(true);
        }

        void HideCompletedCard()
        {
            ReleaseTutorialLocks();
            _active = false;
            Publish(HiddenState());
        }

        void EnterStep(TutorialStep next, bool save = true)
        {
            _step = next;
            if (save)
                TutorialProgress.SaveStep(next);
            TutorialAnalytics.Track("tutorial_step_started", next);
            Publish(BuildViewState(next));
            if (next is TutorialStep.Intro or TutorialStep.MergeResult or
                TutorialStep.Elements or TutorialStep.Completed)
                SetTutorialPause(true);
        }

        void PrepareGuaranteedFirstSummon()
        {
            if (_mergeUnit != null || (int)_step > (int)TutorialStep.Summon)
                return;

            IReadOnlyList<SummonUnitData> pool = _summonManager.Pool;
            for (int i = 0; i < pool.Count; i++)
            {
                if (_summonManager.TryForceNextUnit(pool[i], 0))
                    return;
            }
        }

        bool RestoreMergeReference()
        {
            IReadOnlyList<SummonedUnitController> fieldUnits = _unitManager.Units;
            for (int i = 0; i < fieldUnits.Count; i++)
            {
                SummonedUnitController candidate = fieldUnits[i];
                if (!IsMergeEligible(candidate))
                    continue;
                for (int j = i + 1; j < fieldUnits.Count; j++)
                {
                    SummonedUnitController other = fieldUnits[j];
                    if (!IsMatching(candidate, other))
                        continue;
                    SetMergeReference(candidate.Data, candidate.Instance.Rank);
                    return true;
                }
            }

            for (int i = 0; i < fieldUnits.Count; i++)
            {
                SummonedUnitController candidate = fieldUnits[i];
                if (!IsMergeEligible(candidate))
                    continue;
                SetMergeReference(candidate.Data, candidate.Instance.Rank);
                return true;
            }

            IReadOnlyList<SummonUnitInstance> bench = _summonManager.Bench;
            for (int i = 0; i < bench.Count; i++)
            {
                SummonUnitInstance candidate = bench[i];
                if (candidate?.Unit == null ||
                    candidate.Rank >= SummonRank.MaxInternalRank)
                    continue;
                SetMergeReference(candidate.Unit, candidate.Rank);
                return true;
            }

            return false;
        }

        void PrepareResumeStep()
        {
            if ((int)_step > (int)TutorialStep.Summon && _mergeUnit == null)
            {
                _step = TutorialStep.Summon;
                return;
            }

            if (_step is not (TutorialStep.SecondSummon or TutorialStep.Merge))
                return;

            if (CountMatchingFieldUnits() >= SummonRank.MergeMaterialCount)
            {
                _step = TutorialStep.Merge;
                _unitManager.SetTutorialMergeHint(_mergeUnit.UnitId, _mergeRank, true);
                return;
            }

            _step = TutorialStep.SecondSummon;
            _summonManager.TryForceNextUnit(_mergeUnit, _mergeRank);
        }

        int CountMatchingFieldUnits()
        {
            if (_mergeUnit == null)
                return 0;

            int count = 0;
            IReadOnlyList<SummonedUnitController> units = _unitManager.Units;
            for (int i = 0; i < units.Count; i++)
            {
                SummonedUnitController unit = units[i];
                if (unit?.Data?.UnitId == _mergeUnit.UnitId &&
                    unit.Instance?.Rank == _mergeRank)
                    count++;
            }
            return count;
        }

        void SetMergeReference(SummonUnitData unit, int rank)
        {
            _mergeUnit = unit;
            _mergeRank = rank;
        }

        static bool IsMergeEligible(SummonedUnitController unit) =>
            unit?.Data != null && unit.Instance != null &&
            unit.Instance.Rank < SummonRank.MaxInternalRank;

        static bool IsMatching(
            SummonedUnitController first,
            SummonedUnitController second) =>
            IsMergeEligible(first) && IsMergeEligible(second) &&
            first.Data.UnitId == second.Data.UnitId &&
            first.Instance.Rank == second.Instance.Rank;

        TutorialStep SanitizeResumeStep(TutorialStep saved)
        {
            if ((int)saved < (int)TutorialStep.Intro ||
                (int)saved >= (int)TutorialStep.Completed)
                return TutorialStep.Intro;
            if (saved > TutorialStep.Summon && CountOwnedUnits() == 0)
                return TutorialStep.Summon;
            return saved;
        }

        int CountOwnedUnits()
        {
            int count = _summonManager.Bench.Count;
            IReadOnlyList<SummonedUnitController> units = _unitManager.Units;
            return count + (units?.Count ?? 0);
        }

        void SetTutorialPause(bool paused) =>
            _gameManager.SetGameplayPause(GameplayPauseReason.Tutorial, paused);

        void ReleaseTutorialLocks()
        {
            if (_gameManager == null)
                return;
            _gameManager.SetTutorialWaveHold(false);
            _gameManager.SetGameplayPause(GameplayPauseReason.Tutorial, false);
        }

        void Publish(TutorialViewState state)
        {
            CurrentViewState = state;
            ViewStateChanged?.Invoke(state);
        }

        static TutorialViewState BuildViewState(TutorialStep step)
        {
            string progress = step == TutorialStep.Completed
                ? "완료"
                : $"{Math.Min((int)step + 1, ActionStepCount)}/{ActionStepCount}";
            return step switch
            {
                TutorialStep.Intro => State(step, "FIRST RUN", "소환사를 지켜라",
                    "고블린이 중앙의 소환사를 노리고 있어요.\n슬라임을 소환해 함께 막아내세요.",
                    progress, "시작하기", true, true, true, TutorialCardPlacement.Center),
                TutorialStep.Summon => State(step, "STEP 1", "첫 슬라임 소환",
                    "용병 계약서 1장을 사용해요.\n오른쪽 아래의 ‘소환 ×1’을 눌러 보세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.Reposition => State(step, "STEP 2", "슬라임 재배치",
                    "필드의 슬라임을 길게 끌어\n원하는 빈자리로 옮겨 보세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.StartWave => State(step, "STEP 3", "전투 준비 완료",
                    "슬라임은 가까운 적을 스스로 찾아 싸워요.\n준비가 끝났다면 첫 웨이브를 시작하세요.",
                    progress, "웨이브 시작", true, true, true, TutorialCardPlacement.Center),
                TutorialStep.ObserveReward => State(step, "STEP 4", "전투와 보상",
                    "고블린 처치는 골드와 경험치를 줘요.\n첫 고블린을 처치해 보세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.SecondSummon => State(step, "STEP 5", "머지 재료 확보",
                    "같은 슬라임이 다음 결과로 준비됐어요.\n‘소환 ×1’을 눌러 재료를 받으세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.Merge => State(step, "STEP 6", "같은 슬라임 2머지",
                    "같은 종류·같은 성급 슬라임을 겹치면 승급해요.\n빛나는 슬라임 하나를 다른 하나 위로 끌어 놓으세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.MergeResult => State(step, "STEP 7", "성급 상승",
                    "★1 두 개가 더 강한 ★2 하나가 되었어요.\n머지는 보유 공간도 한 칸 비워 줍니다.",
                    progress, "다음", true, true, true, TutorialCardPlacement.Center),
                TutorialStep.Elements => State(step, "STEP 8", "속성과 역할",
                    "화염·빙결·자연은 서로 상성이 있어요.\n슬라임의 공격 방식과 적 속성을 함께 확인하세요.",
                    progress, "직접 해보기", true, true, true, TutorialCardPlacement.Center),
                TutorialStep.IndependentWave => State(step, "STEP 9", "이제 당신의 선택",
                    "소환·재배치·머지로 다음 웨이브를 준비하세요.\n필요할 때는 소환 탭의 ‘합성’ 표시를 확인하세요.",
                    progress, null, false, true, false, TutorialCardPlacement.Top),
                TutorialStep.Completed => State(step, "TUTORIAL CLEAR", "튜토리얼 완료",
                    "기본 전투 준비를 모두 익혔어요.\n이제 원하는 슬라임 조합을 만들어 보세요!",
                    progress, "전투 계속", true, false, true, TutorialCardPlacement.Center),
                _ => HiddenState(),
            };
        }

        static TutorialViewState State(
            TutorialStep step,
            string eyebrow,
            string title,
            string body,
            string progress,
            string nextLabel,
            bool showNext,
            bool showSkip,
            bool blocksInput,
            TutorialCardPlacement placement) =>
            new(true, blocksInput, step, eyebrow, title, body, progress, nextLabel,
                showNext, showSkip, placement);

        static TutorialViewState HiddenState() =>
            new(false, false, TutorialStep.Completed, string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, false, false,
                TutorialCardPlacement.Top);
    }
}
