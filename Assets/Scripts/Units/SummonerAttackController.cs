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
        [Min(0.1f)] [SerializeField] float attackRange = 10f;
        [Min(0.1f)] [SerializeField] float projectileSpeed = 10f;

        [Header("Click Attack")]
        [Min(0.1f)] [SerializeField] float clickAttackDamage = 18f;
        [Min(0.1f)] [SerializeField] float clickAttacksPerSecond = 2f;
        [Min(0.1f)] [SerializeField] float clickHitRadius = 0.65f;

        [Header("Presentation")]
        [SerializeField] Transform firePosition;
        [Min(0f)] [SerializeField] float spawnOffset = 0.4f;
        [Min(0.01f)] [SerializeField] float projectileScale = 0.65f;

        readonly HashSet<MonsterController> _targets = new();
        GameManager _gameManager;
        float _nextAttackTime;
        float _nextClickAttackTime;

        public SummonerProjectileType SelectedProjectile => selectedProjectile;
        public float AttackDamage => attackDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackRange => attackRange;
        public Transform FirePosition => firePosition;

        public event Action<SummonerProjectileType> ProjectileTypeChanged;

        void Awake()
        {
            _gameManager = GetComponentInParent<GameManager>();
            if (_gameManager == null)
                _gameManager = FindFirstObjectByType<GameManager>();

            if (firePosition == null)
                firePosition = transform.Find("FirePosition") ?? FindDirectChildIgnoreCase("FirePosition");

            if (firePosition == null)
                Debug.LogError("[CrossDefense] Summoner/FirePosition reference is missing. Projectile origin will use the summoner transform.", this);

        }

        void OnEnable()
        {
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
            if (_gameManager == null || _gameManager.IsRunOver || _gameManager.Phase != RunPhase.InWave)
                return;
            if (Time.unscaledTime < _nextAttackTime)
                return;

            var target = FindNearestTarget();
            if (target == null)
                return;

            FireAt(target);
            _nextAttackTime = Time.unscaledTime + 1f / Mathf.Max(0.1f, attacksPerSecond);
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
                clickAttackDamage,
                GetAttackAttribute(selectedProjectile));

            if (preferredTarget != null && !preferredTarget.IsResolved)
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

            _nextClickAttackTime = Time.unscaledTime + 1f / Mathf.Max(0.1f, clickAttacksPerSecond);
            return true;
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

        void FireAt(MonsterController target)
        {
            Sprite sprite = GetProjectileSprite(selectedProjectile);
            if (sprite == null)
            {
                Debug.LogWarning($"[CrossDefense] {selectedProjectile} 투사체 스프라이트가 비어 있습니다.", this);
                return;
            }

            Vector3 origin = firePosition != null ? firePosition.position : transform.position;
            Vector3 direction = (target.transform.position - origin).normalized;
            if (firePosition == null)
                origin += direction * spawnOffset;
            if (_gameManager.Projectiles == null) return;
            _gameManager.Projectiles.Fire(
                origin,
                target,
                sprite,
                new DamagePacket(this, attackDamage, GetAttackAttribute(selectedProjectile)),
                projectileSpeed,
                projectileScale);
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
