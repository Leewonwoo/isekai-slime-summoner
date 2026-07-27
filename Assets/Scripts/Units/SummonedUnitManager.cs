using System;
using System.Collections;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>벤치↔필드 배치, 자유 이동 전투, 재배치와 2머지를 한 인스턴스 ID 기준으로 관리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class SummonedUnitManager : MonoBehaviour
    {
        const float MinUnitSpacing = 0.72f;
        const float MergeDropDistance = 0.7f;

        readonly HashSet<MonsterController> _monsters = new();
        readonly List<SummonedUnitController> _units = new();
        readonly List<CombatPbdBody> _pbdBodies = new(64);
        readonly List<SummonedUnitController> _pbdUnits = new(16);
        readonly List<MonsterController> _pbdMonsters = new(48);
        readonly List<SummonedUnitController> _formationUnits = new(16);

        GameManager _gameManager;
        SummonManager _summonManager;
        Transform _summoner;
        Camera _camera;
        Transform _unitRoot;
        Transform _unitTemplate;
        bool _autoDeploy;
        float _targetSearchRange = 4.5f;
        CombatPbdSettings _combatPbd = new();
        SummonFormationSettings _formation = new();
        bool _formationActive;
        SummonedUnitController _draggedUnit;
        SummonedUnitController _mergeTarget;
        Vector3 _dragOrigin;
        SummonedUnitController _benchPreview;
        SummonUnitInstance _benchDragInstance;
        float _mergeFrenzyUntil;

        public IReadOnlyList<SummonedUnitController> Units => _units;
        public IReadOnlyCollection<MonsterController> Monsters => _monsters;
        public CombatProjectileService Projectiles { get; private set; }
        public CombatEffectService Effects { get; private set; }
        public SlimeAttackEffectService AttackEffects { get; private set; }
        public bool CanUnitsFight => _gameManager != null && !_gameManager.IsRunOver && _gameManager.Phase == RunPhase.InWave;
        public bool IsGameplayPaused => _gameManager?.IsGameplayPaused ?? false;
        public bool IsDragging => _draggedUnit != null || _benchPreview != null;
        public float TargetSearchRange => _targetSearchRange;
        public float SlimeAttackSpeedMultiplier =>
            (_gameManager?.SlimeAttackSpeedMultiplier ?? 1f) *
            (Time.time < _mergeFrenzyUntil
                ? _gameManager?.RunTraits?.MergeFrenzyAttackSpeedMultiplier ?? 1f
                : 1f);
        public float SlimeReviveFraction => _gameManager?.RunTraits?.SlimeReviveFraction ?? 0f;

        public event Action<IReadOnlyList<SummonedUnitController>> UnitsChanged;

        public void Initialize(
            GameManager gameManager,
            SummonManager summonManager,
            Transform summoner,
            Camera worldCamera,
            bool autoDeploy,
            float targetSearchRange,
            CombatPbdSettings combatPbd = null,
            SummonFormationSettings formation = null)
        {
            _gameManager = gameManager;
            _summonManager = summonManager;
            _summoner = summoner;
            _camera = worldCamera != null ? worldCamera : Camera.main;
            _autoDeploy = autoDeploy;
            _targetSearchRange = Mathf.Max(0.1f, targetSearchRange);
            _combatPbd = combatPbd ?? new CombatPbdSettings();
            _formation = formation ?? new SummonFormationSettings();
            _formationActive = ShouldUseFormation(_gameManager.Phase);

            var rootObject = new GameObject("SummonedUnits");
            _unitRoot = rootObject.transform;
            _unitRoot.SetParent(transform, false);
            Projectiles = new CombatProjectileService(transform, () => _monsters, () => CanUnitsFight);
            Effects = new CombatEffectService(transform, () => CanUnitsFight);
            AttackEffects = new SlimeAttackEffectService(
                transform,
                () => CanUnitsFight && !IsGameplayPaused);
            _unitTemplate = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseSummonedUnit",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 4;
                    var collider = gameObject.AddComponent<CircleCollider2D>();
                    collider.radius = 0.38f;
                    gameObject.AddComponent<AnimatedOutlineFeedback>();
                    gameObject.AddComponent<WorldHealthBar>();
                    gameObject.AddComponent<SupportAuraVisual>();
                    gameObject.AddComponent<SummonedUnitController>();
                },
                16,
                128);

            _gameManager.MonsterSpawned += OnMonsterSpawned;
            _gameManager.MonsterResolved += OnMonsterResolved;
            _gameManager.PhaseChanged += OnPhaseChanged;
            _summonManager.UnitAdded += OnUnitAdded;
        }

        void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.MonsterSpawned -= OnMonsterSpawned;
                _gameManager.MonsterResolved -= OnMonsterResolved;
                _gameManager.PhaseChanged -= OnPhaseChanged;
            }
            if (_summonManager != null)
                _summonManager.UnitAdded -= OnUnitAdded;
        }

        void LateUpdate()
        {
            if (IsGameplayPaused)
                return;

            if (CanUnitsFight)
            {
                SolvePbd();
                return;
            }

            if (!_formationActive || !ShouldUseFormation(_gameManager?.Phase ?? RunPhase.Defeat)) return;

            bool allArrived = MoveUnitsIntoFormation();
            SolvePbd();
            if (allArrived)
                _formationActive = false;
        }

        void SolvePbd()
        {
            if (_combatPbd == null || !_combatPbd.Enabled) return;

            BuildPbdBodies();
            CombatPbdSolver.Solve(_pbdBodies, _combatPbd, Time.deltaTime);
            ApplyPbdPositions();
        }

        bool MoveUnitsIntoFormation()
        {
            if (_summoner == null || _formation == null || !_formation.Enabled)
            {
                StopFormationMotion();
                return true;
            }

            _formationUnits.Clear();
            foreach (var unit in _units)
            {
                if (unit == null || unit.IsDragging || unit.IsDefeated || !unit.gameObject.activeInHierarchy ||
                    unit.Data == null || unit.Instance == null)
                    continue;
                _formationUnits.Add(unit);
            }
            _formationUnits.Sort(CompareFormationUnits);

            bool allArrived = true;
            Vector2 center = _summoner.position;
            for (int i = 0; i < _formationUnits.Count; i++)
            {
                Vector2 slot = SummonFormationPlanner.GetSlot(center, i, _formationUnits.Count, _formation);
                Vector3 target = ClampToField(new Vector3(slot.x, slot.y, 0f));
                if (!_formationUnits[i].MoveTowardFormation(
                        target,
                        _formation.ReturnSpeed,
                        _formation.StoppingDistance))
                    allArrived = false;
            }
            return allArrived;
        }

        static int CompareFormationUnits(SummonedUnitController first, SummonedUnitController second)
        {
            int firstRole = SummonFormationPlanner.GetRolePriority(first.Data.AttackStyle);
            int secondRole = SummonFormationPlanner.GetRolePriority(second.Data.AttackStyle);
            int roleComparison = firstRole.CompareTo(secondRole);
            return roleComparison != 0
                ? roleComparison
                : first.Instance.InstanceId.CompareTo(second.Instance.InstanceId);
        }

        bool ShouldUseFormation(RunPhase phase) =>
            _formation != null && _formation.Enabled && phase is RunPhase.Prepare or RunPhase.Intermission or
                RunPhase.TraitChoice or RunPhase.Merchant or RunPhase.Victory;

        void OnPhaseChanged(RunPhase phase)
        {
            _formationActive = ShouldUseFormation(phase);
            if (!_formationActive)
                StopFormationMotion();
            if (phase == RunPhase.InWave)
                PrepareUnitsForWave();
        }

        void PrepareUnitsForWave()
        {
            float shieldFraction = _gameManager?.RunTraits?.SlimeShieldFraction ?? 0f;
            for (int i = 0; i < _units.Count; i++)
                _units[i]?.PrepareForWave(shieldFraction);
        }

        public void ApplyCurrentWaveRunState(SummonedUnitController unit)
        {
            if (unit == null)
                return;
            float shieldFraction = _gameManager?.Phase == RunPhase.InWave
                ? _gameManager?.RunTraits?.SlimeShieldFraction ?? 0f
                : 0f;
            unit.PrepareForWave(shieldFraction);
        }

        void StopFormationMotion()
        {
            foreach (var unit in _units)
                if (unit != null)
                    unit.StopFormationMotion();
        }

        public MonsterController FindTarget(SummonedUnitController unit)
        {
            if (unit.Data.AttackStyle == SummonAttackStyle.Area && unit.Data.AreaRadius > 0f)
                return FindDensestAreaTarget(unit);

            MonsterController best = null;
            float searchRangeSq = _targetSearchRange * _targetSearchRange;
            float bestValue = unit.Data.TargetPriority switch
            {
                SummonTargetPriority.LowestHp => float.MaxValue,
                SummonTargetPriority.HighestHp => float.MinValue,
                SummonTargetPriority.Farthest => float.MinValue,
                _ => float.MaxValue,
            };

            _monsters.RemoveWhere(monster => monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var monster in _monsters)
            {
                float distanceSq = (monster.transform.position - unit.transform.position).sqrMagnitude;
                if (distanceSq > searchRangeSq) continue;
                float value = unit.Data.TargetPriority switch
                {
                    SummonTargetPriority.LowestHp => monster.CurrentHp,
                    SummonTargetPriority.HighestHp => monster.CurrentHp,
                    SummonTargetPriority.Farthest => distanceSq,
                    _ => distanceSq,
                };
                bool better = unit.Data.TargetPriority switch
                {
                    SummonTargetPriority.HighestHp or SummonTargetPriority.Farthest => value > bestValue,
                    _ => value < bestValue,
                };
                if (!better) continue;
                best = monster;
                bestValue = value;
            }

            return best;
        }

        MonsterController FindDensestAreaTarget(SummonedUnitController unit)
        {
            MonsterController best = null;
            int bestNeighbors = -1;
            float bestDistanceSq = float.MaxValue;
            float radiusSq = unit.Data.AreaRadius * unit.Data.AreaRadius;
            float searchRangeSq = _targetSearchRange * _targetSearchRange;
            _monsters.RemoveWhere(monster => monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var candidate in _monsters)
            {
                float distanceSq = (candidate.transform.position - unit.transform.position).sqrMagnitude;
                if (distanceSq > searchRangeSq) continue;

                int neighbors = 0;
                foreach (var nearby in _monsters)
                {
                    if ((nearby.transform.position - candidate.transform.position).sqrMagnitude <= radiusSq)
                        neighbors++;
                }

                if (neighbors < bestNeighbors || neighbors == bestNeighbors && distanceSq >= bestDistanceSq)
                    continue;
                best = candidate;
                bestNeighbors = neighbors;
                bestDistanceSq = distanceSq;
            }
            return best;
        }

        void BuildPbdBodies()
        {
            _pbdBodies.Clear();
            _pbdUnits.Clear();
            _pbdMonsters.Clear();

            foreach (var unit in _units)
            {
                if (unit == null || unit.IsDragging || unit.IsDefeated || !unit.gameObject.activeInHierarchy ||
                    unit.Data == null)
                    continue;

                float scale = Mathf.Max(0.1f, unit.Data.ScaleAtRank(unit.Instance.Rank));
                _pbdUnits.Add(unit);
                _pbdBodies.Add(new CombatPbdBody(
                    unit.transform.position,
                    unit.CombatRadius,
                    _combatPbd.SummonedUnitInverseMass / scale,
                    CombatPbdTeam.SummonedUnit,
                    unit.Data.AttackRange,
                    unit.PbdPreviousPosition));
            }

            _monsters.RemoveWhere(monster => monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var monster in _monsters)
            {
                if (monster.Data == null) continue;
                float scale = Mathf.Max(0.1f, monster.Data.SizeMultiplier);
                _pbdMonsters.Add(monster);
                _pbdBodies.Add(new CombatPbdBody(
                    monster.transform.position,
                    monster.CombatRadius,
                    _combatPbd.MonsterInverseMass / scale,
                    CombatPbdTeam.Monster,
                    monster.AttackRange,
                    monster.PbdPreviousPosition));
            }
        }

        void ApplyPbdPositions()
        {
            int bodyIndex = 0;
            foreach (var unit in _pbdUnits)
            {
                if (unit != null && bodyIndex < _pbdBodies.Count)
                {
                    unit.transform.position = ClampToField(_pbdBodies[bodyIndex].Position);
                    unit.SetPbdResolvedPosition(unit.transform.position);
                }
                bodyIndex++;
            }

            foreach (var monster in _pbdMonsters)
            {
                if (monster != null && bodyIndex < _pbdBodies.Count)
                {
                    Vector2 position = _pbdBodies[bodyIndex].Position;
                    monster.transform.position = new Vector3(position.x, position.y, 0f);
                    monster.SetPbdResolvedPosition(monster.transform.position);
                }
                bodyIndex++;
            }
        }

        public float GetSupportAttackSpeedMultiplier(SummonedUnitController source)
        {
            SummonedUnitController strongest = FindStrongestSupportFor(source);
            if (strongest == null)
                return 1f;
            strongest.PresentSupportBuff();
            return 1f + SupportStrength(strongest);
        }

        public bool TryHealWithSupport(SummonedUnitController healer)
        {
            if (!CanUnitsFight || IsGameplayPaused || healer?.Data == null ||
                healer.Instance == null || healer.Data.AttackStyle != SummonAttackStyle.Support)
                return false;

            SummonedUnitController target = null;
            float lowestRatio = 1f;
            foreach (var unit in _units)
            {
                if (unit == null || unit == healer || unit.IsDefeated || unit.MaxHp <= 0f ||
                    unit.CurrentHp >= unit.MaxHp)
                    continue;
                float radius = healer.Data.SupportRadius;
                if ((unit.transform.position - healer.transform.position).sqrMagnitude > radius * radius ||
                    FindStrongestSupportFor(unit) != healer)
                    continue;
                float ratio = unit.CurrentHp / unit.MaxHp;
                if (ratio >= lowestRatio)
                    continue;
                target = unit;
                lowestRatio = ratio;
            }
            if (target == null)
                return false;

            float overdrive = healer.IsStar3AuraOverdriveActive
                ? Mathf.Max(1f, healer.Data.Star3SkillStrength)
                : 1f;
            float amount = target.MaxHp *
                healer.Data.SupportHealFractionAtRank(healer.Instance.Rank) *
                overdrive;
            float healed = target.Heal(amount);
            if (healed <= 0f)
                return false;
            healer.PresentSupportBuff();
            PresentDamageNumber(target.GetFloatingTextAnchor(), healed, DamageTextKind.Healing);
            AttackEffects?.PlaySupport(
                healer.Instance.Rank,
                healer.GetHeadEffectAnchor(),
                target.GetFloatingTextAnchor());
            return true;
        }

        SummonedUnitController FindStrongestSupportFor(SummonedUnitController target)
        {
            if (target == null)
                return null;
            SummonedUnitController strongest = null;
            float strongestValue = 0f;
            foreach (SummonedUnitController candidate in _units)
            {
                if (candidate == null || candidate == target || candidate.IsDefeated ||
                    candidate.Data == null || candidate.Instance == null ||
                    candidate.Data.AttackStyle != SummonAttackStyle.Support)
                    continue;
                float radius = candidate.Data.SupportRadius;
                if ((candidate.transform.position - target.transform.position).sqrMagnitude > radius * radius)
                    continue;
                float value = SupportStrength(candidate);
                if (value > strongestValue + 0.0001f ||
                    Mathf.Approximately(value, strongestValue) &&
                    (strongest == null || candidate.Instance.InstanceId < strongest.Instance.InstanceId))
                {
                    strongest = candidate;
                    strongestValue = value;
                }
            }
            return strongest;
        }

        static float SupportStrength(SummonedUnitController support)
        {
            float overdrive = support.IsStar3AuraOverdriveActive
                ? Mathf.Max(1f, support.Data.Star3SkillStrength)
                : 1f;
            return support.Data.SupportAttackSpeedBonus *
                (1f + 0.15f * support.Instance.Rank) *
                overdrive;
        }

        public bool TryCastStar3Skill(SummonedUnitController unit)
        {
            if (unit?.Data == null || unit.Instance == null ||
                unit.Instance.Rank != SummonRank.MaxInternalRank ||
                !unit.Data.HasStar3Skill)
                return false;

            SummonUnitData data = unit.Data;
            switch (data.Star3SkillModeValue)
            {
                case Star3SkillMode.SelfArea:
                    if (!HasMonsterWithin(unit.transform.position, data.Star3SkillRadius))
                        return false;
                    ApplyAreaDamage(
                        unit.transform.position,
                        data.Star3SkillRadius,
                        BuildStar3SkillPacket(unit));
                    PlayStar3Effect(unit, unit.transform.position);
                    return true;

                case Star3SkillMode.TargetArea:
                {
                    MonsterController target = FindDensestSkillTarget(
                        unit.transform.position,
                        _targetSearchRange,
                        data.Star3SkillRadius);
                    if (target == null)
                        return false;
                    ApplyAreaDamage(
                        target.transform.position,
                        data.Star3SkillRadius,
                        BuildStar3SkillPacket(unit));
                    PlayStar3Effect(unit, target.transform.position);
                    return true;
                }

                case Star3SkillMode.PiercingProjectile:
                {
                    MonsterController target = FindFarthestSkillTarget(
                        unit.transform.position,
                        _targetSearchRange);
                    if (target == null)
                        return false;
                    PlayStar3Effect(unit, unit.transform.position);
                    Sprite projectileSprite = data.ProjectileSpriteAtRank(unit.Instance.Rank);
                    Projectiles.Fire(
                        unit.transform.position,
                        target,
                        projectileSprite != null ? projectileSprite : data.WorldSprite,
                        BuildStar3SkillPacket(unit),
                        data.ProjectileSpeed,
                        0.58f * data.Star3SkillVisualScale,
                        0f,
                        data.Star3SkillPierceCount,
                        linePierce: true);
                    return true;
                }

                case Star3SkillMode.AuraOverdrive:
                    if (!HasAllyWithin(unit, data.SupportRadius))
                        return false;
                    unit.ActivateStar3AuraOverdrive(data.Star3SkillDuration);
                    PlayStar3Effect(unit, unit.transform.position);
                    return true;

                default:
                    return false;
            }
        }

        DamagePacket BuildStar3SkillPacket(SummonedUnitController unit)
        {
            SummonUnitData data = unit.Data;
            float rankDamage = data.DamageAtRank(unit.Instance.Rank) *
                unit.Instance.DamageMultiplier;
            return new DamagePacket(
                unit,
                ModifySlimeDamage(rankDamage * data.Star3SkillDamageMultiplier),
                data.Attribute,
                data.Star3SkillSlowPercent,
                data.Star3SkillSlowDuration,
                ModifySlimeDamage(rankDamage * data.Star3SkillDotMultiplier),
                data.Star3SkillDotDuration);
        }

        void PlayStar3Effect(SummonedUnitController unit, Vector3 position)
        {
            SummonUnitData data = unit.Data;
            Color color = data.UnitId switch
            {
                "watergun-slime" => new Color(0.58f, 0.9f, 1f),
                "buff-slime" => new Color(1f, 0.82f, 0.42f),
                _ => Color.white,
            };
            float rotation = (unit.Instance.InstanceId * 37) % 360;
            if (data.Star3SkillEffectFrames != null && data.Star3SkillEffectFrames.Length > 0)
            {
                Effects?.PlayFrames(
                    data.UnitId == "explosion-slime" ? unit.GetHeadEffectAnchor() : position,
                    data.Star3SkillEffectFrames,
                    color,
                    data.Star3SkillVisualScale,
                    18f);
                return;
            }
            Effects?.Play(
                position,
                data.Star3SkillEffectSprite,
                color,
                data.Star3SkillVisualScale,
                rotation);
        }

        bool HasMonsterWithin(Vector3 center, float radius)
        {
            float radiusSq = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
            _monsters.RemoveWhere(monster =>
                monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var monster in _monsters)
            {
                if ((monster.transform.position - center).sqrMagnitude <= radiusSq)
                    return true;
            }
            return false;
        }

        bool HasAllyWithin(SummonedUnitController source, float radius)
        {
            float radiusSq = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || unit.IsDefeated ||
                    !unit.gameObject.activeInHierarchy)
                    continue;
                if ((unit.transform.position - source.transform.position).sqrMagnitude <= radiusSq)
                    return true;
            }
            return false;
        }

        MonsterController FindFarthestSkillTarget(Vector3 origin, float range)
        {
            MonsterController best = null;
            float bestDistanceSq = -1f;
            float rangeSq = range * range;
            _monsters.RemoveWhere(monster =>
                monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var monster in _monsters)
            {
                float distanceSq = (monster.transform.position - origin).sqrMagnitude;
                if (distanceSq > rangeSq || distanceSq <= bestDistanceSq)
                    continue;
                best = monster;
                bestDistanceSq = distanceSq;
            }
            return best;
        }

        MonsterController FindDensestSkillTarget(Vector3 origin, float range, float radius)
        {
            MonsterController best = null;
            int bestNeighbors = -1;
            float bestDistanceSq = float.MaxValue;
            float rangeSq = range * range;
            float radiusSq = radius * radius;
            _monsters.RemoveWhere(monster =>
                monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var candidate in _monsters)
            {
                float distanceSq = (candidate.transform.position - origin).sqrMagnitude;
                if (distanceSq > rangeSq)
                    continue;
                int neighbors = 0;
                foreach (var nearby in _monsters)
                {
                    if ((nearby.transform.position - candidate.transform.position).sqrMagnitude <= radiusSq)
                        neighbors++;
                }
                if (neighbors < bestNeighbors ||
                    neighbors == bestNeighbors && distanceSq >= bestDistanceSq)
                    continue;
                best = candidate;
                bestNeighbors = neighbors;
                bestDistanceSq = distanceSq;
            }
            return best;
        }

        public float ModifySlimeDamage(float baseDamage) =>
            _gameManager?.ModifySlimeDamage(baseDamage) ?? Mathf.Max(0f, baseDamage);

        public void PresentDamageNumber(
            Vector3 worldPosition,
            float amount,
            DamageTextKind kind) =>
            _gameManager?.PresentDamageNumber(worldPosition, amount, kind);

        public void ApplyAreaDamage(Vector3 center, float radius, DamagePacket packet)
        {
            float radiusSq = radius * radius;
            var snapshot = new List<MonsterController>(_monsters);
            foreach (var monster in snapshot)
            {
                if (monster == null || monster.IsResolved || !monster.gameObject.activeInHierarchy) continue;
                if ((monster.transform.position - center).sqrMagnitude <= radiusSq)
                    monster.ApplyDamage(packet);
            }
        }

        public bool TryAutoDeploy(int instanceId)
        {
            if (_summonManager == null || !_summonManager.TryTakeFromBench(instanceId, out var instance))
                return false;
            if (!TryFindAutoPlacement(out var position))
            {
                _summonManager.ReturnToBench(instance);
                return false;
            }
            var spawned = SpawnUnit(instance, position, true);
            if (spawned != null) return true;
            _summonManager.ReturnToBench(instance);
            return false;
        }

        public bool BeginBenchDrag(SummonUnitInstance instance, Vector2 screenPosition)
        {
            if (instance == null || IsDragging || !TryScreenToWorld(screenPosition, out var world)) return false;
            _benchDragInstance = instance;
            _benchPreview = SpawnUnit(instance, ClampToField(world), false);
            if (_benchPreview == null)
            {
                _benchDragInstance = null;
                return false;
            }
            _benchPreview.SetDragging(true, UnitOutlineState.ValidPlacement);
            _formationActive = false;
            StopFormationMotion();
            return true;
        }

        public void UpdateBenchDrag(Vector2 screenPosition)
        {
            if (_benchPreview == null || !TryScreenToWorld(screenPosition, out var world)) return;
            world = ClampToField(world);
            _benchPreview.transform.position = world;
            _benchPreview.SetDragFeedback(IsScreenPositionInField(screenPosition) && IsPlacementValid(world, null)
                ? UnitOutlineState.ValidPlacement
                : UnitOutlineState.InvalidPlacement);
        }

        public bool EndBenchDrag(Vector2 screenPosition)
        {
            if (_benchPreview == null || _benchDragInstance == null) return false;
            if (TryScreenToWorld(screenPosition, out var world))
                _benchPreview.transform.position = ClampToField(world);

            SummonUnitInstance taken = null;
            bool valid = IsScreenPositionInField(screenPosition) &&
                IsPlacementValid(_benchPreview.transform.position, null) &&
                _summonManager.TryTakeFromBench(_benchDragInstance.InstanceId, out taken);
            if (valid)
            {
                _benchPreview.Initialize(this, taken);
                _benchPreview.SetDragging(false);
                _units.Add(_benchPreview);
                UnitsChanged?.Invoke(_units);
            }
            else
            {
                ReleasePreview(_benchPreview);
            }

            _benchPreview = null;
            _benchDragInstance = null;
            return valid;
        }

        public bool BeginFieldDrag(SummonedUnitController unit)
        {
            if (unit == null || IsDragging || !_units.Contains(unit)) return false;
            _draggedUnit = unit;
            _dragOrigin = unit.transform.position;
            unit.SetDragging(true);
            _formationActive = false;
            StopFormationMotion();
            return true;
        }

        public void UpdateFieldDrag(Vector3 worldPosition)
        {
            if (_draggedUnit == null) return;
            _draggedUnit.transform.position = ClampToField(worldPosition);
            SetMergeTarget(FindMergeTarget(_draggedUnit));
            _draggedUnit.SetDragFeedback(_mergeTarget != null
                ? UnitOutlineState.MergeTarget
                : IsPlacementValid(_draggedUnit.transform.position, _draggedUnit)
                    ? UnitOutlineState.ValidPlacement
                    : UnitOutlineState.InvalidPlacement);
        }

        public bool EndFieldDrag(Vector3 worldPosition, bool releasedInsideField = true)
        {
            if (_draggedUnit == null) return false;
            if (!releasedInsideField)
                return CancelFieldDrag();

            UpdateFieldDrag(worldPosition);
            var source = _draggedUnit;
            bool merged = _mergeTarget != null && TryMerge(source, _mergeTarget);
            if (!merged)
            {
                if (!IsPlacementValid(source.transform.position, source))
                    source.transform.position = _dragOrigin;
                source.SetDragging(false);
            }
            if (merged)
                _mergeTarget = null;
            else
                SetMergeTarget(null);
            _draggedUnit = null;
            return true;
        }

        public bool CancelFieldDrag()
        {
            if (_draggedUnit == null) return false;
            _draggedUnit.transform.position = _dragOrigin;
            _draggedUnit.SetDragging(false);
            SetMergeTarget(null);
            _draggedUnit = null;
            return true;
        }

        public bool IsScreenPositionInField(Vector2 screenPosition)
        {
            if (_camera == null) return false;
            Vector3 viewport = _camera.ScreenToViewportPoint(screenPosition);
            return viewport.x >= 0.04f && viewport.x <= 0.96f &&
                viewport.y >= 0.43f && viewport.y <= 0.91f;
        }

        public Vector3 ClampToField(Vector3 position)
        {
            if (_camera == null) return position;
            Vector3 min = _camera.ViewportToWorldPoint(new Vector3(0.04f, 0.43f, -_camera.transform.position.z));
            Vector3 max = _camera.ViewportToWorldPoint(new Vector3(0.96f, 0.91f, -_camera.transform.position.z));
            position.x = Mathf.Clamp(position.x, min.x, max.x);
            position.y = Mathf.Clamp(position.y, min.y, max.y);
            position.z = 0f;
            return position;
        }

        bool TryFindAutoPlacement(out Vector3 position)
        {
            Vector3 center = _summoner != null ? _summoner.position : transform.position;
            for (int ring = 0; ring < 3; ring++)
            {
                float radius = 1.15f + ring * 0.75f;
                int count = 8 + ring * 4;
                for (int i = 0; i < count; i++)
                {
                    float angle = (i + ring * 0.37f) / count * Mathf.PI * 2f;
                    Vector3 candidate = ClampToField(center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
                    if (!IsPlacementValid(candidate, null)) continue;
                    position = candidate;
                    return true;
                }
            }
            position = center;
            return false;
        }

        bool IsPlacementValid(Vector3 position, SummonedUnitController ignored)
        {
            if (_summoner != null && (position - _summoner.position).sqrMagnitude < 0.7f * 0.7f)
                return false;
            foreach (var unit in _units)
            {
                if (unit == null || unit == ignored) continue;
                if ((unit.transform.position - position).sqrMagnitude < MinUnitSpacing * MinUnitSpacing)
                    return false;
            }
            return true;
        }

        SummonedUnitController SpawnUnit(SummonUnitInstance instance, Vector3 position, bool register)
        {
            var spawned = RuntimePoolService.Spawn(_unitTemplate, position, Quaternion.identity, _unitRoot);
            if (spawned == null) return null;
            var unit = spawned.GetComponent<SummonedUnitController>();
            unit.Initialize(this, instance);
            if (register)
            {
                _units.Add(unit);
                UnitsChanged?.Invoke(_units);
            }
            return unit;
        }

        void ReleaseUnit(SummonedUnitController unit)
        {
            if (unit == null) return;
            bool removed = _units.Remove(unit);
            unit.ResetForPool();
            RuntimePoolService.Despawn(unit.transform);
            if (removed)
                UnitsChanged?.Invoke(_units);
        }

        public void NotifyUnitDefeated(SummonedUnitController unit)
        {
            if (unit == null || !_units.Contains(unit)) return;
            ReleaseUnit(unit);
        }

        public bool TryRestoreUnitHealth(int instanceId, float hpRatio)
        {
            foreach (SummonedUnitController unit in _units)
            {
                if (unit?.Instance?.InstanceId == instanceId)
                    return unit.RestoreHealthRatio(hpRatio);
            }
            return false;
        }

        static void ReleasePreview(SummonedUnitController unit)
        {
            if (unit == null) return;
            unit.ResetForPool();
            RuntimePoolService.Despawn(unit.transform);
        }

        SummonedUnitController FindMergeTarget(SummonedUnitController source)
        {
            if (source?.Instance == null ||
                source.Instance.Rank >= SummonRank.MaxInternalRank)
                return null;
            SummonedUnitController nearest = null;
            float bestDistanceSq = MergeDropDistance * MergeDropDistance;
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || !CanMergePair(source, unit)) continue;
                float distanceSq = (unit.transform.position - source.transform.position).sqrMagnitude;
                if (distanceSq > bestDistanceSq) continue;
                bestDistanceSq = distanceSq;
                nearest = unit;
            }
            return nearest;
        }

        bool TryMerge(SummonedUnitController source, SummonedUnitController target)
        {
            if (!CanMergePair(source, target) ||
                source.Instance.Rank >= SummonRank.MaxInternalRank)
                return false;

            ReleaseUnit(source);
            if (!target.Instance.TryPromote()) return false;
            target.RefreshRankVisual();
            target.SetDragging(false);
            target.Outline?.SetState(UnitOutlineState.MergeTarget);
            StartCoroutine(ClearOutlineAfter(target, 0.45f));
            ActivateMergeFrenzy();
            UnitsChanged?.Invoke(_units);
            return true;
        }

        void ActivateMergeFrenzy()
        {
            RunTraitProgression runTraits = _gameManager?.RunTraits;
            float duration = runTraits?.MergeFrenzyDuration ?? 0f;
            if (duration <= 0f)
                return;
            _mergeFrenzyUntil = Mathf.Max(_mergeFrenzyUntil, Time.time + duration);
            float healFraction = runTraits.MergeFrenzyHealFraction;
            if (healFraction <= 0f)
                return;
            for (int i = 0; i < _units.Count; i++)
            {
                SummonedUnitController unit = _units[i];
                if (unit != null)
                    unit.Heal(unit.MaxHp * healFraction);
            }
        }

        static bool CanMergePair(SummonedUnitController a, SummonedUnitController b) =>
            a?.Data != null && b?.Data != null && a.Instance != null && b.Instance != null &&
            a.Data.UnitId == b.Data.UnitId && a.Instance.Rank == b.Instance.Rank;

        void SetMergeTarget(SummonedUnitController target)
        {
            if (_mergeTarget == target) return;
            _mergeTarget?.Outline?.SetState(UnitOutlineState.None);
            _mergeTarget = target;
            _mergeTarget?.Outline?.SetState(UnitOutlineState.MergeTarget);
        }

        IEnumerator ClearOutlineAfter(SummonedUnitController unit, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (unit != null && !unit.IsDragging)
                unit.Outline?.SetState(UnitOutlineState.None);
        }

        bool TryScreenToWorld(Vector2 screenPosition, out Vector3 world)
        {
            if (_camera == null)
            {
                world = default;
                return false;
            }
            world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y,
                -_camera.transform.position.z));
            world.z = 0f;
            return true;
        }

        void OnUnitAdded(SummonUnitInstance instance)
        {
            if (_autoDeploy)
            {
                bool deployed = TryAutoDeploy(instance.InstanceId);
                if (deployed && _gameManager != null && ShouldUseFormation(_gameManager.Phase))
                    _formationActive = true;
            }
        }

        void OnMonsterSpawned(MonsterController monster, StageWave _, int __)
        {
            if (monster != null) _monsters.Add(monster);
        }

        void OnMonsterResolved(MonsterController monster)
        {
            if (monster != null) _monsters.Remove(monster);
        }
    }
}
