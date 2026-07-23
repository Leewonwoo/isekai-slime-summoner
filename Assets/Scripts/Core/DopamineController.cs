using System;
using CrossDefense.Data;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    [DisallowMultipleComponent]
    public sealed class DopamineController : MonoBehaviour
    {
        GameManager _gameManager;
        SummonerSkillController _skills;
        DopamineRuntime _runtime;
        System.Random _random;

        public DopamineBalanceData Balance => _runtime?.Balance;
        public DopamineSnapshot Snapshot => _runtime?.Snapshot ?? default;
        public event Action<DopamineSnapshot> StateChanged;

        public void Initialize(
            GameManager gameManager,
            SummonerSkillController skills,
            DopamineBalanceData balance,
            int randomSeed)
        {
            if (_gameManager != null)
                _gameManager.PhaseChanged -= OnPhaseChanged;
            if (_runtime != null)
                _runtime.Changed -= OnRuntimeChanged;

            _gameManager = gameManager;
            _skills = skills;
            _runtime = new DopamineRuntime(balance);
            _random = new System.Random(unchecked(randomSeed ^ 0x4F564552));
            _runtime.Changed += OnRuntimeChanged;
            if (_gameManager != null)
                _gameManager.PhaseChanged += OnPhaseChanged;
            StateChanged?.Invoke(_runtime.Snapshot);
        }

        void Update()
        {
            if (_gameManager == null || _runtime == null ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave)
                return;
            _runtime.Tick(
                Time.deltaTime,
                _gameManager.LivingMonsterCount > 0,
                TryDropMeteor);
        }

        void OnDestroy()
        {
            if (_gameManager != null)
                _gameManager.PhaseChanged -= OnPhaseChanged;
            if (_runtime != null)
                _runtime.Changed -= OnRuntimeChanged;
        }

        public void NotifyMonsterDefeated()
        {
            if (_runtime == null || _gameManager == null ||
                _gameManager.Phase != RunPhase.InWave)
                return;
            _runtime.RegisterDefeat();
            if (!_gameManager.IsGameplayPaused)
                _runtime.Tick(0f, _gameManager.LivingMonsterCount > 0, TryDropMeteor);
        }

        bool TryDropMeteor() =>
            _skills != null &&
            _skills.TryCastOverdriveMeteor(_runtime.Balance, _random);

        void OnPhaseChanged(RunPhase phase)
        {
            if (_runtime == null)
                return;
            if (phase is RunPhase.Victory or RunPhase.Defeat)
                _runtime.ResetRun();
            else if (phase != RunPhase.InWave)
                _runtime.ResetCombo();
        }

        void OnRuntimeChanged(DopamineSnapshot snapshot) => StateChanged?.Invoke(snapshot);
    }
}
