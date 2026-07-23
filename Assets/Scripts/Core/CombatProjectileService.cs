using System;
using System.Collections.Generic;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>소환사와 소환수가 공유하는 PoolBoss 기반 투사체 서비스.</summary>
    public sealed class CombatProjectileService
    {
        readonly Transform _root;
        readonly Func<IReadOnlyCollection<MonsterController>> _monsterProvider;
        readonly Func<bool> _canFight;
        readonly Transform _template;

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

        public void Fire(
            Vector3 origin,
            MonsterController target,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float areaRadius = 0f,
            int pierceCount = 1,
            float chainedDamageMultiplier = 1f,
            bool linePierce = false)
        {
            if (target == null || target.IsResolved) return;
            var spawned = RuntimePoolService.Spawn(_template, origin, Quaternion.identity, _root);
            if (spawned == null) return;
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
                linePierce);
        }

        public void FireToPoint(
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
            var spawned = RuntimePoolService.Spawn(_template, origin, Quaternion.identity, _root);
            if (spawned == null) return;
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

            Release(projectile);
        }

        internal void Release(CombatProjectileController projectile)
        {
            if (projectile == null) return;
            projectile.ResetForPool();
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
        Vector3 _launchOrigin;
        bool _inFlight;

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
            bool linePierce)
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
            _launchOrigin = transform.position;
            _inFlight = true;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            _renderer.sprite = sprite;
            _renderer.color = packet.Attribute switch
            {
                CrossDefense.Data.MonsterAttribute.Fire => new Color(1f, 0.45f, 0.2f),
                CrossDefense.Data.MonsterAttribute.Ice => new Color(0.4f, 0.8f, 1f),
                CrossDefense.Data.MonsterAttribute.Nature => new Color(0.45f, 1f, 0.5f),
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
            _launchOrigin = transform.position;
            _inFlight = true;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            _renderer.sprite = sprite;
            _renderer.color = tint ?? AttributeColor(packet.Attribute);
            FaceDirection(_pointTarget - transform.position);
        }

        void Update()
        {
            if (!_inFlight) return;
            if (Time.timeScale <= 0f)
                return;
            if (_service == null || !_service.CanResolve)
            {
                _service?.Release(this);
                return;
            }
            _remainingLifetime -= Time.deltaTime;
            if (_remainingLifetime <= 0f || (!_usesPointTarget && !IsValidTarget()))
            {
                _service?.Release(this);
                return;
            }

            Vector3 destination = _usesPointTarget ? _pointTarget : _target.transform.position;
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
                else
                    _service.ResolveImpact(
                        this,
                        _target,
                        _packet,
                        _areaRadius,
                        _pierceCount,
                        _chainedDamageMultiplier,
                        _linePierce,
                        _launchOrigin);
                return;
            }

            transform.position += offset.normalized * travel;
            FaceDirection(offset);
        }

        public void ResetForPool()
        {
            _inFlight = false;
            _service = null;
            _target = null;
            _usesPointTarget = false;
            _pointTarget = default;
            _remainingLifetime = 0f;
            _areaRadius = 0f;
            _pierceCount = 1;
            _chainedDamageMultiplier = 1f;
            _hitAllInRadius = false;
            _linePierce = false;
            _pointImpact = null;
            _launchOrigin = Vector3.zero;
            if (_renderer != null)
            {
                _renderer.sprite = null;
                _renderer.color = Color.white;
            }
        }

        bool IsValidTarget() =>
            _target != null && _target.gameObject.activeInHierarchy && !_target.IsResolved && _target.CurrentHp > 0f;

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
                _ => Color.white,
            };
        }
    }
}
