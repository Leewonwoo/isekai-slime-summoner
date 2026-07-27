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
        SupportAuraVisual _supportAura;
        SummonedUnitManager _manager;
        SummonUnitInstance _instance;
        SummonUnitData _data;
        MonsterController _target;
        float _nextSearchTime;
        float _nextAttackTime;
        float _nextStar3SkillTime;
        float _nextSupportHealTime;
        float _star3AuraOverdriveUntil;
        float _moveAnimationElapsed;
        float _hp;
        float _shieldHp;
        Vector2 _pbdPreviousPosition;
        int _revivesUsedThisWave;
        bool _isDragging;
        bool _isMoving;

        public SummonUnitInstance Instance => _instance;
        public SummonUnitData Data => _data;
        public MonsterController Target => _target;
        public bool IsDragging => _isDragging;
        public bool IsDefeated => MaxHp > 0f && _hp <= 0f;
        public float CurrentHp => _hp;
        public float MaxHp { get; private set; }
        public bool IsStar3AuraOverdriveActive => Time.time < _star3AuraOverdriveUntil;
        public AnimatedOutlineFeedback Outline => _outline;
        public Vector2 PbdPreviousPosition => _pbdPreviousPosition;
        public float CombatRadius => (_collider != null ? _collider.radius : 0.38f) *
            Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<CircleCollider2D>();
            _outline = GetComponent<AnimatedOutlineFeedback>();
            _healthBar = GetComponent<WorldHealthBar>();
            _supportAura = GetComponent<SupportAuraVisual>();
        }

        public void Initialize(SummonedUnitManager manager, SummonUnitInstance instance)
        {
            _manager = manager;
            _instance = instance;
            _data = instance?.Unit;
            _target = null;
            _nextSearchTime = 0f;
            _nextAttackTime = 0f;
            _nextStar3SkillTime = 0f;
            _nextSupportHealTime = Time.time + (_data?.SupportHealInterval ?? 2f);
            _star3AuraOverdriveUntil = 0f;
            _moveAnimationElapsed = 0f;
            _hp = 0f;
            _shieldHp = 0f;
            _revivesUsedThisWave = 0;
            MaxHp = 0f;
            _isDragging = false;
            _isMoving = false;
            _pbdPreviousPosition = transform.position;
            RefreshRankVisual();
            _manager?.ApplyCurrentWaveRunState(this);
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
            if (_manager.IsGameplayPaused)
                return;
            TickStar3Skill();
            if (_data.AttackStyle == SummonAttackStyle.Support)
            {
                if (Time.time >= _nextSupportHealTime)
                {
                    _nextSupportHealTime = Time.time + _data.SupportHealInterval;
                    _manager.TryHealWithSupport(this);
                }
                TickMoveAnimation(false, 0f);
                return;
            }

            if (!IsTargetValid(_target) || Time.time >= _nextSearchTime)
            {
                _nextSearchTime = Time.time + SearchInterval;
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
                TickMoveAnimation(true, Time.deltaTime);
                transform.position += offset.normalized * (_data.MoveSpeed * Time.deltaTime);
                transform.position = _manager.ClampToField(transform.position);
                return;
            }

            TickMoveAnimation(false, 0f);
            if (Time.time < _nextAttackTime) return;
            Attack(_target);
            float speedMultiplier = _manager.GetSupportAttackSpeedMultiplier(this);
            _nextAttackTime = Time.time + 1f /
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
            if (!dragging)
                _pbdPreviousPosition = transform.position;
            _healthBar?.SetVisible(!dragging);
            _outline?.SetState(dragging ? state : UnitOutlineState.None);
            if (dragging)
                TickMoveAnimation(false, 0f);
        }

        public void SetDragFeedback(UnitOutlineState state) => _outline?.SetState(state);

        public void PresentSupportBuff() => _supportAura?.PlayPulse();

        public void SetPbdResolvedPosition(Vector3 position) => _pbdPreviousPosition = position;

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

            float deltaTime = Time.deltaTime;
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
            _star3AuraOverdriveUntil = 0f;
            _nextSupportHealTime = Time.time + (_data?.SupportHealInterval ?? 2f);
            _moveAnimationElapsed = 0f;
            _isMoving = false;
            _renderer.sprite = _data.WorldSpriteAtRank(_instance.Rank);
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
            _supportAura?.Configure(
                _renderer,
                _data.SupportRadius,
                _data.AttackStyle == SummonAttackStyle.Support);
            ArmStar3Skill();
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f || IsDefeated) return;
            float remainingDamage = amount;
            if (_shieldHp > 0f)
            {
                float absorbed = Mathf.Min(_shieldHp, remainingDamage);
                _shieldHp -= absorbed;
                remainingDamage -= absorbed;
            }
            _hp = Mathf.Max(0f, _hp - remainingDamage);
            _healthBar?.SetHealth(_hp, MaxHp);
            _manager?.PresentDamageNumber(
                GetDamageNumberAnchor(),
                amount,
                DamageTextKind.Received);
            if (_hp <= 0f)
            {
                float reviveFraction = _manager?.SlimeReviveFraction ?? 0f;
                if (_revivesUsedThisWave == 0 && reviveFraction > 0f)
                {
                    _revivesUsedThisWave++;
                    _hp = Mathf.Max(1f, MaxHp * Mathf.Clamp01(reviveFraction));
                    _healthBar?.SetHealth(_hp, MaxHp);
                    return;
                }
                _manager?.NotifyUnitDefeated(this);
            }
        }

        public void ApplyDamage(DamagePacket packet)
        {
            MonsterAttribute defense =
                _data != null ? _data.Attribute : MonsterAttribute.None;
            TakeDamage(packet.ResolveDamage(defense));
        }

        public void PrepareForWave(float shieldFraction)
        {
            _revivesUsedThisWave = 0;
            _shieldHp = MaxHp * Mathf.Clamp01(shieldFraction);
            _star3AuraOverdriveUntil = 0f;
            ArmStar3Skill();
        }

        public void ActivateStar3AuraOverdrive(float duration)
        {
            if (_data == null || _instance == null ||
                _instance.Rank != SummonRank.MaxInternalRank)
                return;
            _star3AuraOverdriveUntil = Mathf.Max(
                _star3AuraOverdriveUntil,
                Time.time + Mathf.Max(0f, duration));
        }

        public float Heal(float amount)
        {
            if (amount <= 0f || IsDefeated || MaxHp <= 0f) return 0f;
            float previousHp = _hp;
            _hp = Mathf.Min(MaxHp, _hp + amount);
            _healthBar?.SetHealth(_hp, MaxHp);
            return _hp - previousHp;
        }

        public bool RestoreHealthRatio(float hpRatio)
        {
            if (MaxHp <= 0f || hpRatio <= 0f)
                return false;
            _hp = MaxHp * Mathf.Clamp01(hpRatio);
            _healthBar?.SetHealth(_hp, MaxHp);
            return true;
        }

        public Vector3 GetFloatingTextAnchor() => GetDamageNumberAnchor();

        public Vector3 GetHeadEffectAnchor()
        {
            if (_renderer != null && _renderer.sprite != null)
            {
                Bounds bounds = _renderer.bounds;
                return new Vector3(bounds.center.x, bounds.max.y, transform.position.z);
            }
            return transform.position + Vector3.up * 0.45f;
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
            _nextStar3SkillTime = 0f;
            _nextSupportHealTime = 0f;
            _star3AuraOverdriveUntil = 0f;
            _hp = 0f;
            _shieldHp = 0f;
            _revivesUsedThisWave = 0;
            _pbdPreviousPosition = Vector2.zero;
            MaxHp = 0f;
            if (_collider != null) _collider.enabled = true;
            if (_renderer != null) _renderer.sprite = null;
            _healthBar?.ResetForPool();
            _supportAura?.ResetForPool();
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

            if (_data.AttackStyle == SummonAttackStyle.Support)
            {
                Gizmos.color = new Color(0.5f, 0.9f, 0.6f, 0.85f);
                Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.1f, _data.SupportRadius));
            }
        }

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

        void Attack(MonsterController target)
        {
            Sprite projectileSprite = _data.ProjectileSpriteAtRank(_instance.Rank);
            float projectileScale = _data.ProjectileScaleAtRank(_instance.Rank);
            string unitId = _data.UnitId;
            MonsterAttribute attribute = _data.Attribute;
            SummonAttackStyle attackStyle = _data.AttackStyle;
            int rank = _instance.Rank;
            Vector3 origin = GetHeadEffectAnchor();
            SlimeAttackEffectService attackEffects = _manager.AttackEffects;
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
                    attackEffects?.PlayMelee(
                        unitId,
                        attribute,
                        rank,
                        origin,
                        target.transform.position);
                    break;
                case SummonAttackStyle.Area:
                    if (projectileSprite != null)
                    {
                        bool fired = _manager.Projectiles.Fire(
                            origin,
                            target,
                            projectileSprite,
                            packet,
                            _data.ProjectileSpeed,
                            0.42f * projectileScale,
                            Mathf.Max(0.6f, _data.AreaRadius),
                            onImpact: context => attackEffects?.PlayImpact(
                                unitId,
                                attribute,
                                attackStyle,
                                rank,
                                context.Position));
                        if (fired)
                            attackEffects?.PlayLaunch(
                                unitId,
                                attribute,
                                attackStyle,
                                rank,
                                origin,
                                target.transform.position);
                    }
                    else
                    {
                        _manager.ApplyAreaDamage(target.transform.position,
                            Mathf.Max(0.6f, _data.AreaRadius), packet);
                        attackEffects?.PlayImpact(
                            unitId,
                            attribute,
                            attackStyle,
                            rank,
                            target.transform.position);
                    }
                    break;
                case SummonAttackStyle.Piercing:
                case SummonAttackStyle.Projectile:
                    bool launched = _manager.Projectiles.Fire(
                        origin,
                        target,
                        projectileSprite != null ? projectileSprite : _data.WorldSprite,
                        packet,
                        _data.ProjectileSpeed,
                        0.35f * projectileScale,
                        _data.AreaRadius,
                        _data.AttackStyle == SummonAttackStyle.Piercing ? _data.PierceCount : 1,
                        onImpact: context => attackEffects?.PlayImpact(
                            unitId,
                            attribute,
                            attackStyle,
                            rank,
                            context.Position));
                    if (launched)
                        attackEffects?.PlayLaunch(
                            unitId,
                            attribute,
                            attackStyle,
                            rank,
                            origin,
                            target.transform.position);
                    break;
            }
        }

        void TickStar3Skill()
        {
            if (_data == null || _instance == null ||
                _instance.Rank != SummonRank.MaxInternalRank ||
                !_data.HasStar3Skill)
                return;

            if (_nextStar3SkillTime <= 0f)
                ArmStar3Skill();
            if (Time.time < _nextStar3SkillTime)
                return;

            if (_manager.TryCastStar3Skill(this))
            {
                _nextStar3SkillTime = Time.time + _data.Star3SkillCooldown;
                return;
            }

            // 대상이 없을 때 매 프레임 전체 적 목록을 훑지 않도록 짧게 재시도한다.
            _nextStar3SkillTime = Time.time + 0.25f;
        }

        void ArmStar3Skill()
        {
            if (_data == null || _instance == null ||
                _instance.Rank != SummonRank.MaxInternalRank ||
                !_data.HasStar3Skill)
            {
                _nextStar3SkillTime = 0f;
                return;
            }

            float stagger = (_instance.InstanceId % 9) * 0.1f;
            _nextStar3SkillTime = Time.time + _data.Star3SkillCooldown * 0.6f + stagger;
        }

        void TickMoveAnimation(bool moving, float deltaTime)
        {
            if (_renderer == null || _data == null) return;
            Sprite rankSprite = _data.WorldSpriteAtRank(_instance?.Rank ?? SummonRank.MinInternalRank);
            Sprite[] frames = _data.MoveFrames;
            bool canAnimate = moving &&
                _instance != null &&
                _instance.Rank == SummonRank.MinInternalRank &&
                frames != null &&
                frames.Length > 0;
            if (!canAnimate)
            {
                if (_renderer.sprite != rankSprite)
                {
                    _renderer.sprite = rankSprite;
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
