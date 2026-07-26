using CrossDefense.Data;
using CrossDefense.Units;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>원거리 몬스터가 슬라임 또는 소환사를 공격할 때 사용하는 풀링 투사체.</summary>
    public sealed class MonsterProjectileService
    {
        public const int MaxActiveProjectiles = 64;

        readonly GameManager _gameManager;
        readonly Transform _root;
        readonly Transform _template;
        int _activeCount;

        public MonsterProjectileService(GameManager gameManager, Transform parent)
        {
            _gameManager = gameManager;
            var rootObject = new GameObject("MonsterProjectiles");
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseMonsterProjectile",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 5;
                    gameObject.AddComponent<MonsterProjectileController>();
                },
                16,
                128);
        }

        internal bool CanResolve =>
            _gameManager != null &&
            !_gameManager.IsRunOver &&
            !_gameManager.IsGameplayPaused;

        public bool FireAtUnit(
            Vector3 origin,
            SummonedUnitController target,
            MonsterData data,
            int damage)
        {
            if (!IsValid(target) || !CanFire(data))
                return false;
            MonsterProjectileController projectile = Spawn(origin, data);
            return projectile != null && projectile.LaunchAtUnit(
                this,
                target,
                Mathf.Max(0, damage),
                data.Attribute,
                data.ProjectileSpeed);
        }

        public bool FireAtCore(
            Vector3 origin,
            Transform target,
            MonsterData data,
            int damage)
        {
            if (target == null || !CanFire(data))
                return false;
            MonsterProjectileController projectile = Spawn(origin, data);
            return projectile != null && projectile.LaunchAtCore(
                this,
                _gameManager,
                target,
                Mathf.Max(0, damage),
                data.Attribute,
                data.ProjectileSpeed);
        }

        MonsterProjectileController Spawn(Vector3 origin, MonsterData data)
        {
            Transform spawned = RuntimePoolService.Spawn(
                _template,
                origin,
                Quaternion.identity,
                _root);
            if (spawned == null)
                return null;
            _activeCount++;
            var projectile = spawned.GetComponent<MonsterProjectileController>();
            projectile.ApplyVisual(
                data.ProjectileSprite,
                data.ProjectileScale,
                AttributeTint(data.Attribute));
            return projectile;
        }

        bool CanFire(MonsterData data) =>
            data != null &&
            data.AttackStyle == MonsterAttackStyle.Projectile &&
            data.ProjectileSprite != null &&
            _activeCount < MaxActiveProjectiles;

        internal void Release(MonsterProjectileController projectile)
        {
            if (projectile == null)
                return;
            projectile.ResetForPool();
            _activeCount = Mathf.Max(0, _activeCount - 1);
            RuntimePoolService.Despawn(projectile.transform);
        }

        static bool IsValid(SummonedUnitController unit) =>
            unit != null &&
            unit.gameObject.activeInHierarchy &&
            !unit.IsDefeated &&
            unit.CurrentHp > 0f;

        static Color AttributeTint(MonsterAttribute attribute) => attribute switch
        {
            MonsterAttribute.Fire => new Color(1f, 0.55f, 0.3f),
            MonsterAttribute.Ice => new Color(0.55f, 0.85f, 1f),
            MonsterAttribute.Nature => new Color(0.55f, 1f, 0.55f),
            MonsterAttribute.Lightning => new Color(0.75f, 0.65f, 1f),
            MonsterAttribute.Water => new Color(0.3f, 0.72f, 1f),
            MonsterAttribute.Wind => new Color(0.72f, 1f, 0.88f),
            _ => new Color(0.82f, 0.72f, 0.55f),
        };
    }

    public sealed class MonsterProjectileController : MonoBehaviour
    {
        const float HitDistance = 0.14f;
        const float MaxLifetime = 6f;

        MonsterProjectileService _service;
        GameManager _gameManager;
        SummonedUnitController _unitTarget;
        Transform _coreTarget;
        SpriteRenderer _renderer;
        float _speed;
        float _expiresAt;
        int _damage;
        MonsterAttribute _attribute;

        public void ApplyVisual(Sprite sprite, float scale, Color tint)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = sprite;
            _renderer.color = tint;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        }

        public bool LaunchAtUnit(
            MonsterProjectileService service,
            SummonedUnitController target,
            int damage,
            MonsterAttribute attribute,
            float speed)
        {
            if (service == null || target == null)
                return false;
            _service = service;
            _unitTarget = target;
            _coreTarget = null;
            _gameManager = null;
            _attribute = attribute;
            Begin(damage, speed);
            return true;
        }

        public bool LaunchAtCore(
            MonsterProjectileService service,
            GameManager gameManager,
            Transform target,
            int damage,
            MonsterAttribute attribute,
            float speed)
        {
            if (service == null || gameManager == null || target == null)
                return false;
            _service = service;
            _unitTarget = null;
            _coreTarget = target;
            _gameManager = gameManager;
            _attribute = attribute;
            Begin(damage, speed);
            return true;
        }

        void Begin(int damage, float speed)
        {
            _damage = Mathf.Max(0, damage);
            _speed = Mathf.Max(0.1f, speed);
            _expiresAt = Time.time + MaxLifetime;
        }

        void Update()
        {
            if (_service == null)
                return;
            if (!_service.CanResolve)
                return;
            if (Time.time >= _expiresAt)
            {
                _service.Release(this);
                return;
            }

            Transform target = _unitTarget != null ? _unitTarget.transform : _coreTarget;
            if (target == null || (_unitTarget != null &&
                (_unitTarget.IsDefeated || _unitTarget.CurrentHp <= 0f ||
                 !_unitTarget.gameObject.activeInHierarchy)))
            {
                _service.Release(this);
                return;
            }

            Vector3 offset = target.position - transform.position;
            float step = _speed * Time.deltaTime;
            if (offset.sqrMagnitude <= Mathf.Max(HitDistance, step) * Mathf.Max(HitDistance, step))
            {
                ResolveImpact();
                return;
            }

            Vector3 direction = offset.normalized;
            transform.position += direction * step;
            transform.rotation = Quaternion.Euler(
                0f,
                0f,
                Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        void ResolveImpact()
        {
            if (_unitTarget != null)
                _unitTarget.ApplyDamage(new DamagePacket(
                    this,
                    _damage,
                    _attribute));
            else if (_gameManager != null && !_gameManager.IsRunOver)
                _gameManager.ApplyCoreDamage(_damage);
            _service.Release(this);
        }

        public void ResetForPool()
        {
            _service = null;
            _gameManager = null;
            _unitTarget = null;
            _coreTarget = null;
            _speed = 0f;
            _expiresAt = 0f;
            _damage = 0;
            _attribute = MonsterAttribute.None;
            if (_renderer != null)
            {
                _renderer.sprite = null;
                _renderer.color = Color.white;
            }
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}
