using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>외곽에서 소환사를 향해 이동하는 몬스터의 최소 생명주기.</summary>
    public sealed class MonsterController : MonoBehaviour
    {
        const float TargetSearchInterval = 0.2f;

        MonsterData _data;
        GameManager _gameManager;
        Transform _coreTarget;
        SummonedUnitController _unitTarget;
        float _hp;
        float _speed;
        float _attackRange;
        float _attacksPerSecond;
        float _nextAttackTime;
        float _nextTargetSearchTime;
        int _contactDamage;
        int _rewardGold;
        bool _resolved;
        float _slowMultiplier = 1f;
        float _slowUntil;
        float _dotDamagePerSecond;
        float _dotUntil;
        float _nextDotTick;
        SpriteRenderer _renderer;
        CircleCollider2D _collider;
        WorldHealthBar _healthBar;
        float _moveAnimationElapsed;
        float _attackAnimationElapsed;
        bool _isAttackAnimating;
        Vector2 _pbdPreviousPosition;

        public MonsterData Data => _data;
        public float CurrentHp => _hp;
        public float MaxHp { get; private set; }
        public bool IsResolved => _resolved;
        public SummonedUnitController UnitTarget => _unitTarget;
        public bool IsTargetingCore => _unitTarget == null;
        public bool IsAttackAnimating => _isAttackAnimating;
        public float AttackRange => _attackRange;
        public Vector2 PbdPreviousPosition => _pbdPreviousPosition;
        public float CombatRadius
        {
            get
            {
                float localRadius = _collider != null ? _collider.radius : 0.35f;
                return localRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            }
        }

        public void Initialize(GameManager gameManager, Transform target, MonsterData data,
            float hpMultiplier, float speedMultiplier, float rewardMultiplier,
            float sizeMultiplier = 1f)
        {
            _gameManager = gameManager;
            _coreTarget = target;
            _unitTarget = null;
            _data = data;
            MaxHp = Mathf.Max(1f, data.BaseHp * hpMultiplier);
            _hp = MaxHp;
            _speed = Mathf.Max(0.01f, data.MoveSpeed * speedMultiplier);
            _attackRange = Mathf.Max(0.1f, data.AttackRange);
            _attacksPerSecond = Mathf.Max(0.1f, data.AttacksPerSecond);
            _nextAttackTime = 0f;
            _nextTargetSearchTime = 0f;
            _contactDamage = Mathf.Max(0, data.ContactDamage);
            _rewardGold = Mathf.Max(0, Mathf.RoundToInt(data.RewardGold * rewardMultiplier));
            _resolved = false;
            _slowMultiplier = 1f;
            _slowUntil = 0f;
            _dotDamagePerSecond = 0f;
            _dotUntil = 0f;
            _nextDotTick = 0f;
            _moveAnimationElapsed = 0f;
            _attackAnimationElapsed = 0f;
            _isAttackAnimating = false;
            _pbdPreviousPosition = transform.position;
            if (_collider == null)
                _collider = GetComponent<CircleCollider2D>();
            ApplyVisual(data, sizeMultiplier);
        }

        void Update()
        {
            if (_resolved || _coreTarget == null || _gameManager == null ||
                _gameManager.IsRunOver || _gameManager.IsGameplayPaused)
                return;

            TickStatusEffects();
            if (_resolved) return;

            RefreshUnitTarget();
            Transform target = _unitTarget != null ? _unitTarget.transform : _coreTarget;
            if (target == null)
            {
                _unitTarget = null;
                return;
            }
            Vector3 targetPosition = target.position;
            Vector3 offset = targetPosition - transform.position;
            float arrivalDistance = _attackRange;
            if (offset.sqrMagnitude <= arrivalDistance * arrivalDistance)
            {
                if (_unitTarget != null)
                    AttackUnitTarget();
                else
                    AttackCoreTarget();
                TickCombatAnimation(false, Time.deltaTime);
                return;
            }

            float slowMultiplier = Time.time < _slowUntil ? _slowMultiplier : 1f;
            transform.position += offset.normalized * (_speed * slowMultiplier * Time.deltaTime);
            TickCombatAnimation(true, Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (_resolved || amount <= 0f) return;

            float previousHp = _hp;
            _hp = Mathf.Max(0f, _hp - amount);
            _healthBar?.SetHealth(_hp, MaxHp);
            _gameManager?.PresentDamageNumber(
                GetDamageNumberAnchor(),
                previousHp - _hp,
                DamageTextKind.Dealt);
            if (_hp <= 0f)
                ResolveDefeated();
        }

        public void ApplyDamage(DamagePacket packet)
        {
            if (_resolved) return;
            TakeDamage(packet.ResolveDamage(_data.Attribute));
            if (_resolved) return;

            if (packet.SlowPercent > 0f && packet.SlowDuration > 0f)
            {
                _slowMultiplier = Mathf.Min(_slowMultiplier, 1f - packet.SlowPercent);
                _slowUntil = Mathf.Max(_slowUntil, Time.time + packet.SlowDuration);
            }

            if (packet.DamageOverTime > 0f && packet.DamageOverTimeDuration > 0f)
            {
                _dotDamagePerSecond = Mathf.Max(_dotDamagePerSecond, packet.DamageOverTime);
                _dotUntil = Mathf.Max(_dotUntil, Time.time + packet.DamageOverTimeDuration);
                _nextDotTick = Mathf.Min(_nextDotTick <= 0f ? Time.time + 0.25f : _nextDotTick,
                    Time.time + 0.25f);
            }
        }

        public void ResetForPool()
        {
            _data = null;
            _gameManager = null;
            _coreTarget = null;
            _unitTarget = null;
            _hp = 0f;
            MaxHp = 0f;
            _attackRange = 0f;
            _attacksPerSecond = 0f;
            _nextAttackTime = 0f;
            _nextTargetSearchTime = 0f;
            _resolved = false;
            _slowMultiplier = 1f;
            _slowUntil = 0f;
            _dotDamagePerSecond = 0f;
            _dotUntil = 0f;
            _nextDotTick = 0f;
            _moveAnimationElapsed = 0f;
            _attackAnimationElapsed = 0f;
            _isAttackAnimating = false;
            _healthBar?.ResetForPool();
            _pbdPreviousPosition = Vector2.zero;
            transform.localScale = Vector3.one;
        }

        public void SetPbdResolvedPosition(Vector3 position) => _pbdPreviousPosition = position;

        void TickStatusEffects()
        {
            if (_dotDamagePerSecond <= 0f || Time.time >= _dotUntil)
            {
                if (Time.time >= _dotUntil)
                    _dotDamagePerSecond = 0f;
                return;
            }

            if (Time.time < _nextDotTick) return;
            const float tickInterval = 0.25f;
            _nextDotTick = Time.time + tickInterval;
            TakeDamage(_dotDamagePerSecond * tickInterval);
        }

        void RefreshUnitTarget()
        {
            if (_unitTarget != null && !IsValidUnitTarget(_unitTarget))
            {
                _unitTarget = null;
                _nextTargetSearchTime = 0f;
            }

            if (Time.time < _nextTargetSearchTime) return;
            _nextTargetSearchTime = Time.time + TargetSearchInterval;
            _unitTarget = FindNearestLivingUnit();
        }

        SummonedUnitController FindNearestLivingUnit()
        {
            var manager = _gameManager?.SummonedUnitManager;
            if (manager == null) return null;

            SummonedUnitController nearest = null;
            float nearestDistanceSq = float.MaxValue;
            var units = manager.Units;
            for (int i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (!IsValidUnitTarget(candidate)) continue;
                float distanceSq = (candidate.transform.position - transform.position).sqrMagnitude;
                if (distanceSq >= nearestDistanceSq) continue;
                nearest = candidate;
                nearestDistanceSq = distanceSq;
            }
            return nearest;
        }

        void AttackUnitTarget()
        {
            if (!IsValidUnitTarget(_unitTarget))
            {
                _unitTarget = null;
                _nextTargetSearchTime = 0f;
                return;
            }
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + 1f / _attacksPerSecond;
            PlayAttackAnimation();
            _unitTarget.TakeDamage(_contactDamage);
            if (!IsValidUnitTarget(_unitTarget))
            {
                _unitTarget = null;
                _nextTargetSearchTime = 0f;
            }
        }

        void AttackCoreTarget()
        {
            if (_gameManager == null || _gameManager.IsRunOver) return;
            if (Time.time < _nextAttackTime) return;

            _nextAttackTime = Time.time + 1f / _attacksPerSecond;
            PlayAttackAnimation();
            _gameManager.ApplyCoreDamage(_contactDamage);
        }

        static bool IsValidUnitTarget(SummonedUnitController unit) =>
            unit != null && unit.gameObject.activeInHierarchy && !unit.IsDefeated && unit.CurrentHp > 0f;

        Vector3 GetDamageNumberAnchor()
        {
            if (_renderer != null && _renderer.sprite != null)
            {
                Bounds bounds = _renderer.bounds;
                return new Vector3(
                    bounds.center.x,
                    Mathf.Lerp(bounds.center.y, bounds.max.y, 0.55f),
                    transform.position.z);
            }
            return transform.position + Vector3.up * 0.3f;
        }

        void ResolveDefeated()
        {
            if (_resolved) return;
            _resolved = true;
            _gameManager.NotifyMonsterDefeated(this, _rewardGold);
        }

        void ApplyVisual(MonsterData data, float spawnSizeMultiplier)
        {
            if (!TryGetComponent(out _renderer))
                _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = GetMoveFrame(data, 0) ?? data.Sprite ?? RuntimeSprite.Shared;
            _renderer.color = Color.white;
            float size = Mathf.Clamp(data.SizeMultiplier * Mathf.Max(0.1f, spawnSizeMultiplier), 0.5f, 4f);
            transform.localScale = Vector3.one * size;
            _renderer.sortingOrder = 2;
            if (_healthBar == null)
                _healthBar = WorldHealthBar.GetOrAdd(gameObject);
            _healthBar.Configure(_renderer, WorldHealthBarProfile.Monster);
            _healthBar.SetHealth(_hp, MaxHp);
            gameObject.name = data.DisplayName;
        }

        void TickMoveAnimation(float deltaTime)
        {
            if (_renderer == null || _data == null || _data.MoveFrames == null || _data.MoveFrames.Length == 0)
                return;

            _moveAnimationElapsed += Mathf.Max(0f, deltaTime);
            int frameIndex = Mathf.FloorToInt(_moveAnimationElapsed * _data.MoveAnimationFps) % _data.MoveFrames.Length;
            _renderer.sprite = GetMoveFrame(_data, frameIndex) ?? _data.Sprite ?? RuntimeSprite.Shared;
        }

        void TickCombatAnimation(bool moving, float deltaTime)
        {
            if (TickAttackAnimation(deltaTime))
                return;

            if (moving)
            {
                TickMoveAnimation(deltaTime);
                return;
            }

            if (_renderer != null && _data != null)
                _renderer.sprite = GetMoveFrame(_data, 0) ?? _data.Sprite ?? RuntimeSprite.Shared;
        }

        void PlayAttackAnimation()
        {
            if (_renderer == null || _data == null ||
                _data.AttackFrames == null || _data.AttackFrames.Length == 0)
                return;

            _isAttackAnimating = true;
            _attackAnimationElapsed = 0f;
            _renderer.sprite = GetAttackFrame(_data, 0) ?? GetMoveFrame(_data, 0) ??
                _data.Sprite ?? RuntimeSprite.Shared;
        }

        bool TickAttackAnimation(float deltaTime)
        {
            if (!_isAttackAnimating)
                return false;
            if (_renderer == null || _data == null ||
                _data.AttackFrames == null || _data.AttackFrames.Length == 0)
            {
                _isAttackAnimating = false;
                return false;
            }

            _attackAnimationElapsed += Mathf.Max(0f, deltaTime);
            int frameIndex = Mathf.FloorToInt(
                _attackAnimationElapsed * Mathf.Max(1f, _data.AttackAnimationFps));
            if (frameIndex >= _data.AttackFrames.Length)
            {
                _isAttackAnimating = false;
                _attackAnimationElapsed = 0f;
                return false;
            }

            _renderer.sprite = GetAttackFrame(_data, frameIndex) ?? GetMoveFrame(_data, 0) ??
                _data.Sprite ?? RuntimeSprite.Shared;
            return true;
        }

        static Sprite GetMoveFrame(MonsterData data, int frameIndex)
        {
            if (data == null || data.MoveFrames == null || data.MoveFrames.Length == 0) return null;
            int safeIndex = Mathf.Clamp(frameIndex, 0, data.MoveFrames.Length - 1);
            return data.MoveFrames[safeIndex];
        }

        static Sprite GetAttackFrame(MonsterData data, int frameIndex)
        {
            if (data == null || data.AttackFrames == null || data.AttackFrames.Length == 0) return null;
            int safeIndex = Mathf.Clamp(frameIndex, 0, data.AttackFrames.Length - 1);
            return data.AttackFrames[safeIndex];
        }

        static class RuntimeSprite
        {
            static Sprite _shared;
            public static Sprite Shared => _shared ??= Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }
    }
}
