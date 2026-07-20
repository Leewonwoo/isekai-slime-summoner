using System;
using System.Collections;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>런 보상으로 각성한 주 공격을 자동 조준·클릭 방향 공격에 함께 적용한다.</summary>
    [DisallowMultipleComponent]
    public sealed class SummonerAttackController : MonoBehaviour
    {
        [Header("Projectile Visuals")]
        [SerializeField] Sprite energyBoltSprite;
        [SerializeField] Sprite fireballSprite;
        [SerializeField] Sprite iceballSprite;
        [SerializeField] Sprite lightningOrbSprite;

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
        [Min(0.01f)] [SerializeField] float volleyShotDelay = 0.09f;

        [Header("Animation")]
        [SerializeField] Sprite[] idleFrames;
        [Min(1f)] [SerializeField] float idleAnimationFps = 8f;
        [SerializeField] Sprite[] attackFrames;
        [Min(1f)] [SerializeField] float attackAnimationFps = 18f;

        readonly HashSet<MonsterController> _targets = new();
        readonly List<MonsterController> _volleyTargets = new(4);
        readonly List<MonsterController> _volleyCandidates = new(32);
        GameManager _gameManager;
        SpriteRenderer _renderer;
        WorldHealthBar _healthBar;
        float _nextAttackTime;
        float _nextClickAttackTime;
        float _idleAnimationElapsed;
        float _attackAnimationElapsed;
        int _attackSequence;
        bool _isAttackAnimating;

        public float AttackDamage => attackDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackRange => attackRange;
        public Transform FirePosition => firePosition;
        public int IdleFrameCount => idleFrames?.Length ?? 0;
        public int AttackFrameCount => attackFrames?.Length ?? 0;
        public bool IsAttackAnimating => _isAttackAnimating;

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
            _attackSequence = 0;
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
            if (_gameManager == null || _gameManager.IsGameplayPaused)
                return;

            TickAnimation(Time.deltaTime);

            if (_gameManager.IsRunOver || _gameManager.Phase != RunPhase.InWave)
                return;
            if (Time.time < _nextAttackTime)
                return;

            var target = FindNearestTarget();
            if (target == null)
                return;

            FireAt(target, GetAttackProfile());
            _nextAttackTime = Time.time + 1f /
                Mathf.Max(0.1f, attacksPerSecond * _gameManager.SummonerAttackSpeedMultiplier);
        }

        public bool TryClickAttack(Vector3 worldPoint, MonsterController preferredTarget = null)
        {
            if (_gameManager == null || _gameManager.IsRunOver || _gameManager.IsGameplayPaused ||
                _gameManager.Phase != RunPhase.InWave ||
                _gameManager.Projectiles == null || Time.time < _nextClickAttackTime)
                return false;

            SummonerRunAttackProfile profile = GetAttackProfile();
            Sprite sprite = GetProjectileSprite(profile.Archetype);
            if (sprite == null) return false;
            Vector3 origin = firePosition != null ? firePosition.position : transform.position;
            bool empowered = IsEmpoweredAttack(profile);

            bool preferredTargetInRange = preferredTarget != null &&
                !preferredTarget.IsResolved &&
                (preferredTarget.transform.position - origin).sqrMagnitude <= attackRange * attackRange;
            if (preferredTargetInRange)
            {
                FireVolley(
                    origin,
                    preferredTarget,
                    sprite,
                    profile,
                    clickAttackDamage,
                    empowered);
            }
            else
            {
                Vector3 direction = worldPoint - origin;
                if (direction.sqrMagnitude <= 0.01f) return false;
                float radius = profile.Archetype switch
                {
                    SummonerAttackArchetype.Fireball => Mathf.Max(clickHitRadius, profile.AreaRadius),
                    SummonerAttackArchetype.ThunderSlash => Mathf.Max(clickHitRadius, 0.9f),
                    _ => clickHitRadius,
                };
                if (empowered)
                    radius = Mathf.Max(radius, profile.EmpoweredAreaRadius);

                StartCoroutine(FirePointVolleyRoutine(
                    origin,
                    direction.normalized,
                    sprite,
                    profile,
                    clickAttackDamage,
                    empowered,
                    radius));
            }

            _nextClickAttackTime = Time.time + 1f /
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

        bool FireAt(MonsterController target, SummonerRunAttackProfile profile)
        {
            Sprite sprite = GetProjectileSprite(profile.Archetype);
            if (sprite == null)
            {
                Debug.LogWarning($"[CrossDefense] {profile.Archetype} 투사체 스프라이트가 비어 있습니다.", this);
                return false;
            }

            Vector3 origin = firePosition != null ? firePosition.position : transform.position;
            Vector3 direction = (target.transform.position - origin).normalized;
            if (firePosition == null)
                origin += direction * spawnOffset;
            if (_gameManager.Projectiles == null) return false;
            FireVolley(origin, target, sprite, profile, attackDamage, IsEmpoweredAttack(profile));
            return true;
        }

        void FireVolley(
            Vector3 origin,
            MonsterController primaryTarget,
            Sprite sprite,
            SummonerRunAttackProfile profile,
            float baseDamage,
            bool empowered)
        {
            BuildVolleyTargets(primaryTarget, profile.ProjectileCount);
            var volleyTargets = new List<MonsterController>(_volleyTargets);
            float areaRadius = empowered
                ? Mathf.Max(profile.AreaRadius, profile.EmpoweredAreaRadius)
                : profile.AreaRadius;
            StartCoroutine(FireTargetVolleyRoutine(
                origin,
                volleyTargets,
                sprite,
                profile,
                baseDamage,
                empowered,
                areaRadius));
        }

        IEnumerator FireTargetVolleyRoutine(
            Vector3 origin,
            IReadOnlyList<MonsterController> volleyTargets,
            Sprite sprite,
            SummonerRunAttackProfile profile,
            float baseDamage,
            bool empowered,
            float areaRadius)
        {
            for (int i = 0; i < volleyTargets.Count; i++)
            {
                if (!CanContinueVolley())
                    yield break;
                MonsterController target = volleyTargets[i];
                if (IsInvalidTarget(target))
                    target = FindNearestTarget();
                if (target == null)
                    yield break;

                float damageScale = (i == 0 ? 1f : profile.AdditionalProjectileDamageMultiplier) *
                                    (empowered ? profile.EmpoweredDamageMultiplier : 1f);
                _gameManager.Projectiles.Fire(
                    origin,
                    target,
                    sprite,
                    BuildDamagePacket(baseDamage * damageScale, profile),
                    projectileSpeed,
                    ProjectileScale(profile),
                    areaRadius,
                    profile.PierceCount,
                    profile.ChainDamageMultiplier);
                PlayAttackAnimation();
                if (i + 1 < volleyTargets.Count)
                    yield return new WaitForSeconds(Mathf.Max(0.01f, volleyShotDelay));
            }
        }

        IEnumerator FirePointVolleyRoutine(
            Vector3 origin,
            Vector3 centerDirection,
            Sprite sprite,
            SummonerRunAttackProfile profile,
            float baseDamage,
            bool empowered,
            float hitRadius)
        {
            for (int i = 0; i < profile.ProjectileCount; i++)
            {
                if (!CanContinueVolley())
                    yield break;
                float angle = i == 0
                    ? 0f
                    : (i % 2 == 0 ? -1f : 1f) * (4f + 3f * ((i - 1) / 2));
                Vector3 destination = origin + Rotate(centerDirection, angle) * attackRange;
                float damageScale = (i == 0 ? 1f : profile.AdditionalProjectileDamageMultiplier) *
                                    (empowered ? profile.EmpoweredDamageMultiplier : 1f);
                _gameManager.Projectiles.FireToPoint(
                    origin,
                    destination,
                    sprite,
                    BuildDamagePacket(baseDamage * damageScale, profile),
                    projectileSpeed,
                    ProjectileScale(profile),
                    hitRadius,
                    profile.Archetype is SummonerAttackArchetype.Fireball or
                        SummonerAttackArchetype.ThunderSlash);
                PlayAttackAnimation();
                if (i + 1 < profile.ProjectileCount)
                    yield return new WaitForSeconds(Mathf.Max(0.01f, volleyShotDelay));
            }
        }

        bool CanContinueVolley() =>
            _gameManager != null &&
            !_gameManager.IsRunOver &&
            _gameManager.Phase == RunPhase.InWave &&
            _gameManager.Projectiles != null;

        DamagePacket BuildDamagePacket(float baseDamage, SummonerRunAttackProfile profile)
        {
            return new DamagePacket(
                this,
                _gameManager.ModifySummonerDamage(baseDamage),
                profile.Attribute,
                profile.SlowPercent,
                profile.SlowDuration,
                profile.DamageOverTime,
                profile.DamageOverTimeDuration);
        }

        void BuildVolleyTargets(MonsterController primary, int count)
        {
            _volleyTargets.Clear();
            if (primary == null || count <= 0)
                return;
            _volleyTargets.Add(primary);
            if (count == 1)
                return;

            _volleyCandidates.Clear();
            foreach (MonsterController candidate in _targets)
            {
                if (IsInvalidTarget(candidate) || candidate == primary)
                    continue;
                if ((candidate.transform.position - transform.position).sqrMagnitude > attackRange * attackRange)
                    continue;
                _volleyCandidates.Add(candidate);
            }
            _volleyCandidates.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - primary.transform.position)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - primary.transform.position)));
            for (int i = 0; i < _volleyCandidates.Count && _volleyTargets.Count < count; i++)
                _volleyTargets.Add(_volleyCandidates[i]);
            while (_volleyTargets.Count < count)
                _volleyTargets.Add(primary);
        }

        bool IsEmpoweredAttack(SummonerRunAttackProfile profile)
        {
            _attackSequence++;
            return profile.EmpoweredShotInterval > 0 &&
                   _attackSequence % profile.EmpoweredShotInterval == 0;
        }

        SummonerRunAttackProfile GetAttackProfile()
        {
            if (_gameManager?.RunTraits != null)
                return _gameManager.RunTraits.BuildAttackProfile();
            return new SummonerRunAttackProfile(
                SummonerAttackArchetype.EnergyBolt,
                MonsterAttribute.None,
                1,
                0.65f,
                0f,
                1,
                1f,
                0f,
                0f,
                0f,
                0f,
                0,
                0f,
                1f);
        }

        float ProjectileScale(SummonerRunAttackProfile profile)
        {
            float multiplier = profile.Archetype switch
            {
                SummonerAttackArchetype.Fireball => 1.2f,
                SummonerAttackArchetype.IceLance => 0.95f,
                SummonerAttackArchetype.ThunderSlash => 1.08f,
                _ => 1f,
            };
            return Mathf.Max(0.01f, projectileScale * multiplier);
        }

        static Vector3 Rotate(Vector3 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector3(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos,
                0f);
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

        Sprite GetProjectileSprite(SummonerAttackArchetype archetype)
        {
            return archetype switch
            {
                SummonerAttackArchetype.Fireball => fireballSprite != null ? fireballSprite : energyBoltSprite,
                SummonerAttackArchetype.IceLance => iceballSprite != null ? iceballSprite : energyBoltSprite,
                SummonerAttackArchetype.ThunderSlash =>
                    lightningOrbSprite != null ? lightningOrbSprite : energyBoltSprite,
                _ => energyBoltSprite,
            };
        }

    }

}
