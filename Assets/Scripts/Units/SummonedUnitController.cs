using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    [DisallowMultipleComponent]
    public sealed class SummonedUnitController : MonoBehaviour
    {
        const float SearchInterval = 0.15f;
        const int DefaultSortingOrder = 4;
        const int DragSortingOrder = 20;

        SpriteRenderer _renderer;
        CircleCollider2D _collider;
        AnimatedOutlineFeedback _outline;
        WorldHealthBar _healthBar;
        SummonedUnitManager _manager;
        SummonUnitInstance _instance;
        SummonUnitData _data;
        MonsterController _target;
        float _nextSearchTime;
        float _nextAttackTime;
        float _moveAnimationElapsed;
        float _hp;
        bool _isDragging;
        bool _isMoving;

        public SummonUnitInstance Instance => _instance;
        public SummonUnitData Data => _data;
        public MonsterController Target => _target;
        public bool IsDragging => _isDragging;
        public bool IsDefeated => MaxHp > 0f && _hp <= 0f;
        public float CurrentHp => _hp;
        public float MaxHp { get; private set; }
        public AnimatedOutlineFeedback Outline => _outline;
        public float CombatRadius => (_collider != null ? _collider.radius : 0.38f) *
            Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _outline = GetComponent<AnimatedOutlineFeedback>();
            _healthBar = GetComponent<WorldHealthBar>();
        }

        public void Initialize(SummonedUnitManager manager, SummonUnitInstance instance)
        {
            _manager = manager;
            _instance = instance;
            _data = instance?.Unit;
            _target = null;
            _nextSearchTime = 0f;
            _nextAttackTime = 0f;
            _moveAnimationElapsed = 0f;
            _hp = 0f;
            MaxHp = 0f;
            _isDragging = false;
            _isMoving = false;
            RefreshRankVisual();
            _outline?.SetState(UnitOutlineState.None);
        }

        void Update()
        {
            if (_manager == null || _data == null || _instance == null || _isDragging)
            {
                TickMoveAnimation(false, 0f);
                return;
            }
            if (!_manager.CanUnitsFight)
            {
                return;
            }
            if (_data.AttackStyle == SummonAttackStyle.Support)
            {
                TickMoveAnimation(false, 0f);
                return;
            }

            if (!IsTargetValid(_target) || Time.unscaledTime >= _nextSearchTime)
            {
                _nextSearchTime = Time.unscaledTime + SearchInterval;
                _target = _manager.FindTarget(this);
            }

            if (!IsTargetValid(_target))
            {
                TickMoveAnimation(false, 0f);
                return;
            }
            Vector3 offset = _target.transform.position - transform.position;
            float attackRange = Mathf.Max(0.1f, _data.AttackRange);
            if (offset.sqrMagnitude > attackRange * attackRange)
            {
                TickMoveAnimation(true, Time.unscaledDeltaTime);
                transform.position += offset.normalized * (_data.MoveSpeed * Time.unscaledDeltaTime);
                transform.position = _manager.ClampToField(transform.position);
                return;
            }

            TickMoveAnimation(false, 0f);
            if (Time.unscaledTime < _nextAttackTime) return;
            Attack(_target);
            float speedMultiplier = _manager.GetSupportAttackSpeedMultiplier(this);
            _nextAttackTime = Time.unscaledTime + 1f /
                Mathf.Max(0.1f, _data.AttacksPerSecondAtRank(_instance.Rank) *
                    _instance.AttackSpeedMultiplier * speedMultiplier *
                    _manager.SlimeAttackSpeedMultiplier);
        }

        public void SetDragging(bool dragging, UnitOutlineState state = UnitOutlineState.Selected)
        {
            _isDragging = dragging;
            _target = null;
            if (_collider != null)
                _collider.enabled = !dragging;
            if (_renderer != null)
                _renderer.sortingOrder = dragging ? DragSortingOrder : DefaultSortingOrder;
            _healthBar?.SetVisible(!dragging);
            _outline?.SetState(dragging ? state : UnitOutlineState.None);
            if (dragging)
                TickMoveAnimation(false, 0f);
        }

        public void SetDragFeedback(UnitOutlineState state) => _outline?.SetState(state);

        public void StopFormationMotion() => TickMoveAnimation(false, 0f);

        public bool MoveTowardFormation(Vector3 targetPosition, float speed, float stoppingDistance)
        {
            if (_manager == null || _data == null || _instance == null || _isDragging || IsDefeated)
                return true;

            _target = null;
            Vector3 offset = targetPosition - transform.position;
            float stop = Mathf.Max(0.01f, stoppingDistance);
            if (offset.sqrMagnitude <= stop * stop)
            {
                transform.position = _manager.ClampToField(targetPosition);
                TickMoveAnimation(false, 0f);
                return true;
            }

            float deltaTime = Time.unscaledDeltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                Mathf.Max(0.1f, speed) * deltaTime);
            transform.position = _manager.ClampToField(transform.position);
            TickMoveAnimation(true, deltaTime);
            return false;
        }

        public void RefreshRankVisual()
        {
            if (_data == null || _instance == null || _renderer == null) return;
            float previousMaxHp = MaxHp;
            float previousHealthRatio = previousMaxHp > 0f ? _hp / previousMaxHp : 1f;
            _target = null;
            _nextSearchTime = 0f;
            _nextAttackTime = 0f;
            _moveAnimationElapsed = 0f;
            _isMoving = false;
            _renderer.sprite = _data.WorldSprite;
            _renderer.color = _data.Tint;
            _renderer.sortingOrder = 4;
            transform.localScale = Vector3.one * _data.ScaleAtRank(_instance.Rank);
            MaxHp = Mathf.Max(1f, _data.MaxHpAtRank(_instance.Rank));
            _hp = MaxHp * Mathf.Clamp01(previousHealthRatio);
            gameObject.name = $"{_data.NameAtRank(_instance.Rank)} #{_instance.InstanceId}";
            if (_collider != null)
                _collider.radius = 0.38f;
            if (_healthBar != null)
            {
                _healthBar.Configure(_renderer, WorldHealthBarProfile.SummonedUnit);
                _healthBar.SetHealth(_hp, MaxHp);
            }
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDefeated) return;
            _hp = Mathf.Max(0f, _hp - amount);
            _healthBar?.SetHealth(_hp, MaxHp);
            if (_hp <= 0f)
                _manager?.NotifyUnitDefeated(this);
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || IsDefeated || MaxHp <= 0f) return;
            _hp = Mathf.Min(MaxHp, _hp + amount);
            _healthBar?.SetHealth(_hp, MaxHp);
        }

        public void ResetForPool()
        {
            _manager = null;
            _instance = null;
            _data = null;
            _target = null;
            _isDragging = false;
            _isMoving = false;
            _moveAnimationElapsed = 0f;
            _hp = 0f;
            MaxHp = 0f;
            if (_collider != null) _collider.enabled = true;
            if (_renderer != null) _renderer.sprite = null;
            _healthBar?.ResetForPool();
            _outline?.SetState(UnitOutlineState.None);
        }

        void OnDrawGizmosSelected()
        {
            if (_data == null) return;

            if (_manager != null)
            {
                Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.65f);
                Gizmos.DrawWireSphere(transform.position, _manager.TargetSearchRange);
            }

            Gizmos.color = new Color(1f, 0.72f, 0.2f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, _data.AttackRange));
        }

        void Attack(MonsterController target)
        {
            var packet = new DamagePacket(
                this,
                _manager.ModifySlimeDamage(
                    _data.DamageAtRank(_instance.Rank) * _instance.DamageMultiplier),
                _data.Attribute,
                _data.SlowPercent,
                _data.SlowDuration,
                _data.DamageOverTime,
                _data.DamageOverTimeDuration);

            switch (_data.AttackStyle)
            {
                case SummonAttackStyle.Melee:
                    target.ApplyDamage(packet);
                    break;
                case SummonAttackStyle.Area:
                    if (_data.ProjectileSprite != null)
                    {
                        _manager.Projectiles.Fire(
                            transform.position,
                            target,
                            _data.ProjectileSprite,
                            packet,
                            _data.ProjectileSpeed,
                            0.42f,
                            Mathf.Max(0.6f, _data.AreaRadius));
                    }
                    else
                    {
                        _manager.ApplyAreaDamage(target.transform.position,
                            Mathf.Max(0.6f, _data.AreaRadius), packet);
                    }
                    break;
                case SummonAttackStyle.Piercing:
                case SummonAttackStyle.Projectile:
                    _manager.Projectiles.Fire(
                        transform.position,
                        target,
                        _data.ProjectileSprite != null ? _data.ProjectileSprite : _data.WorldSprite,
                        packet,
                        _data.ProjectileSpeed,
                        0.35f,
                        _data.AreaRadius,
                        _data.AttackStyle == SummonAttackStyle.Piercing ? _data.PierceCount : 1);
                    break;
            }
        }

        void TickMoveAnimation(bool moving, float deltaTime)
        {
            if (_renderer == null || _data == null) return;
            Sprite[] frames = _data.MoveFrames;
            bool canAnimate = moving && frames != null && frames.Length > 0;
            if (!canAnimate)
            {
                if (_isMoving && _renderer.sprite != _data.WorldSprite)
                {
                    _renderer.sprite = _data.WorldSprite;
                    _healthBar?.RefreshLayout();
                }

                _isMoving = false;
                _moveAnimationElapsed = 0f;
                return;
            }

            if (!_isMoving)
            {
                _isMoving = true;
                _moveAnimationElapsed = 0f;
            }
            else
            {
                _moveAnimationElapsed += Mathf.Max(0f, deltaTime);
            }

            int frameIndex = Mathf.FloorToInt(_moveAnimationElapsed * _data.MoveAnimationFps) % frames.Length;
            Sprite frame = frames[frameIndex];
            if (frame == null || _renderer.sprite == frame) return;
            _renderer.sprite = frame;
            _healthBar?.RefreshLayout();
        }

        static bool IsTargetValid(MonsterController target) =>
            target != null && target.gameObject.activeInHierarchy && !target.IsResolved && target.CurrentHp > 0f;
    }
}
