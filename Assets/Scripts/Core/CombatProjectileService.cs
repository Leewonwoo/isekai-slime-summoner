using System;
using System.Collections.Generic;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    public readonly struct ProjectileImpactContext
    {
        public Vector3 Position { get; }
        public DamagePacket Packet { get; }
        public bool DidHit { get; }
        public bool PrimaryDefeated { get; }
        public bool WasSlimeResonating { get; }

        public ProjectileImpactContext(
            Vector3 position,
            DamagePacket packet,
            bool didHit,
            bool primaryDefeated,
            bool wasSlimeResonating)
        {
            Position = position;
            Packet = packet;
            DidHit = didHit;
            PrimaryDefeated = primaryDefeated;
            WasSlimeResonating = wasSlimeResonating;
        }
    }

    /// <summary>소환사와 소환수가 공유하는 PoolBoss 기반 투사체 서비스.</summary>
    public sealed class CombatProjectileService
    {
        public const int MaxActiveProjectiles = 48;

        readonly Transform _root;
        readonly Func<IReadOnlyCollection<MonsterController>> _monsterProvider;
        readonly Func<bool> _canFight;
        readonly Transform _template;
        int _activeCount;

        public int ActiveCount => Mathf.Max(0, _activeCount);

        public CombatProjectileService(
            Transform parent,
            Func<IReadOnlyCollection<MonsterController>> monsterProvider,
            Func<bool> canFight = null)
        {
            var rootObject = new GameObject("CombatProjectiles");
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            _monsterProvider = monsterProvider;
            _canFight = canFight;
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseCombatProjectile",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 6;
                    gameObject.AddComponent<CombatProjectileController>();
                },
                24,
                256);
        }

        internal bool CanResolve => _canFight?.Invoke() ?? true;

        public bool Fire(
            Vector3 origin,
            MonsterController target,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float areaRadius = 0f,
            int pierceCount = 1,
            float chainedDamageMultiplier = 1f,
            bool linePierce = false,
            Action<ProjectileImpactContext> onImpact = null)
        {
            if (target == null || target.IsResolved || _activeCount >= MaxActiveProjectiles)
                return false;
            var spawned = RuntimePoolService.Spawn(_template, origin, Quaternion.identity, _root);
            if (spawned == null) return false;
            _activeCount++;
            spawned.GetComponent<CombatProjectileController>().Launch(
                this,
                target,
                sprite,
                packet,
                speed,
                scale,
                areaRadius,
                pierceCount,
                chainedDamageMultiplier,
                linePierce,
                onImpact);
            return true;
        }

        public bool FireToPoint(
            Vector3 origin,
            Vector3 destination,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float hitRadius = 0.55f,
            bool hitAllInRadius = false,
            Action<Vector3> onImpact = null,
            Color? tint = null)
        {
            if (_activeCount >= MaxActiveProjectiles)
                return false;
            var spawned = RuntimePoolService.Spawn(_template, origin, Quaternion.identity, _root);
            if (spawned == null) return false;
            _activeCount++;
            spawned.GetComponent<CombatProjectileController>().LaunchToPoint(
                this,
                destination,
                sprite,
                packet,
                speed,
                scale,
                hitRadius,
                hitAllInRadius,
                onImpact,
                tint);
            return true;
        }

        internal void ResolveImpact(
            CombatProjectileController projectile,
            MonsterController primary,
            DamagePacket packet,
            float areaRadius,
            int pierceCount,
            float chainedDamageMultiplier,
            bool linePierce,
            Vector3 launchOrigin)
        {
            if (primary == null)
            {
                Release(projectile);
                return;
            }

            try
            {
                Vector3 impactPosition = primary.transform.position;
                float hpBefore = primary.CurrentHp;
                bool wasSlimeResonating = primary.HasSlimeResonance;
                Action<ProjectileImpactContext> onImpact = projectile.ImpactCallback;
                var monsters = _monsterProvider?.Invoke();
                if (areaRadius > 0f && monsters != null)
                {
                    float radiusSq = areaRadius * areaRadius;
                    var snapshot = new List<MonsterController>(monsters);
                    foreach (var monster in snapshot)
                    {
                        if (!IsValid(monster)) continue;
                        if ((monster.transform.position - primary.transform.position).sqrMagnitude <= radiusSq)
                            monster.ApplyDamage(packet);
                    }
                }
                else
                {
                    if (linePierce && pierceCount > 1 && monsters != null)
                        ResolveLinePierce(
                            launchOrigin,
                            primary,
                            packet,
                            pierceCount,
                            monsters,
                            chainedDamageMultiplier);
                    else
                    {
                        primary.ApplyDamage(packet);
                    }
                    if (!linePierce && pierceCount > 1 && monsters != null)
                        ResolvePierce(primary, packet, pierceCount - 1, monsters, chainedDamageMultiplier);
                }

                bool primaryDefeated = hpBefore > 0f &&
                                       (!primary.gameObject.activeInHierarchy || primary.CurrentHp <= 0f);
                onImpact?.Invoke(new ProjectileImpactContext(
                    impactPosition,
                    packet,
                    true,
                    primaryDefeated,
                    wasSlimeResonating));
            }
            finally
            {
                Release(projectile);
            }
        }

        internal void ResolveMissImpact(
            CombatProjectileController projectile,
            Vector3 impactPosition,
            DamagePacket packet)
        {
            if (projectile == null)
                return;
            Action<ProjectileImpactContext> onImpact = projectile.ImpactCallback;
            try
            {
                onImpact?.Invoke(new ProjectileImpactContext(
                    impactPosition,
                    packet,
                    false,
                    false,
                    false));
            }
            finally
            {
                projectile.BeginMissBurst();
            }
        }

        internal void Release(CombatProjectileController projectile)
        {
            if (projectile == null) return;
            projectile.ResetForPool();
            _activeCount = Mathf.Max(0, _activeCount - 1);
            RuntimePoolService.Despawn(projectile.transform);
        }

        internal void ResolvePointImpact(
            CombatProjectileController projectile,
            Vector3 point,
            DamagePacket packet,
            float radius,
            bool hitAllInRadius,
            Action<Vector3> onImpact)
        {
            var monsters = _monsterProvider?.Invoke();
            if (monsters != null)
            {
                float radiusSq = Mathf.Max(0.1f, radius) * Mathf.Max(0.1f, radius);
                MonsterController nearest = null;
                float nearestSq = radiusSq;
                var snapshot = new List<MonsterController>(monsters);
                foreach (var monster in snapshot)
                {
                    if (!IsValid(monster)) continue;
                    float distanceSq = (monster.transform.position - point).sqrMagnitude;
                    if (distanceSq > radiusSq) continue;
                    if (hitAllInRadius)
                    {
                        monster.ApplyDamage(packet);
                        continue;
                    }
                    if (distanceSq > nearestSq) continue;
                    nearestSq = distanceSq;
                    nearest = monster;
                }
                if (!hitAllInRadius)
                    nearest?.ApplyDamage(packet);
            }
            try
            {
                onImpact?.Invoke(point);
            }
            finally
            {
                Release(projectile);
            }
        }

        public void ApplyAreaDamage(
            Vector3 point,
            DamagePacket packet,
            float radius)
        {
            var monsters = _monsterProvider?.Invoke();
            if (monsters == null || radius <= 0f)
                return;
            float radiusSq = radius * radius;
            var snapshot = new List<MonsterController>(monsters);
            for (int i = 0; i < snapshot.Count; i++)
            {
                MonsterController monster = snapshot[i];
                if (!IsValid(monster) ||
                    (monster.transform.position - point).sqrMagnitude > radiusSq)
                    continue;
                monster.ApplyDamage(packet);
            }
        }

        static void ResolvePierce(
            MonsterController primary,
            DamagePacket packet,
            int remaining,
            IReadOnlyCollection<MonsterController> monsters,
            float chainedDamageMultiplier)
        {
            var candidates = new List<MonsterController>();
            foreach (var monster in monsters)
            {
                if (!IsValid(monster) || monster == primary) continue;
                if ((monster.transform.position - primary.transform.position).sqrMagnitude <= 3.5f * 3.5f)
                    candidates.Add(monster);
            }

            candidates.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - primary.transform.position)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - primary.transform.position)));

            float retainedDamage = Mathf.Clamp(chainedDamageMultiplier, 0.05f, 1f);
            for (int i = 0; i < Mathf.Min(remaining, candidates.Count); i++)
                candidates[i].ApplyDamage(packet.Scaled(Mathf.Pow(retainedDamage, i + 1)));
        }

        static void ResolveLinePierce(
            Vector3 origin,
            MonsterController primary,
            DamagePacket packet,
            int hitCount,
            IReadOnlyCollection<MonsterController> monsters,
            float retainedDamageMultiplier)
        {
            Vector3 toPrimary = primary.transform.position - origin;
            float primaryDistance = toPrimary.magnitude;
            if (primaryDistance <= Mathf.Epsilon)
            {
                primary.ApplyDamage(packet);
                return;
            }

            Vector3 direction = toPrimary / primaryDistance;
            float corridorRadiusSq = 0.45f * 0.45f;
            float maxProjection = primaryDistance + 0.6f;
            var candidates = new List<(MonsterController monster, float projection)>();
            foreach (var monster in monsters)
            {
                if (!IsValid(monster))
                    continue;
                Vector3 relative = monster.transform.position - origin;
                float projection = Vector3.Dot(relative, direction);
                if (projection < 0f || projection > maxProjection)
                    continue;
                float perpendicularSq = Mathf.Max(
                    0f,
                    relative.sqrMagnitude - projection * projection);
                if (perpendicularSq > corridorRadiusSq)
                    continue;
                candidates.Add((monster, projection));
            }

            candidates.Sort((first, second) => first.projection.CompareTo(second.projection));
            float retained = Mathf.Clamp(retainedDamageMultiplier, 0.05f, 1f);
            for (int i = 0; i < Mathf.Min(hitCount, candidates.Count); i++)
                candidates[i].monster.ApplyDamage(packet.Scaled(Mathf.Pow(retained, i)));
        }

        static bool IsValid(MonsterController monster) =>
            monster != null && monster.gameObject.activeInHierarchy && !monster.IsResolved && monster.CurrentHp > 0f;
    }

    [DisallowMultipleComponent]
    public sealed class CombatProjectileController : MonoBehaviour
    {
        const float HitDistance = 0.12f;
        const float MaxLifetime = 5f;
        const float MissBurstDuration = 0.12f;

        SpriteRenderer _renderer;
        CombatProjectileService _service;
        MonsterController _target;
        Vector3 _pointTarget;
        bool _usesPointTarget;
        DamagePacket _packet;
        float _speed;
        float _remainingLifetime;
        float _areaRadius;
        int _pierceCount;
        float _chainedDamageMultiplier;
        bool _hitAllInRadius;
        bool _linePierce;
        Action<Vector3> _pointImpact;
        Action<ProjectileImpactContext> _impactCallback;
        Vector3 _launchOrigin;
        Vector3 _lastKnownTargetPosition;
        int _targetSpawnVersion;
        bool _inFlight;
        bool _missBursting;
        float _missBurstRemaining;
        Vector3 _missBurstScale;
        Color _missBurstColor;

        internal Action<ProjectileImpactContext> ImpactCallback => _impactCallback;

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        public void Launch(
            CombatProjectileService service,
            MonsterController target,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float areaRadius,
            int pierceCount,
            float chainedDamageMultiplier,
            bool linePierce,
            Action<ProjectileImpactContext> onImpact)
        {
            _service = service;
            _target = target;
            _usesPointTarget = false;
            _packet = packet;
            _speed = Mathf.Max(0.1f, speed);
            _remainingLifetime = MaxLifetime;
            _areaRadius = Mathf.Max(0f, areaRadius);
            _pierceCount = Mathf.Max(1, pierceCount);
            _chainedDamageMultiplier = Mathf.Clamp(chainedDamageMultiplier, 0.05f, 1f);
            _hitAllInRadius = false;
            _linePierce = linePierce;
            _pointImpact = null;
            _impactCallback = onImpact;
            _launchOrigin = transform.position;
            _lastKnownTargetPosition = target.transform.position;
            _targetSpawnVersion = target.SpawnVersion;
            _inFlight = true;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            _renderer.sprite = sprite;
            _renderer.color = packet.Attribute switch
            {
                CrossDefense.Data.MonsterAttribute.Fire => new Color(1f, 0.45f, 0.2f),
                CrossDefense.Data.MonsterAttribute.Ice => new Color(0.4f, 0.8f, 1f),
                CrossDefense.Data.MonsterAttribute.Nature => new Color(0.45f, 1f, 0.5f),
                CrossDefense.Data.MonsterAttribute.Lightning => new Color(0.78f, 0.62f, 1f),
                CrossDefense.Data.MonsterAttribute.Water => new Color(0.25f, 0.68f, 1f),
                CrossDefense.Data.MonsterAttribute.Wind => new Color(0.7f, 1f, 0.88f),
                _ => Color.white,
            };
            FaceTarget();
        }

        public void LaunchToPoint(
            CombatProjectileService service,
            Vector3 point,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float hitRadius,
            bool hitAllInRadius,
            Action<Vector3> onImpact,
            Color? tint)
        {
            _service = service;
            _target = null;
            _pointTarget = point;
            _usesPointTarget = true;
            _packet = packet;
            _speed = Mathf.Max(0.1f, speed);
            _remainingLifetime = MaxLifetime;
            _areaRadius = Mathf.Max(0.1f, hitRadius);
            _pierceCount = 1;
            _chainedDamageMultiplier = 1f;
            _hitAllInRadius = hitAllInRadius;
            _linePierce = false;
            _pointImpact = onImpact;
            _impactCallback = null;
            _launchOrigin = transform.position;
            _inFlight = true;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            _renderer.sprite = sprite;
            _renderer.color = tint ?? AttributeColor(packet.Attribute);
            FaceDirection(_pointTarget - transform.position);
        }

        void Update()
        {
            if (_missBursting)
            {
                TickMissBurst();
                return;
            }
            if (!_inFlight) return;
            if (Time.timeScale <= 0f)
                return;
            if (_service == null || !_service.CanResolve)
            {
                _service?.Release(this);
                return;
            }
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f)
            {
                _service?.Release(this);
                return;
            }

            bool hasLiveTarget = _usesPointTarget || IsValidTarget();
            if (!_usesPointTarget)
            {
                if (hasLiveTarget)
                {
                    _lastKnownTargetPosition = _target.transform.position;
                }
                else
                {
                    _target = null;
                }
            }

            Vector3 destination = _usesPointTarget
                ? _pointTarget
                : hasLiveTarget
                    ? _target.transform.position
                    : _lastKnownTargetPosition;
            Vector3 offset = destination - transform.position;
            float travel = _speed * Time.deltaTime;
            if (offset.sqrMagnitude <= Mathf.Max(HitDistance * HitDistance, travel * travel))
            {
                _inFlight = false;
                if (_usesPointTarget)
                    _service.ResolvePointImpact(
                        this,
                        _pointTarget,
                        _packet,
                        _areaRadius,
                        _hitAllInRadius,
                        _pointImpact);
                else if (hasLiveTarget)
                    _service.ResolveImpact(
                        this,
                        _target,
                        _packet,
                        _areaRadius,
                        _pierceCount,
                        _chainedDamageMultiplier,
                        _linePierce,
                        _launchOrigin);
                else
                    _service.ResolveMissImpact(
                        this,
                        _lastKnownTargetPosition,
                        _packet);
                return;
            }

            transform.position += offset.normalized * travel;
            FaceDirection(offset);
        }

        internal void BeginMissBurst()
        {
            _inFlight = false;
            _missBursting = true;
            _missBurstRemaining = MissBurstDuration;
            _missBurstScale = transform.localScale;
            _missBurstColor = _renderer != null ? _renderer.color : Color.white;
        }

        void TickMissBurst()
        {
            if (Time.timeScale <= 0f)
                return;
            _missBurstRemaining -= Time.deltaTime;
            float progress = 1f - Mathf.Clamp01(
                _missBurstRemaining / MissBurstDuration);
            transform.localScale = _missBurstScale * Mathf.Lerp(1f, 1.8f, progress);
            if (_renderer != null)
            {
                Color color = _missBurstColor;
                color.a *= 1f - progress;
                _renderer.color = color;
            }
            if (_missBurstRemaining <= 0f)
                _service?.Release(this);
        }

        public void ResetForPool()
        {
            _inFlight = false;
            _missBursting = false;
            _missBurstRemaining = 0f;
            _missBurstScale = Vector3.one;
            _missBurstColor = Color.white;
            _service = null;
            _target = null;
            _usesPointTarget = false;
            _pointTarget = default;
            _packet = default;
            _speed = 0f;
            _remainingLifetime = 0f;
            _areaRadius = 0f;
            _pierceCount = 1;
            _chainedDamageMultiplier = 1f;
            _hitAllInRadius = false;
            _linePierce = false;
            _pointImpact = null;
            _impactCallback = null;
            _launchOrigin = Vector3.zero;
            _lastKnownTargetPosition = Vector3.zero;
            _targetSpawnVersion = 0;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
            if (_renderer != null)
            {
                _renderer.sprite = null;
                _renderer.color = Color.white;
            }
        }

        bool IsValidTarget() =>
            _target != null &&
            _target.SpawnVersion == _targetSpawnVersion &&
            _target.gameObject.activeInHierarchy &&
            !_target.IsResolved &&
            _target.CurrentHp > 0f;

        void FaceTarget()
        {
            if (_target != null)
                FaceDirection(_target.transform.position - transform.position);
        }

        void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude <= Mathf.Epsilon) return;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        static Color AttributeColor(CrossDefense.Data.MonsterAttribute attribute)
        {
            return attribute switch
            {
                CrossDefense.Data.MonsterAttribute.Fire => new Color(1f, 0.45f, 0.2f),
                CrossDefense.Data.MonsterAttribute.Ice => new Color(0.4f, 0.8f, 1f),
                CrossDefense.Data.MonsterAttribute.Nature => new Color(0.45f, 1f, 0.5f),
                CrossDefense.Data.MonsterAttribute.Lightning => new Color(0.78f, 0.62f, 1f),
                CrossDefense.Data.MonsterAttribute.Water => new Color(0.25f, 0.68f, 1f),
                CrossDefense.Data.MonsterAttribute.Wind => new Color(0.7f, 1f, 0.88f),
                _ => Color.white,
            };
        }
    }
}
