using System;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>필드 오버레이 — 필드 상단 웨이브 상태와 스킬 플로팅 버튼.</summary>
    public class FieldOverlayController
    {
        readonly Label _waveLabel;
        readonly Label _remainingMonstersLabel;
        readonly Label _comboLabel;
        readonly VisualElement _overdriveGauge;
        readonly VisualElement _overdriveGaugeFill;
        readonly Label _overdriveGaugeLabel;
        readonly Button _skillButton;
        readonly Button _codexButton;
        readonly Label _unlockToast;
        readonly Label _skillName;
        readonly Label _skillCooldown;
        readonly VisualElement _skillIcon;
        readonly Action _skillClickHandler;
        string _skillIconClass;
        StageWaveKind _waveKind;
        int _lastCombo;
        IVisualElementScheduledItem _comboMilestoneReset;
        IVisualElementScheduledItem _unlockToastReset;

        public event Action SkillRequested;
        public event Action CodexRequested;

        public FieldOverlayController(VisualElement root)
        {
            _waveLabel = root.Q<Label>("wave-label");
            _remainingMonstersLabel = root.Q<Label>("remaining-monsters-label");
            _comboLabel = root.Q<Label>("combo-label");
            _overdriveGauge = root.Q<VisualElement>("overdrive-gauge");
            _overdriveGaugeFill = root.Q<VisualElement>("overdrive-gauge-fill");
            _overdriveGaugeLabel = root.Q<Label>("overdrive-gauge-label");
            _skillButton = root.Q<Button>("skill-button");
            _codexButton = root.Q<Button>("codex-button");
            _unlockToast = root.Q<Label>("unlock-toast");
            _skillName = root.Q<Label>("skill-button-name");
            _skillCooldown = root.Q<Label>("skill-button-cooldown");
            _skillIcon = root.Q<VisualElement>("skill-button-icon");
            _skillClickHandler = () => SkillRequested?.Invoke();
            _skillButton.clicked += _skillClickHandler;
            _codexButton.clicked += OnCodexClicked;
        }

        public void SetWave(int current, int remainingMonsters)
        {
            _waveLabel.text = _waveKind == StageWaveKind.Rush
                ? $"RUSH WAVE {current}"
                : UIFormat.Wave(current);
            _remainingMonstersLabel.text = UIFormat.RemainingMonsters(remainingMonsters);
        }

        public void SetWaveKind(StageWaveKind kind)
        {
            _waveKind = kind;
            bool rush = kind == StageWaveKind.Rush;
            _waveLabel.EnableInClassList("field-overlay__wave--rush", rush);
        }

        public void ShowUnlockToast(string message)
        {
            _unlockToastReset?.Pause();
            _unlockToast.text = message;
            _unlockToast.RemoveFromClassList("hidden");
            _unlockToastReset = _unlockToast.schedule.Execute(
                () => _unlockToast.AddToClassList("hidden")).StartingIn(2600);
        }

        public void SetSkillButtonVisible(bool visible) => _skillButton.EnableInClassList("hidden", !visible);

        public void Dispose()
        {
            _comboMilestoneReset?.Pause();
            _unlockToastReset?.Pause();
            _skillButton.clicked -= _skillClickHandler;
            _codexButton.clicked -= OnCodexClicked;
        }

        public void SetDopamineState(
            DopamineSnapshot snapshot,
            DopamineBalanceData balance)
        {
            int combo = snapshot.Combo;
            _comboLabel.text = combo >= 2 ? $"x{combo:N0} COMBO" : string.Empty;
            _comboLabel.EnableInClassList("hidden", combo < 2);
            if (balance != null && combo > _lastCombo && balance.IsComboMilestone(combo))
            {
                _comboMilestoneReset?.Pause();
                _comboLabel.AddToClassList("combo--milestone");
                _comboMilestoneReset = _comboLabel.schedule.Execute(
                    () => _comboLabel.RemoveFromClassList("combo--milestone"))
                    .StartingIn(220);
            }
            if (combo == 0)
                _comboLabel.RemoveFromClassList("combo--milestone");
            _lastCombo = combo;

            float fill = snapshot.IsActive && balance != null
                ? snapshot.ActiveTimeRemaining / balance.OverdriveDuration
                : snapshot.GaugeProgress;
            _overdriveGaugeFill.style.width = Length.Percent(Mathf.Clamp01(fill) * 100f);
            _overdriveGauge.EnableInClassList("overdrive--charging", !snapshot.IsReady && !snapshot.IsActive);
            _overdriveGauge.EnableInClassList("overdrive--ready", snapshot.IsReady);
            _overdriveGauge.EnableInClassList("overdrive--active", snapshot.IsActive);
            _overdriveGaugeLabel.text = snapshot.IsActive
                ? $"OVERDRIVE {snapshot.ActiveTimeRemaining:0.0}s"
                : snapshot.IsReady ? "READY" : string.Empty;
        }

        public void SetSkillState(
            SummonerSkillDefinition definition,
            float remainingCooldown,
            bool targeting)
        {
            _skillName.text = targeting ? "지점 선택" : definition.DisplayName;
            string nextIconClass = SkillIconClass(definition.Id);
            if (!string.IsNullOrEmpty(_skillIconClass))
                _skillIcon.RemoveFromClassList(_skillIconClass);
            _skillIconClass = nextIconClass;
            _skillIcon.AddToClassList(_skillIconClass);
            _skillCooldown.text = remainingCooldown > 0f
                ? Mathf.CeilToInt(remainingCooldown).ToString()
                : string.Empty;
            _skillButton.EnableInClassList("skill-button--cooldown", remainingCooldown > 0f);
            _skillButton.EnableInClassList("skill-button--targeting", targeting);
            _skillButton.SetEnabled(targeting || remainingCooldown <= 0f);
        }

        static string SkillIconClass(SummonerSkillId id) => id switch
        {
            SummonerSkillId.IceWall => "skill-icon--ice-wall",
            SummonerSkillId.Aegis => "skill-icon--aegis",
            _ => "skill-icon--meteor",
        };

        void OnCodexClicked() => CodexRequested?.Invoke();
    }
}
