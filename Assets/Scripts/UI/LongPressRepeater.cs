using System;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>짧은 탭은 1회, 0.4초 이상 누르면 0.08초 간격으로 의도를 반복 발행한다.</summary>
    public sealed class LongPressRepeater : IDisposable
    {
        const long InitialDelayMs = 400;
        const long RepeatIntervalMs = 80;

        readonly Button _button;
        readonly Action _action;
        IVisualElementScheduledItem _scheduled;
        bool _pressed;
        bool _repeated;

        public LongPressRepeater(Button button, Action action)
        {
            _button = button;
            _action = action;
            _button.clicked += OnClicked;
            _button.RegisterCallback<PointerDownEvent>(OnPointerDown);
            _button.RegisterCallback<PointerUpEvent>(OnPointerUp);
            _button.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            _button.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        public void Dispose()
        {
            Stop();
            _button.clicked -= OnClicked;
            _button.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            _button.UnregisterCallback<PointerUpEvent>(OnPointerUp);
            _button.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
            _button.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || !_button.enabledSelf) return;
            Stop();
            _pressed = true;
            _repeated = false;
            _scheduled = _button.schedule.Execute(Repeat).StartingIn(InitialDelayMs).Every(RepeatIntervalMs);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (!_pressed || evt.button != 0) return;
            Stop();
        }

        void OnClicked()
        {
            if (_repeated)
            {
                _repeated = false;
                return;
            }
            if (_button.enabledSelf)
                _action?.Invoke();
        }

        void OnPointerCancel(PointerCancelEvent _)
        {
            CancelPress();
        }

        void OnPointerCaptureOut(PointerCaptureOutEvent _) => CancelPress();

        void Repeat()
        {
            if (!_pressed || !_button.enabledSelf)
            {
                Stop();
                return;
            }
            _repeated = true;
            _action?.Invoke();
        }

        void CancelPress()
        {
            Stop();
            _repeated = false;
        }

        void Stop()
        {
            _pressed = false;
            _scheduled?.Pause();
            _scheduled = null;
        }
    }
}
