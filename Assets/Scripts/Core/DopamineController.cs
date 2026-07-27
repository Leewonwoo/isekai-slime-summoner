using System;
using System.Collections.Generic;
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
        SummonerAttackController _summonerAttack;

        public DopamineBalanceData Balance => _runtime?.Balance;
        public DopamineSnapshot Snapshot => _runtime?.Snapshot ?? default;
        public float SummonerDamageMultiplier =>
            Snapshot.IsActive && Balance != null
                ? Balance.OverdriveDamageMultiplier
                : 1f;
        public float SummonerAttackSpeedMultiplier =>
            Snapshot.IsActive && Balance != null
                ? Balance.OverdriveAttackSpeedMultiplier
                : 1f;
        public event Action<DopamineSnapshot> StateChanged;
        public event Action<int, float, int> ComboCashedOut;

        public void Initialize(
            GameManager gameManager,
            SummonerSkillController skills,
            DopamineBalanceData balance,
            int randomSeed,
            int startingGauge = 0)
        {
            if (_gameManager != null)
                _gameManager.PhaseChanged -= OnPhaseChanged;
            if (_runtime != null)
            {
                _runtime.Changed -= OnRuntimeChanged;
                _runtime.ComboExpired -= OnComboExpired;
            }

            _gameManager = gameManager;
            _skills = skills;
            _summonerAttack = gameManager?.Summoner != null
                ? gameManager.Summoner.GetComponent<SummonerAttackController>()
                : null;
            _runtime = new DopamineRuntime(balance, startingGauge);
            _random = new System.Random(unchecked(randomSeed ^ 0x4F564552));
            _runtime.Changed += OnRuntimeChanged;
            _runtime.ComboExpired += OnComboExpired;
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
                ActivateOverdrive);
        }

        void OnDestroy()
        {
            if (_gameManager != null)
                _gameManager.PhaseChanged -= OnPhaseChanged;
            if (_runtime != null)
            {
                _runtime.Changed -= OnRuntimeChanged;
                _runtime.ComboExpired -= OnComboExpired;
            }
        }

        public void NotifyMonsterDefeated()
        {
            if (_runtime == null || _gameManager == null ||
                _gameManager.Phase != RunPhase.InWave)
                return;
            _runtime.RegisterDefeat();
            if (!_gameManager.IsGameplayPaused)
                _runtime.Tick(0f, _gameManager.LivingMonsterCount > 0, ActivateOverdrive);
        }

        bool ActivateOverdrive() =>
            _skills != null &&
            _skills.ResetCooldownAndAutoCastRelic(_random);

        void OnComboExpired(int combo)
        {
            if (_gameManager == null || _runtime?.Balance == null ||
                combo < _runtime.Balance.ComboCashoutMinimum)
                return;

            float baseDamage = _summonerAttack != null
                ? _summonerAttack.AttackDamage
                : 12f;
            float damage = _gameManager.ModifySummonerDamage(
                baseDamage * combo * _runtime.Balance.ComboDamagePerCount);
            var packet = new DamagePacket(
                this,
                damage,
                MonsterAttribute.None);
            IReadOnlyCollection<MonsterController> monsters =
                _gameManager.SummonedUnitManager?.Monsters;
            if (monsters != null)
            {
                var snapshot = new List<MonsterController>(monsters);
                for (int i = 0; i < snapshot.Count; i++)
                {
                    MonsterController monster = snapshot[i];
                    if (monster == null || monster.IsResolved ||
                        !monster.gameObject.activeInHierarchy)
                        continue;
                    monster.ApplyDamage(packet);
                }
            }

            int gold = combo * _runtime.Balance.ComboGoldPerCount;
            if (gold > 0)
                _gameManager.AddGold(gold);
            ComboCashedOut?.Invoke(combo, damage, gold);
            Debug.Log(
                $"[CrossDefense] Combo cashout: {combo} combo, " +
                $"all enemies {damage:0.#} damage, gold +{gold}",
                this);
        }

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
