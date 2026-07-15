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
            int pierceCount = 1)
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
                pierceCount);
        }

        public void FireToPoint(
            Vector3 origin,
            Vector3 destination,
            Sprite sprite,
            DamagePacket packet,
            float speed,
            float scale,
            float hitRadius = 0.55f)
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
                hitRadius);
        }

        internal void ResolveImpact(
            CombatProjectileController projectile,
            MonsterController primary,
            DamagePacket packet,
            float areaRadius,
            int pierceCount)
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
                primary.ApplyDamage(packet);
                if (pierceCount > 1 && monsters != null)
                    ResolvePierce(primary, packet, pierceCount - 1, monsters);
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
            float radius)
        {
            var monsters = _monsterProvider?.Invoke();
            if (monsters != null)
            {
                MonsterController nearest = null;
                float nearestSq = Mathf.Max(0.1f, radius) * Mathf.Max(0.1f, radius);
                foreach (var monster in monsters)
                {
                    if (!IsValid(monster)) continue;
                    float distanceSq = (monster.transform.position - point).sqrMagnitude;
                    if (distanceSq > nearestSq) continue;
                    nearestSq = distanceSq;
                    nearest = monster;
                }
                nearest?.ApplyDamage(packet);
            }
            Release(projectile);
        }

        static void ResolvePierce(
            MonsterController primary,
            DamagePacket packet,
            int remaining,
            IReadOnlyCollection<MonsterController> monsters)
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

            for (int i = 0; i < Mathf.Min(remaining, candidates.Count); i++)
                candidates[i].ApplyDamage(packet);
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
            int pierceCount)
        {
            _service = service;
            _target = target;
            _usesPointTarget = false;
            _packet = packet;
            _speed = Mathf.Max(0.1f, speed);
            _remainingLifetime = MaxLifetime;
            _areaRadius = Mathf.Max(0f, areaRadius);
            _pierceCount = Mathf.Max(1, pierceCount);
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
            float hitRadius)
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
            _inFlight = true;
            transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);
            _renderer.sprite = sprite;
            _renderer.color = Color.white;
            FaceDirection(_pointTarget - transform.position);
        }

        void Update()
        {
            if (!_inFlight) return;
            if (_service == null || !_service.CanResolve)
            {
                _service?.Release(this);
                return;
            }
            _remainingLifetime -= Time.unscaledDeltaTime;
            if (_remainingLifetime <= 0f || (!_usesPointTarget && !IsValidTarget()))
            {
                _service?.Release(this);
                return;
            }

            Vector3 destination = _usesPointTarget ? _pointTarget : _target.transform.position;
            Vector3 offset = destination - transform.position;
            float travel = _speed * Time.unscaledDeltaTime;
            if (offset.sqrMagnitude <= Mathf.Max(HitDistance * HitDistance, travel * travel))
            {
                _inFlight = false;
                if (_usesPointTarget)
                    _service.ResolvePointImpact(this, _pointTarget, _packet, _areaRadius);
                else
                    _service.ResolveImpact(this, _target, _packet, _areaRadius, _pierceCount);
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
            if (_renderer != null)
                _renderer.sprite = null;
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
    }
}
