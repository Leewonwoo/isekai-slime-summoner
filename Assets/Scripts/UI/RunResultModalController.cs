using System;
using CrossDefense.Core;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class RunResultModalController : IDisposable
    {
        readonly VisualElement _overlay;
        readonly VisualElement _panel;
        readonly Label _eyebrow;
        readonly Label _title;
        readonly Label _summary;
        readonly Label _gold;
        readonly Button _restartButton;
        readonly Button _continueButton;

        public bool IsVisible =>
            _overlay != null && !_overlay.ClassListContains("hidden");

        public event Action RestartRequested;
        public event Action ContinueRequested;

        public RunResultModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("run-result-overlay");
            _panel = root.Q<VisualElement>("run-result-panel");
            _eyebrow = root.Q<Label>("run-result-eyebrow");
            _title = root.Q<Label>("run-result-title");
            _summary = root.Q<Label>("run-result-summary");
            _gold = root.Q<Label>("run-result-gold");
            _restartButton = root.Q<Button>("run-result-restart-button");
            _continueButton = root.Q<Button>("run-result-continue-button");

            if (_restartButton != null)
                _restartButton.clicked += OnRestartClicked;
            if (_continueButton != null)
                _continueButton.clicked += OnContinueClicked;
            Hide();
        }

        public void Show(
            RunPhase phase,
            string stageName,
            int currentDay,
            int totalDays,
            int gold,
            bool canContinue)
        {
            bool victory = phase == RunPhase.Victory;
            if (!victory && phase != RunPhase.Defeat)
            {
                Hide();
                return;
            }

            if (_overlay != null)
            {
                _overlay.pickingMode = PickingMode.Position;
                _overlay.RemoveFromClassList("hidden");
            }
            _panel?.EnableInClassList("run-result__panel--victory", victory);
            _panel?.EnableInClassList("run-result__panel--defeat", !victory);
            if (_eyebrow != null)
                _eyebrow.text = stageName ?? string.Empty;
            if (_title != null)
                _title.text = victory ? "스테이지 클리어" : "소환사 패배";
            if (_summary != null)
                _summary.text = victory
                    ? $"DAY {Math.Max(1, totalDays):N0}까지 방어했습니다"
                    : $"DAY {Math.Max(1, currentDay):N0}에서 쓰러졌습니다\nDAY 1부터 다시 시작합니다";
            if (_gold != null)
                _gold.text = $"보유 금화  {Math.Max(0, gold):N0} G";
            if (_restartButton != null)
                _restartButton.text = victory ? "처음부터 다시" : "다시 시작";
            if (_continueButton != null)
                _continueButton.style.display =
                    victory && canContinue ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Hide()
        {
            if (_overlay == null)
                return;
            _overlay.AddToClassList("hidden");
            _overlay.pickingMode = PickingMode.Ignore;
        }

        public void Dispose()
        {
            if (_restartButton != null)
                _restartButton.clicked -= OnRestartClicked;
            if (_continueButton != null)
                _continueButton.clicked -= OnContinueClicked;
        }

        void OnRestartClicked() => RestartRequested?.Invoke();
        void OnContinueClicked() => ContinueRequested?.Invoke();
    }
}
