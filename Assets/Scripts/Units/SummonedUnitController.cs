using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    [DisallowMultipleComponent]
    public sealed class SummonedUnitController : MonoBehaviour
    {
        const float SearchInterval = 0.15f;

        SpriteRenderer _renderer;
        CircleCollider2D _collider;
        AnimatedOutlineFeedback _outline;
        SummonedUnitManager _manager;
        SummonUnitInstance _instance;
        SummonUnitData _data;
        MonsterController _target;
        float _nextSearchTime;
        float _nextAttackTime;
        bool _isDragging;

        public SummonUnitInstance Instance => _instance;
        public SummonUnitData Data => _data;
        public MonsterController Target => _target;
        public bool IsDragging => _isDragging;
        public AnimatedOutlineFeedback Outline => _outline;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _outline = GetComponent<AnimatedOutlineFeedback>();
        }

        public void Initialize(SummonedUnitManager manager, SummonUnitInstance instance)
        {
            _manager = manager;
            _instance = instance;
            _data = instance?.Unit;
            _target = null;
            _nextSearchTime = 0f;
            _nextAttackTime = 0f;
            _isDragging = false;
            RefreshRankVisual();
            _outline?.SetState(UnitOutlineState.None);
        }

        void Update()
        {
            if (_manager == null || _data == null || _instance == null || _isDragging)
                return;
            if (!_manager.CanUnitsFight)
                return;
            if (_data.AttackStyle == SummonAttackStyle.Support)
                return;

            if (!IsTargetValid(_target) || Time.unscaledTime >= _nextSearchTime)
            {
                _nextSearchTime = Time.unscaledTime + SearchInterval;
                _target = _manager.FindTarget(this);
            }

            if (!IsTargetValid(_target)) return;
            Vector3 offset = _target.transform.position - transform.position;
            float attackRange = Mathf.Max(0.1f, _data.AttackRange);
            if (offset.sqrMagnitude > attackRange * attackRange)
            {
                Vector3 separation = _manager.CalculateSeparation(this);
                Vector3 direction = (offset.normalized + separation).normalized;
                transform.position += direction * (_data.MoveSpeed * Time.unscaledDeltaTime);
                transform.position = _manager.ClampToField(transform.position);
                return;
            }

            if (Time.unscaledTime < _nextAttackTime) return;
            Attack(_target);
            float speedMultiplier = _manager.GetSupportAttackSpeedMultiplier(this);
            _nextAttackTime = Time.unscaledTime + 1f /
                Mathf.Max(0.1f, _data.AttacksPerSecondAtRank(_instance.Rank) * speedMultiplier);
        }

        public void SetDragging(bool dragging, UnitOutlineState state = UnitOutlineState.Selected)
        {
            _isDragging = dragging;
            _target = null;
            _collider.enabled = !dragging;
            _outline?.SetState(dragging ? state : UnitOutlineState.None);
        }

        public void SetDragFeedback(UnitOutlineState state) => _outline?.SetState(state);

        public void RefreshRankVisual()
        {
            if (_data == null || _instance == null || _renderer == null) return;
            _target = null;
            _nextSearchTime = 0f;
            _nextAttackTime = 0f;
            _renderer.sprite = _data.WorldSprite;
            _renderer.color = _data.Tint;
            _renderer.sortingOrder = 4;
            transform.localScale = Vector3.one * _data.ScaleAtRank(_instance.Rank);
            gameObject.name = $"{_data.NameAtRank(_instance.Rank)} #{_instance.InstanceId}";
            if (_collider != null)
                _collider.radius = 0.38f;
        }

        public void ResetForPool()
        {
            _manager = null;
            _instance = null;
            _data = null;
            _target = null;
            _isDragging = false;
            if (_collider != null) _collider.enabled = true;
            if (_renderer != null) _renderer.sprite = null;
            _outline?.SetState(UnitOutlineState.None);
        }

        void Attack(MonsterController target)
        {
            var packet = new DamagePacket(
                this,
                _data.DamageAtRank(_instance.Rank),
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

        static bool IsTargetValid(MonsterController target) =>
            target != null && target.gameObject.activeInHierarchy && !target.IsResolved && target.CurrentHp > 0f;
    }
}
