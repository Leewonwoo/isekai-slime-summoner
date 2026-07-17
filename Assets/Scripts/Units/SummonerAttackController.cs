using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    public enum SummonerProjectileType
    {
        EnergyBolt,
        Fireball,
        Iceball,
    }

    /// <summary>가장 가까운 몬스터를 자동 조준해 선택한 속성 투사체를 발사한다.</summary>
    [DisallowMultipleComponent]
    public sealed class SummonerAttackController : MonoBehaviour
    {
        [Header("Projectile Choice")]
        [SerializeField] SummonerProjectileType selectedProjectile = SummonerProjectileType.EnergyBolt;
        [SerializeField] Sprite energyBoltSprite;
        [SerializeField] Sprite fireballSprite;
        [SerializeField] Sprite iceballSprite;

        [Header("Balance")]
        [Min(0.1f)] [SerializeField] float attackDamage = 12f;
        [Min(0.1f)] [SerializeField] float attacksPerSecond = 1.25f;
        [Min(0.1f)] [SerializeField] float attackRange = 4.5f;
        [Min(0.1f)] [SerializeField] float projectileSpeed = 10f;

        [Header("Click Attack")]
        [Min(0.1f)] [SerializeField] float clickAttackDamage = 18f;
        [Min(0.1f)] [SerializeField] float clickAttacksPerSecond = 2f;
        [Min(0.1f)] [SerializeField] float clickHitRadius = 0.65f;

        [Header("Presentation")]
        [SerializeField] Transform firePosition;
        [Min(0f)] [SerializeField] float spawnOffset = 0.4f;
        [Min(0.01f)] [SerializeField] float projectileScale = 0.65f;

        [Header("Animation")]
        [SerializeField] Sprite[] idleFrames;
        [Min(1f)] [SerializeField] float idleAnimationFps = 8f;
        [SerializeField] Sprite[] attackFrames;
        [Min(1f)] [SerializeField] float attackAnimationFps = 18f;

        readonly HashSet<MonsterController> _targets = new();
        GameManager _gameManager;
        SpriteRenderer _renderer;
        WorldHealthBar _healthBar;
        float _nextAttackTime;
        float _nextClickAttackTime;
        float _idleAnimationElapsed;
        float _attackAnimationElapsed;
        bool _isAttackAnimating;

        public SummonerProjectileType SelectedProjectile => selectedProjectile;
        public float AttackDamage => attackDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackRange => attackRange;
        public Transform FirePosition => firePosition;
        public int IdleFrameCount => idleFrames?.Length ?? 0;
        public int AttackFrameCount => attackFrames?.Length ?? 0;
        public bool IsAttackAnimating => _isAttackAnimating;

        public event Action<SummonerProjectileType> ProjectileTypeChanged;

        void Awake()
        {
            _gameManager = GetComponentInParent<GameManager>();
            if (_gameManager == null)
                _gameManager = FindFirstObjectByType<GameManager>();
            _renderer = GetComponent<SpriteRenderer>();
            _healthBar = GetComponent<WorldHealthBar>();

            if (firePosition == null)
                firePosition = transform.Find("FirePosition") ?? FindDirectChildIgnoreCase("FirePosition");

            if (firePosition == null)
                Debug.LogError("[CrossDefense] Summoner/FirePosition reference is missing. Projectile origin will use the summoner transform.", this);

        }

        void OnEnable()
        {
            _idleAnimationElapsed = 0f;
            _attackAnimationElapsed = 0f;
            _isAttackAnimating = false;
            ApplyAnimationFrame(GetFrame(idleFrames, 0));

            if (_gameManager == null)
                _gameManager = GetComponentInParent<GameManager>();
            if (_gameManager == null) return;

            _gameManager.MonsterSpawned += OnMonsterSpawned;
            _gameManager.MonsterResolved += OnMonsterResolved;
        }

        void OnDisable()
        {
            if (_gameManager == null) return;
            _gameManager.MonsterSpawned -= OnMonsterSpawned;
            _gameManager.MonsterResolved -= OnMonsterResolved;
            _targets.Clear();
        }

        void Update()
        {
            TickAnimation(Time.unscaledDeltaTime);

            if (_gameManager == null || _gameManager.IsRunOver || _gameManager.Phase != RunPhase.InWave)
                return;
            if (Time.unscaledTime < _nextAttackTime)
                return;

            var target = FindNearestTarget();
            if (target == null)
                return;

            FireAt(target);
            _nextAttackTime = Time.unscaledTime + 1f /
                Mathf.Max(0.1f, attacksPerSecond * _gameManager.SummonerAttackSpeedMultiplier);
        }

        public void SetProjectileType(SummonerProjectileType projectileType)
        {
            if (selectedProjectile == projectileType) return;
            selectedProjectile = projectileType;
            ProjectileTypeChanged?.Invoke(projectileType);
        }

        public bool TryClickAttack(Vector3 worldPoint, MonsterController preferredTarget = null)
        {
            if (_gameManager == null || _gameManager.IsRunOver || _gameManager.Phase != RunPhase.InWave ||
                _gameManager.Projectiles == null || Time.unscaledTime < _nextClickAttackTime)
                return false;

            Sprite sprite = GetProjectileSprite(selectedProjectile);
            if (sprite == null) return false;
            Vector3 origin = firePosition != null ? firePosition.position : transform.position;
            var packet = new DamagePacket(
                this,
                _gameManager.ModifySummonerDamage(clickAttackDamage),
                GetAttackAttribute(selectedProjectile));

            bool preferredTargetInRange = preferredTarget != null &&
                !preferredTarget.IsResolved &&
                (preferredTarget.transform.position - origin).sqrMagnitude <= attackRange * attackRange;
            if (preferredTargetInRange)
            {
                _gameManager.Projectiles.Fire(
                    origin,
                    preferredTarget,
                    sprite,
                    packet,
                    projectileSpeed,
                    projectileScale);
            }
            else
            {
                Vector3 direction = worldPoint - origin;
                if (direction.sqrMagnitude <= 0.01f) return false;
                Vector3 destination = origin + direction.normalized * attackRange;
                _gameManager.Projectiles.FireToPoint(
                    origin,
                    destination,
                    sprite,
                    packet,
                    projectileSpeed,
                    projectileScale,
                    clickHitRadius);
            }

            PlayAttackAnimation();
            _nextClickAttackTime = Time.unscaledTime + 1f /
                Mathf.Max(0.1f, clickAttacksPerSecond * _gameManager.SummonerAttackSpeedMultiplier);
            return true;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, attackRange));
        }

        void OnMonsterSpawned(MonsterController monster, StageWave _, int __)
        {
            if (monster != null)
                _targets.Add(monster);
        }

        void OnMonsterResolved(MonsterController monster)
        {
            if (monster != null)
                _targets.Remove(monster);
        }

        MonsterController FindNearestTarget()
        {
            MonsterController nearest = null;
            float nearestDistanceSq = attackRange * attackRange;
            _targets.RemoveWhere(IsInvalidTarget);

            foreach (var candidate in _targets)
            {
                float distanceSq = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distanceSq > nearestDistanceSq) continue;
                nearest = candidate;
                nearestDistanceSq = distanceSq;
            }

            return nearest;
        }

        static bool IsInvalidTarget(MonsterController monster)
        {
            return monster == null || !monster.gameObject.activeInHierarchy || monster.CurrentHp <= 0f;
        }

        bool FireAt(MonsterController target)
        {
            Sprite sprite = GetProjectileSprite(selectedProjectile);
            if (sprite == null)
            {
                Debug.LogWarning($"[CrossDefense] {selectedProjectile} 투사체 스프라이트가 비어 있습니다.", this);
                return false;
            }

            Vector3 origin = firePosition != null ? firePosition.position : transform.position;
            Vector3 direction = (target.transform.position - origin).normalized;
            if (firePosition == null)
                origin += direction * spawnOffset;
            if (_gameManager.Projectiles == null) return false;
            _gameManager.Projectiles.Fire(
                origin,
                target,
                sprite,
                new DamagePacket(
                    this,
                    _gameManager.ModifySummonerDamage(attackDamage),
                    GetAttackAttribute(selectedProjectile)),
                projectileSpeed,
                projectileScale);
            PlayAttackAnimation();
            return true;
        }

        void PlayAttackAnimation()
        {
            if (attackFrames == null || attackFrames.Length == 0) return;
            _isAttackAnimating = true;
            _attackAnimationElapsed = 0f;
            ApplyAnimationFrame(GetFrame(attackFrames, 0));
        }

        void TickAnimation(float deltaTime)
        {
            if (_renderer == null) return;

            if (_isAttackAnimating && attackFrames != null && attackFrames.Length > 0)
            {
                _attackAnimationElapsed += Mathf.Max(0f, deltaTime);
                int attackFrameIndex = Mathf.FloorToInt(
                    _attackAnimationElapsed * Mathf.Max(1f, attackAnimationFps));
                if (attackFrameIndex < attackFrames.Length)
                {
                    ApplyAnimationFrame(GetFrame(attackFrames, attackFrameIndex));
                    return;
                }

                _isAttackAnimating = false;
                _attackAnimationElapsed = 0f;
                _idleAnimationElapsed = 0f;
            }

            if (idleFrames == null || idleFrames.Length == 0) return;
            _idleAnimationElapsed += Mathf.Max(0f, deltaTime);
            int idleFrameIndex = Mathf.FloorToInt(
                _idleAnimationElapsed * Mathf.Max(1f, idleAnimationFps)) % idleFrames.Length;
            ApplyAnimationFrame(GetFrame(idleFrames, idleFrameIndex));
        }

        void ApplyAnimationFrame(Sprite frame)
        {
            if (frame == null || _renderer == null || _renderer.sprite == frame) return;
            _renderer.sprite = frame;
            if (_healthBar == null)
                _healthBar = GetComponent<WorldHealthBar>();
            _healthBar?.RefreshLayout();
        }

        static Sprite GetFrame(Sprite[] frames, int index)
        {
            if (frames == null || frames.Length == 0) return null;
            return frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        Transform FindDirectChildIgnoreCase(string childName)
        {
            foreach (Transform child in transform)
            {
                if (string.Equals(child.name, childName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        Sprite GetProjectileSprite(SummonerProjectileType projectileType)
        {
            return projectileType switch
            {
                SummonerProjectileType.Fireball => fireballSprite,
                SummonerProjectileType.Iceball => iceballSprite,
                _ => energyBoltSprite,
            };
        }

        static MonsterAttribute GetAttackAttribute(SummonerProjectileType projectileType)
        {
            return projectileType switch
            {
                SummonerProjectileType.Fireball => MonsterAttribute.Fire,
                SummonerProjectileType.Iceball => MonsterAttribute.Ice,
                _ => MonsterAttribute.None,
            };
        }
    }

}
