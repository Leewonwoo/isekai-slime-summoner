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
        readonly Button _slimeCodexButton;
        readonly Button _monsterCodexButton;
        readonly Label _unlockToast;
        readonly Label _skillName;
        readonly Label _skillCooldown;
        readonly VisualElement _skillIcon;
        readonly Action _skillClickHandler;
        readonly VisualElement _buffSkillCluster;
        readonly Button[] _buffSkillButtons = new Button[SummonerBuffCatalog.MaxEquipped];
        readonly VisualElement[] _buffSkillIcons = new VisualElement[SummonerBuffCatalog.MaxEquipped];
        readonly Label[] _buffSkillNames = new Label[SummonerBuffCatalog.MaxEquipped];
        readonly Label[] _buffSkillCooldowns = new Label[SummonerBuffCatalog.MaxEquipped];
        readonly Action[] _buffSkillClickHandlers = new Action[SummonerBuffCatalog.MaxEquipped];
        readonly string[] _buffSkillIconClasses = new string[SummonerBuffCatalog.MaxEquipped];
        string _skillIconClass;
        StageWaveKind _waveKind;
        int _lastCombo;
        IVisualElementScheduledItem _comboMilestoneReset;
        IVisualElementScheduledItem _unlockToastReset;

        public event Action SkillRequested;
        public event Action<int> BuffSkillRequested;
        public event Action SlimeCodexRequested;
        public event Action MonsterCodexRequested;

        public FieldOverlayController(VisualElement root)
        {
            _waveLabel = root.Q<Label>("wave-label");
            _remainingMonstersLabel = root.Q<Label>("remaining-monsters-label");
            _comboLabel = root.Q<Label>("combo-label");
            _overdriveGauge = root.Q<VisualElement>("overdrive-cluster");
            _overdriveGaugeFill = root.Q<VisualElement>("overdrive-gauge-fill");
            _overdriveGaugeLabel = root.Q<Label>("overdrive-gauge-label");
            _skillButton = root.Q<Button>("skill-button");
            _slimeCodexButton = root.Q<Button>("slime-codex-button");
            _monsterCodexButton = root.Q<Button>("monster-codex-button");
            _unlockToast = root.Q<Label>("unlock-toast");
            _skillName = root.Q<Label>("skill-button-name");
            _skillCooldown = root.Q<Label>("skill-button-cooldown");
            _skillIcon = root.Q<VisualElement>("skill-button-icon");
            _buffSkillCluster = root.Q<VisualElement>("buff-skill-cluster");
            _skillClickHandler = () => SkillRequested?.Invoke();
            _skillButton.clicked += _skillClickHandler;
            for (int i = 0; i < SummonerBuffCatalog.MaxEquipped; i++)
            {
                int captured = i;
                _buffSkillButtons[i] = root.Q<Button>($"buff-skill-button-{i}");
                _buffSkillIcons[i] = root.Q<VisualElement>($"buff-skill-icon-{i}");
                _buffSkillNames[i] = root.Q<Label>($"buff-skill-name-{i}");
                _buffSkillCooldowns[i] = root.Q<Label>($"buff-skill-cooldown-{i}");
                _buffSkillClickHandlers[i] = () => BuffSkillRequested?.Invoke(captured);
                _buffSkillButtons[i].clicked += _buffSkillClickHandlers[i];
            }
            _slimeCodexButton.clicked += OnSlimeCodexClicked;
            _monsterCodexButton.clicked += OnMonsterCodexClicked;
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

        public void SetSkillButtonVisible(bool visible)
        {
            _skillButton.EnableInClassList("hidden", !visible);
            _buffSkillCluster.EnableInClassList("hidden", !visible);
        }

        public void Dispose()
        {
            _comboMilestoneReset?.Pause();
            _unlockToastReset?.Pause();
            _skillButton.clicked -= _skillClickHandler;
            for (int i = 0; i < _buffSkillButtons.Length; i++)
                _buffSkillButtons[i].clicked -= _buffSkillClickHandlers[i];
            _slimeCodexButton.clicked -= OnSlimeCodexClicked;
            _monsterCodexButton.clicked -= OnMonsterCodexClicked;
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
            _overdriveGaugeFill.style.height = Length.Percent(Mathf.Clamp01(fill) * 100f);
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

        public void SetBuffSkillState(
            int slotIndex,
            SummonerBuffDefinition? definition,
            float remainingCooldown,
            bool active)
        {
            if (slotIndex < 0 || slotIndex >= _buffSkillButtons.Length)
                return;

            Button button = _buffSkillButtons[slotIndex];
            VisualElement icon = _buffSkillIcons[slotIndex];
            if (!string.IsNullOrEmpty(_buffSkillIconClasses[slotIndex]))
                icon.RemoveFromClassList(_buffSkillIconClasses[slotIndex]);

            bool assigned = definition.HasValue;
            if (assigned)
            {
                SummonerBuffDefinition value = definition.Value;
                string iconClass = BuffIconClass(value.Id);
                _buffSkillIconClasses[slotIndex] = iconClass;
                icon.AddToClassList(iconClass);
                _buffSkillNames[slotIndex].text = BuffShortName(value.Id);
                _buffSkillCooldowns[slotIndex].text = remainingCooldown > 0f
                    ? Mathf.CeilToInt(remainingCooldown).ToString()
                    : string.Empty;
            }
            else
            {
                _buffSkillIconClasses[slotIndex] = string.Empty;
                _buffSkillNames[slotIndex].text = "비어 있음";
                _buffSkillCooldowns[slotIndex].text = string.Empty;
            }

            button.EnableInClassList("buff-skill-button--empty", !assigned);
            button.EnableInClassList(
                "buff-skill-button--cooldown",
                assigned && remainingCooldown > 0f);
            button.EnableInClassList("buff-skill-button--active", assigned && active);
            button.SetEnabled(assigned && remainingCooldown <= 0f);
        }

        static string SkillIconClass(SummonerSkillId id) => id switch
        {
            SummonerSkillId.IceWall => "skill-icon--ice-wall",
            SummonerSkillId.Aegis => "skill-icon--aegis",
            _ => "skill-icon--meteor",
        };

        static string BuffIconClass(SummonerBuffId id) => id switch
        {
            SummonerBuffId.Aegis => "buff-icon--aegis",
            SummonerBuffId.LegionCommand => "buff-icon--command",
            SummonerBuffId.LifeBlessing => "buff-icon--life",
            SummonerBuffId.ElementalResonance => "buff-icon--resonance",
            SummonerBuffId.TimeAcceleration => "buff-icon--time",
            _ => "buff-icon--aegis",
        };

        static string BuffShortName(SummonerBuffId id) => id switch
        {
            SummonerBuffId.Aegis => "보호막",
            SummonerBuffId.LegionCommand => "지휘",
            SummonerBuffId.LifeBlessing => "치유",
            SummonerBuffId.ElementalResonance => "공명",
            SummonerBuffId.TimeAcceleration => "가속",
            _ => string.Empty,
        };

        void OnSlimeCodexClicked() => SlimeCodexRequested?.Invoke();
        void OnMonsterCodexClicked() => MonsterCodexRequested?.Invoke();
    }
}
