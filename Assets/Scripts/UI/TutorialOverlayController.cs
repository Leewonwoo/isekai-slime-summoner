using System;
using CrossDefense.Core;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class TutorialOverlayController : IDisposable
    {
        readonly VisualElement _overlay;
        readonly VisualElement _card;
        readonly Label _eyebrow;
        readonly Label _title;
        readonly Label _body;
        readonly Label _progress;
        readonly Button _nextButton;
        readonly Button _skipButton;
        FirstRunTutorial _tutorial;

        public event Action ContinueRequested;
        public event Action SkipRequested;

        public TutorialOverlayController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("tutorial-overlay");
            _card = root.Q<VisualElement>("tutorial-card");
            _eyebrow = root.Q<Label>("tutorial-eyebrow");
            _title = root.Q<Label>("tutorial-title");
            _body = root.Q<Label>("tutorial-body");
            _progress = root.Q<Label>("tutorial-progress");
            _nextButton = root.Q<Button>("tutorial-next-button");
            _skipButton = root.Q<Button>("tutorial-skip-button");

            if (_nextButton != null)
                _nextButton.clicked += OnContinueClicked;
            if (_skipButton != null)
                _skipButton.clicked += OnSkipClicked;
            Apply(default);
        }

        public void Bind(FirstRunTutorial tutorial)
        {
            Unbind();
            _tutorial = tutorial;
            if (_tutorial == null)
            {
                Apply(default);
                return;
            }

            _tutorial.ViewStateChanged += Apply;
            Apply(_tutorial.CurrentViewState);
        }

        public void Unbind()
        {
            if (_tutorial != null)
                _tutorial.ViewStateChanged -= Apply;
            _tutorial = null;
        }

        public void ShowStandalone(TutorialViewState state) => Apply(state);

        public void HideStandalone() => Apply(default);

        public void Dispose()
        {
            Unbind();
            if (_nextButton != null)
                _nextButton.clicked -= OnContinueClicked;
            if (_skipButton != null)
                _skipButton.clicked -= OnSkipClicked;
        }

        void Apply(TutorialViewState state)
        {
            if (_overlay == null)
                return;

            _overlay.EnableInClassList("tutorial-overlay--hidden", !state.Visible);
            _overlay.EnableInClassList("tutorial-overlay--blocking", state.BlocksInput);
            _overlay.pickingMode = state.BlocksInput ? PickingMode.Position : PickingMode.Ignore;
            if (!state.Visible)
                return;

            _eyebrow.text = state.Eyebrow;
            _title.text = state.Title;
            _body.text = state.Body;
            _progress.text = state.Progress;
            _nextButton.text = state.NextLabel ?? string.Empty;
            _nextButton.EnableInClassList("hidden", !state.ShowNext);
            _skipButton.EnableInClassList("hidden", !state.ShowSkip);
            _card.EnableInClassList(
                "tutorial-card--top",
                state.Placement == TutorialCardPlacement.Top);
            _card.EnableInClassList(
                "tutorial-card--bottom",
                state.Placement == TutorialCardPlacement.Bottom);
        }

        void OnContinueClicked() => ContinueRequested?.Invoke();
        void OnSkipClicked() => SkipRequested?.Invoke();
    }
}
