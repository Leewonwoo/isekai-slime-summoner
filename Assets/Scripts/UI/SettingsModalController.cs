using System;
using CrossDefense.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class SettingsModalController : IDisposable
    {
        const float DefaultVolume = 0.8f;

        readonly VisualElement _overlay;
        readonly Slider _musicSlider;
        readonly Slider _effectsSlider;
        readonly Label _musicValue;
        readonly Label _effectsValue;
        readonly Button _closeButton;
        readonly Button _resetButton;
        readonly Button _continueButton;
        readonly Button _tutorialButton;

        public bool IsVisible =>
            _overlay != null && !_overlay.ClassListContains("hidden");

        public event Action CloseRequested;
        public event Action<float> EffectsVolumeChanged;
        public event Action TutorialReplayRequested;

        public SettingsModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("settings-overlay");
            _musicSlider = root.Q<Slider>("settings-music-slider");
            _effectsSlider = root.Q<Slider>("settings-effects-slider");
            _musicValue = root.Q<Label>("settings-music-value");
            _effectsValue = root.Q<Label>("settings-effects-value");
            _closeButton = root.Q<Button>("settings-close-button");
            _resetButton = root.Q<Button>("settings-reset-button");
            _continueButton = root.Q<Button>("settings-continue-button");
            _tutorialButton = root.Q<Button>("settings-tutorial-button");

            _musicSlider?.RegisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider?.RegisterValueChangedCallback(OnEffectsVolumeChanged);
            if (_closeButton != null)
                _closeButton.clicked += OnCloseClicked;
            if (_resetButton != null)
                _resetButton.clicked += OnResetClicked;
            if (_continueButton != null)
                _continueButton.clicked += OnCloseClicked;
            if (_tutorialButton != null)
                _tutorialButton.clicked += OnTutorialClicked;

            GameAudioSettings.ApplyStoredSettings();
            SyncControls();
            Hide();
        }

        public void Show()
        {
            SyncControls();
            if (_overlay == null)
                return;
            _overlay.pickingMode = PickingMode.Position;
            _overlay.RemoveFromClassList("hidden");
        }

        public void Hide()
        {
            if (_overlay == null)
                return;
            _overlay.AddToClassList("hidden");
            _overlay.pickingMode = PickingMode.Ignore;
            GameAudioSettings.Save();
        }

        public void Dispose()
        {
            _musicSlider?.UnregisterValueChangedCallback(OnMusicVolumeChanged);
            _effectsSlider?.UnregisterValueChangedCallback(OnEffectsVolumeChanged);
            if (_closeButton != null)
                _closeButton.clicked -= OnCloseClicked;
            if (_resetButton != null)
                _resetButton.clicked -= OnResetClicked;
            if (_continueButton != null)
                _continueButton.clicked -= OnCloseClicked;
            if (_tutorialButton != null)
                _tutorialButton.clicked -= OnTutorialClicked;
        }

        void SyncControls()
        {
            SetSliderWithoutNotify(_musicSlider, GameAudioSettings.MusicVolume);
            SetSliderWithoutNotify(_effectsSlider, GameAudioSettings.EffectsVolume);
            UpdateValueLabel(_musicValue, GameAudioSettings.MusicVolume);
            UpdateValueLabel(_effectsValue, GameAudioSettings.EffectsVolume);
        }

        void OnMusicVolumeChanged(ChangeEvent<float> evt)
        {
            float normalized = Mathf.Clamp01(evt.newValue / 100f);
            GameAudioSettings.SetMusicVolume(normalized);
            UpdateValueLabel(_musicValue, normalized);
        }

        void OnEffectsVolumeChanged(ChangeEvent<float> evt)
        {
            float normalized = Mathf.Clamp01(evt.newValue / 100f);
            GameAudioSettings.SetEffectsVolume(normalized);
            UpdateValueLabel(_effectsValue, normalized);
            EffectsVolumeChanged?.Invoke(normalized);
        }

        void OnResetClicked()
        {
            GameAudioSettings.SetMusicVolume(DefaultVolume);
            GameAudioSettings.SetEffectsVolume(DefaultVolume);
            SyncControls();
            EffectsVolumeChanged?.Invoke(DefaultVolume);
        }

        void OnCloseClicked() => CloseRequested?.Invoke();
        void OnTutorialClicked() => TutorialReplayRequested?.Invoke();

        static void SetSliderWithoutNotify(Slider slider, float normalized)
        {
            slider?.SetValueWithoutNotify(Mathf.Clamp01(normalized) * 100f);
        }

        static void UpdateValueLabel(Label label, float normalized)
        {
            if (label != null)
                label.text = $"{Mathf.RoundToInt(Mathf.Clamp01(normalized) * 100f)}%";
        }
    }
}
