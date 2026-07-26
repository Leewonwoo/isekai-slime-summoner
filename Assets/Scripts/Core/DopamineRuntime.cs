using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct DopamineSnapshot
    {
        public DopamineSnapshot(
            int combo,
            float comboTimeRemaining,
            int gauge,
            int maxGauge,
            bool isActive,
            float activeTimeRemaining)
        {
            Combo = combo;
            ComboTimeRemaining = Mathf.Max(0f, comboTimeRemaining);
            Gauge = Mathf.Clamp(gauge, 0, Mathf.Max(1, maxGauge));
            MaxGauge = Mathf.Max(1, maxGauge);
            IsActive = isActive;
            ActiveTimeRemaining = Mathf.Max(0f, activeTimeRemaining);
        }

        public int Combo { get; }
        public float ComboTimeRemaining { get; }
        public int Gauge { get; }
        public int MaxGauge { get; }
        public float GaugeProgress => MaxGauge <= 0 ? 0f : (float)Gauge / MaxGauge;
        public bool IsReady => !IsActive && Gauge >= MaxGauge;
        public bool IsActive { get; }
        public float ActiveTimeRemaining { get; }
    }

    /// <summary>Unity 생명주기와 분리해 검증 가능한 콤보·오버드라이브 상태 머신.</summary>
    public sealed class DopamineRuntime
    {
        readonly DopamineBalanceData _balance;
        int _combo;
        float _comboTimeRemaining;
        int _gauge;
        bool _isActive;
        float _activeTimeRemaining;

        public DopamineRuntime(DopamineBalanceData balance, int startingGauge = 0)
        {
            _balance = balance != null ? balance : DopamineBalanceData.CreateRuntimeDefault();
            _gauge = Mathf.Clamp(startingGauge, 0, _balance.MaxGauge);
        }

        public DopamineBalanceData Balance => _balance;
        public DopamineSnapshot Snapshot => new(
            _combo,
            _comboTimeRemaining,
            _gauge,
            _balance.MaxGauge,
            _isActive,
            _activeTimeRemaining);

        public event Action<DopamineSnapshot> Changed;

        public void RegisterDefeat()
        {
            _combo++;
            _comboTimeRemaining = _balance.ComboGraceSeconds;
            if (!_isActive)
                _gauge = Mathf.Min(_balance.MaxGauge, _gauge + _balance.GaugePerKill(_combo));
            NotifyChanged();
        }

        public void Tick(float deltaTime, bool hasLivingEnemy, Func<bool> tryActivateOverdrive)
        {
            float safeDelta = Mathf.Max(0f, deltaTime);
            bool changed = TickCombo(safeDelta);

            if (!_isActive && _gauge >= _balance.MaxGauge && hasLivingEnemy)
            {
                BeginOverdrive();
                tryActivateOverdrive?.Invoke();
                changed = true;
            }

            if (_isActive && hasLivingEnemy)
                changed |= TickOverdrive(safeDelta);

            if (changed)
                NotifyChanged();
        }

        public void ResetCombo()
        {
            if (_combo == 0 && _comboTimeRemaining <= 0f)
                return;
            _combo = 0;
            _comboTimeRemaining = 0f;
            NotifyChanged();
        }

        public void ResetRun()
        {
            _combo = 0;
            _comboTimeRemaining = 0f;
            _gauge = 0;
            _isActive = false;
            _activeTimeRemaining = 0f;
            NotifyChanged();
        }

        bool TickCombo(float deltaTime)
        {
            if (_combo <= 0 || deltaTime <= 0f)
                return false;
            float previous = _comboTimeRemaining;
            _comboTimeRemaining = Mathf.Max(0f, _comboTimeRemaining - deltaTime);
            if (_comboTimeRemaining > 0f)
                return !Mathf.Approximately(previous, _comboTimeRemaining);
            _combo = 0;
            return true;
        }

        void BeginOverdrive()
        {
            _gauge = 0;
            _isActive = true;
            _activeTimeRemaining = _balance.OverdriveDuration;
        }

        bool TickOverdrive(float deltaTime)
        {
            bool changed = false;
            float previousTime = _activeTimeRemaining;
            _activeTimeRemaining = Mathf.Max(0f, _activeTimeRemaining - deltaTime);
            changed |= !Mathf.Approximately(previousTime, _activeTimeRemaining);

            if (_activeTimeRemaining > 0f)
                return changed;

            _isActive = false;
            _activeTimeRemaining = 0f;
            return true;
        }

        void NotifyChanged() => Changed?.Invoke(Snapshot);
    }
}
