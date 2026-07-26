using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    [DisallowMultipleComponent]
    public sealed class SummonerBuffController : MonoBehaviour
    {
        const float CoreShieldFraction = 0.35f;
        const float CoreHealFractionPerTick = 0.02f;
        const float SlimeHealFractionPerTick = 0.04f;
        const float LegionDamageMultiplier = 1.2f;
        const float LegionAttackSpeedMultiplier = 1.25f;
        const float ResonanceDamageMultiplier = 1.3f;
        const float ResonanceRadiusMultiplier = 1.15f;
        const float ResonanceStatusMultiplier = 1.25f;
        const float AcceleratedCooldownMultiplier = 2f;

        readonly Dictionary<SummonerBuffId, float> _cooldowns = new();
        readonly Dictionary<SummonerBuffId, float> _activeDurations = new();
        readonly List<SummonerBuffId> _keys = new(5);

        GameManager _gameManager;
        SummonerBuffLoadout _loadout;
        CombatEffectService _effects;
        SkillParticleEffectService _particleEffects;
        Sprite _aegisSprite;
        float _lifeHealAccumulator;

        public SummonerBuffLoadout Loadout => _loadout;
        public float SlimeDamageMultiplier =>
            IsActive(SummonerBuffId.LegionCommand) ? LegionDamageMultiplier : 1f;
        public float SlimeAttackSpeedMultiplier =>
            IsActive(SummonerBuffId.LegionCommand) ? LegionAttackSpeedMultiplier : 1f;
        public float RelicCooldownRecoveryMultiplier =>
            IsActive(SummonerBuffId.TimeAcceleration) ? AcceleratedCooldownMultiplier : 1f;

        public event Action StateChanged;

        public void Initialize(
            GameManager gameManager,
            SummonerBuffLoadout loadout,
            Sprite aegisSprite)
        {
            _gameManager = gameManager;
            if (_loadout != null)
                _loadout.Changed -= OnLoadoutChanged;
            _loadout = loadout;
            if (_loadout != null)
                _loadout.Changed += OnLoadoutChanged;
            _aegisSprite = aegisSprite;
            _effects ??= new CombatEffectService(
                transform,
                rootName: "SummonerBuffEffects");
            _particleEffects ??= new SkillParticleEffectService(
                transform,
                () => _gameManager != null &&
                      !_gameManager.IsGameplayPaused &&
                      _gameManager.Phase == RunPhase.InWave,
                "SummonerBuffParticleEffects");
            StateChanged?.Invoke();
        }

        void Update()
        {
            if (_gameManager == null || _gameManager.IsGameplayPaused ||
                _gameManager.Phase != RunPhase.InWave)
                return;

            float deltaTime = Time.deltaTime;
            bool accelerated = IsActive(SummonerBuffId.TimeAcceleration);
            bool changed = TickCooldowns(deltaTime, accelerated);
            changed |= TickActiveDurations(deltaTime);
            if (changed)
                StateChanged?.Invoke();
        }

        void OnDestroy()
        {
            if (_loadout != null)
                _loadout.Changed -= OnLoadoutChanged;
        }

        public SummonerBuffId? EquippedAt(int slotIndex)
        {
            if (_loadout?.Equipped == null ||
                slotIndex < 0 || slotIndex >= _loadout.Equipped.Count)
                return null;
            return _loadout.Equipped[slotIndex];
        }

        public float RemainingCooldown(SummonerBuffId id) =>
            _cooldowns.TryGetValue(id, out float remaining)
                ? Mathf.Max(0f, remaining)
                : 0f;

        public float ActiveRemaining(SummonerBuffId id) =>
            _activeDurations.TryGetValue(id, out float remaining)
                ? Mathf.Max(0f, remaining)
                : 0f;

        public bool IsActive(SummonerBuffId id) => ActiveRemaining(id) > 0f;

        public bool PressSkillButton(int slotIndex)
        {
            SummonerBuffId? selected = EquippedAt(slotIndex);
            if (!selected.HasValue || _gameManager == null ||
                _gameManager.IsRunOver || _gameManager.IsGameplayPaused ||
                _gameManager.Phase != RunPhase.InWave ||
                RemainingCooldown(selected.Value) > 0f)
                return false;

            SummonerBuffDefinition definition = SummonerBuffCatalog.Get(selected.Value);
            switch (selected.Value)
            {
                case SummonerBuffId.Aegis:
                    _gameManager.GrantCoreShield(
                        _gameManager.MaxCoreHp * CoreShieldFraction,
                        definition.Duration);
                    _effects?.Play(
                        _gameManager.Summoner != null
                            ? _gameManager.Summoner.position
                            : transform.position,
                        _aegisSprite,
                        new Color(1f, 0.82f, 0.32f),
                        1.45f);
                    PlayPartyBuff(SummonerBuffId.Aegis, includeSlimes: false);
                    Activate(selected.Value, definition.Duration);
                    break;
                case SummonerBuffId.LifeBlessing:
                    Activate(selected.Value, definition.Duration);
                    _lifeHealAccumulator = 0f;
                    PlayPartyBuff(SummonerBuffId.LifeBlessing);
                    break;
                case SummonerBuffId.LegionCommand:
                    Activate(selected.Value, definition.Duration);
                    PlayPartyBuff(SummonerBuffId.LegionCommand);
                    break;
                case SummonerBuffId.ElementalResonance:
                    Activate(selected.Value, definition.Duration);
                    PlayPartyBuff(SummonerBuffId.ElementalResonance, includeSlimes: false);
                    break;
                case SummonerBuffId.TimeAcceleration:
                    Activate(selected.Value, definition.Duration);
                    PlayPartyBuff(SummonerBuffId.TimeAcceleration);
                    break;
                default:
                    return false;
            }

            _cooldowns[selected.Value] = definition.Cooldown;
            StateChanged?.Invoke();
            return true;
        }

        public bool GetRelicAmplification(
            out float damageMultiplier,
            out float radiusMultiplier,
            out float statusDurationMultiplier)
        {
            damageMultiplier = 1f;
            radiusMultiplier = 1f;
            statusDurationMultiplier = 1f;
            if (!IsActive(SummonerBuffId.ElementalResonance))
                return false;

            damageMultiplier = ResonanceDamageMultiplier;
            radiusMultiplier = ResonanceRadiusMultiplier;
            statusDurationMultiplier = ResonanceStatusMultiplier;
            return true;
        }

        public void ConsumeRelicAmplification()
        {
            if (!IsActive(SummonerBuffId.ElementalResonance))
                return;
            _activeDurations[SummonerBuffId.ElementalResonance] = 0f;
            StateChanged?.Invoke();
        }

        bool TickCooldowns(float deltaTime, bool accelerated)
        {
            bool changed = false;
            _keys.Clear();
            _keys.AddRange(_cooldowns.Keys);
            for (int i = 0; i < _keys.Count; i++)
            {
                SummonerBuffId id = _keys[i];
                float previous = _cooldowns[id];
                float multiplier =
                    accelerated && id != SummonerBuffId.TimeAcceleration
                        ? AcceleratedCooldownMultiplier
                        : 1f;
                float next = Mathf.Max(0f, previous - deltaTime * multiplier);
                _cooldowns[id] = next;
                changed |= Mathf.CeilToInt(previous) != Mathf.CeilToInt(next);
            }
            return changed;
        }

        bool TickActiveDurations(float deltaTime)
        {
            bool changed = false;
            _keys.Clear();
            _keys.AddRange(_activeDurations.Keys);
            for (int i = 0; i < _keys.Count; i++)
            {
                SummonerBuffId id = _keys[i];
                float previous = _activeDurations[id];
                if (previous <= 0f)
                    continue;
                float appliedDelta = Mathf.Min(previous, deltaTime);
                if (id == SummonerBuffId.LifeBlessing)
                {
                    _lifeHealAccumulator += appliedDelta;
                    while (_lifeHealAccumulator >= 1f)
                    {
                        _lifeHealAccumulator -= 1f;
                        HealParty();
                    }
                }

                float next = Mathf.Max(0f, previous - deltaTime);
                _activeDurations[id] = next;
                changed |= Mathf.CeilToInt(previous) != Mathf.CeilToInt(next);
            }
            return changed;
        }

        void HealParty()
        {
            float coreBefore = _gameManager.CoreHp;
            _gameManager.HealCore(_gameManager.MaxCoreHp * CoreHealFractionPerTick);
            float coreHealed = _gameManager.CoreHp - coreBefore;
            if (coreHealed > 0f)
            {
                Vector3 position = _gameManager.Summoner != null
                    ? _gameManager.Summoner.position
                    : transform.position;
                _gameManager.PresentDamageNumber(position, coreHealed, DamageTextKind.Healing);
                _particleEffects?.PlayBuff(SummonerBuffId.LifeBlessing, position, 0.85f);
            }

            IReadOnlyList<SummonedUnitController> units =
                _gameManager.SummonedUnitManager?.Units;
            if (units == null)
                return;
            for (int i = 0; i < units.Count; i++)
            {
                SummonedUnitController unit = units[i];
                if (unit == null || unit.IsDefeated)
                    continue;
                float healed = unit.Heal(unit.MaxHp * SlimeHealFractionPerTick);
                if (healed > 0f)
                {
                    _gameManager.PresentDamageNumber(
                        unit.GetFloatingTextAnchor(),
                        healed,
                        DamageTextKind.Healing);
                    _particleEffects?.PlayBuff(
                        SummonerBuffId.LifeBlessing,
                        unit.GetFloatingTextAnchor(),
                        0.7f);
                }
            }
        }

        void PlayPartyBuff(SummonerBuffId id, bool includeSlimes = true)
        {
            Vector3 summonerPosition = _gameManager.Summoner != null
                ? _gameManager.Summoner.position
                : transform.position;
            _particleEffects?.PlayBuff(id, summonerPosition, 1f);
            if (!includeSlimes)
                return;

            IReadOnlyList<SummonedUnitController> units =
                _gameManager.SummonedUnitManager?.Units;
            if (units == null)
                return;
            for (int i = 0; i < units.Count; i++)
            {
                SummonedUnitController unit = units[i];
                if (unit == null || unit.IsDefeated)
                    continue;
                _particleEffects?.PlayBuff(id, unit.GetFloatingTextAnchor(), 0.75f);
            }
        }

        void Activate(SummonerBuffId id, float duration)
        {
            float current = ActiveRemaining(id);
            _activeDurations[id] = Mathf.Max(current, duration);
        }

        void OnLoadoutChanged() => StateChanged?.Invoke();
    }
}
