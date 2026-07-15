using System.Collections;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>벤치↔필드 배치, 자유 이동 전투, 재배치와 3머지를 한 인스턴스 ID 기준으로 관리한다.</summary>
    [DisallowMultipleComponent]
    public sealed class SummonedUnitManager : MonoBehaviour
    {
        const float MinUnitSpacing = 0.72f;
        const float MergeDropDistance = 0.7f;

        readonly HashSet<MonsterController> _monsters = new();
        readonly List<SummonedUnitController> _units = new();

        GameManager _gameManager;
        SummonManager _summonManager;
        Transform _summoner;
        Camera _camera;
        Transform _unitRoot;
        Transform _unitTemplate;
        bool _autoDeploy;
        SummonedUnitController _draggedUnit;
        SummonedUnitController _mergeTarget;
        Vector3 _dragOrigin;
        SummonedUnitController _benchPreview;
        SummonUnitInstance _benchDragInstance;

        public IReadOnlyList<SummonedUnitController> Units => _units;
        public IReadOnlyCollection<MonsterController> Monsters => _monsters;
        public CombatProjectileService Projectiles { get; private set; }
        public bool CanUnitsFight => _gameManager != null && !_gameManager.IsRunOver && _gameManager.Phase == RunPhase.InWave;
        public bool IsDragging => _draggedUnit != null || _benchPreview != null;

        public void Initialize(
            GameManager gameManager,
            SummonManager summonManager,
            Transform summoner,
            Camera worldCamera,
            bool autoDeploy)
        {
            _gameManager = gameManager;
            _summonManager = summonManager;
            _summoner = summoner;
            _camera = worldCamera != null ? worldCamera : Camera.main;
            _autoDeploy = autoDeploy;

            var rootObject = new GameObject("SummonedUnits");
            _unitRoot = rootObject.transform;
            _unitRoot.SetParent(transform, false);
            Projectiles = new CombatProjectileService(transform, () => _monsters, () => CanUnitsFight);
            _unitTemplate = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseSummonedUnit",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 4;
                    var collider = gameObject.AddComponent<CircleCollider2D>();
                    collider.radius = 0.38f;
                    gameObject.AddComponent<AnimatedOutlineFeedback>();
                    gameObject.AddComponent<SummonedUnitController>();
                },
                16,
                128);

            _gameManager.MonsterSpawned += OnMonsterSpawned;
            _gameManager.MonsterResolved += OnMonsterResolved;
            _summonManager.UnitAdded += OnUnitAdded;
        }

        void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.MonsterSpawned -= OnMonsterSpawned;
                _gameManager.MonsterResolved -= OnMonsterResolved;
            }
            if (_summonManager != null)
                _summonManager.UnitAdded -= OnUnitAdded;
        }

        public MonsterController FindTarget(SummonedUnitController unit)
        {
            if (unit.Data.AttackStyle == SummonAttackStyle.Area && unit.Data.AreaRadius > 0f)
                return FindDensestAreaTarget(unit);

            MonsterController best = null;
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
            _monsters.RemoveWhere(monster => monster == null || !monster.gameObject.activeInHierarchy || monster.IsResolved);
            foreach (var candidate in _monsters)
            {
                int neighbors = 0;
                foreach (var nearby in _monsters)
                {
                    if ((nearby.transform.position - candidate.transform.position).sqrMagnitude <= radiusSq)
                        neighbors++;
                }

                float distanceSq = (candidate.transform.position - unit.transform.position).sqrMagnitude;
                if (neighbors < bestNeighbors || neighbors == bestNeighbors && distanceSq >= bestDistanceSq)
                    continue;
                best = candidate;
                bestNeighbors = neighbors;
                bestDistanceSq = distanceSq;
            }
            return best;
        }

        public Vector3 CalculateSeparation(SummonedUnitController source)
        {
            Vector3 separation = Vector3.zero;
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || unit.IsDragging) continue;
                Vector3 offset = source.transform.position - unit.transform.position;
                float distanceSq = offset.sqrMagnitude;
                if (distanceSq <= Mathf.Epsilon || distanceSq >= MinUnitSpacing * MinUnitSpacing) continue;
                separation += offset.normalized * (1f - Mathf.Sqrt(distanceSq) / MinUnitSpacing);
            }
            return separation * 0.75f;
        }

        public float GetSupportAttackSpeedMultiplier(SummonedUnitController source)
        {
            float multiplier = 1f;
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || unit.Data == null ||
                    unit.Data.AttackStyle != SummonAttackStyle.Support)
                    continue;
                float radius = unit.Data.SupportRadius;
                if ((unit.transform.position - source.transform.position).sqrMagnitude <= radius * radius)
                    multiplier += unit.Data.SupportAttackSpeedBonus * (1f + 0.15f * unit.Instance.Rank);
            }
            return multiplier;
        }

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
            {
                _draggedUnit.transform.position = _dragOrigin;
                _draggedUnit.SetDragging(false);
                SetMergeTarget(null);
                _draggedUnit = null;
                return true;
            }

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
            if (register) _units.Add(unit);
            return unit;
        }

        void ReleaseUnit(SummonedUnitController unit)
        {
            if (unit == null) return;
            _units.Remove(unit);
            unit.ResetForPool();
            RuntimePoolService.Despawn(unit.transform);
        }

        static void ReleasePreview(SummonedUnitController unit)
        {
            if (unit == null) return;
            unit.ResetForPool();
            RuntimePoolService.Despawn(unit.transform);
        }

        SummonedUnitController FindMergeTarget(SummonedUnitController source)
        {
            if (source?.Instance == null || source.Instance.Rank >= 3) return null;
            SummonedUnitController nearest = null;
            float bestDistanceSq = MergeDropDistance * MergeDropDistance;
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || !CanMergePair(source, unit)) continue;
                float distanceSq = (unit.transform.position - source.transform.position).sqrMagnitude;
                if (distanceSq > bestDistanceSq || !HasThirdMatch(source, unit)) continue;
                bestDistanceSq = distanceSq;
                nearest = unit;
            }
            return nearest;
        }

        bool HasThirdMatch(SummonedUnitController source, SummonedUnitController target)
        {
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || unit == target) continue;
                if (CanMergePair(source, unit)) return true;
            }
            foreach (var candidate in _summonManager.Bench)
            {
                if (candidate.Unit != null && candidate.Unit.UnitId == source.Data.UnitId &&
                    candidate.Rank == source.Instance.Rank)
                    return true;
            }
            return false;
        }

        bool TryMerge(SummonedUnitController source, SummonedUnitController target)
        {
            if (!CanMergePair(source, target) || source.Instance.Rank >= 3) return false;
            SummonedUnitController thirdField = null;
            foreach (var unit in _units)
            {
                if (unit == null || unit == source || unit == target) continue;
                if (CanMergePair(source, unit))
                {
                    thirdField = unit;
                    break;
                }
            }

            if (thirdField == null &&
                !_summonManager.TryRemoveBenchMatch(source.Data.UnitId, source.Instance.Rank, out _))
                return false;

            if (thirdField != null) ReleaseUnit(thirdField);
            ReleaseUnit(source);
            if (!target.Instance.TryPromote()) return false;
            target.RefreshRankVisual();
            target.SetDragging(false);
            target.Outline?.SetState(UnitOutlineState.MergeTarget);
            StartCoroutine(ClearOutlineAfter(target, 0.45f));
            return true;
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
                TryAutoDeploy(instance.InstanceId);
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
