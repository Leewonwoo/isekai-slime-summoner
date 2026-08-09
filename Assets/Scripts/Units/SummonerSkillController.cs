using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Units
{
    [DisallowMultipleComponent]
    public sealed class SummonerSkillController : MonoBehaviour
    {
        const float TargetingTimeout = 5f;
        const float IceWallHalfLength = 1.6f;
        const float MeteorFallSpeed = 11f;
        static readonly Vector3 MeteorLaunchOffset = new(3.2f, 4.8f, 0f);

        sealed class IceWallZone
        {
            public Vector2 Center;
            public Vector2 Axis;
            public float ExpiresAt;
            public float DamageMultiplier;
            public float Radius;
            public MonsterAttribute Attribute;
            public float SlowPercent;
            public float SlowDuration;
            public readonly HashSet<MonsterController> Hit = new();
        }

        sealed class RelicBarrage
        {
            public SummonerSkillId SkillId;
            public SkillExecutionMode ExecutionMode;
            public int Rank;
            public Vector3 Center;
            public int StrikeCount;
            public int NextStrikeIndex;
            public float NextStrikeAt;
            public float StrikeInterval;
            public float DamageMultiplier;
            public float Radius;
            public float StatusDurationMultiplier;
            public SkillRankProfile RankProfile;
            public MonsterAttribute Attribute;
        }

        readonly Dictionary<SummonerSkillId, float> _cooldowns = new();
        readonly List<IceWallZone> _iceWalls = new();
        readonly List<RelicBarrage> _relicBarrages = new();
        readonly List<MonsterController> _monsterBuffer = new(64);
        readonly List<SummonerSkillId> _cooldownKeys = new(3);

        GameManager _gameManager;
        SummonerAttackController _summonerAttack;
        RelicProgression _relics;
        SkillCatalog _skillCatalog;
        CombatEffectService _skillEffects;
        SkillParticleEffectService _particleEffects;
        Sprite _meteorProjectileSprite;
        Sprite[] _meteorFrames;
        Sprite[] _iceWallFrames;
        Sprite _aegisSprite;
        SpriteRenderer _targetPreview;
        bool _targeting;
        float _targetingExpiresAt;

        public RelicProgression Relics => _relics;
        public SummonerSkillId EquippedSkill =>
            _relics?.EquippedDefinition?.SkillId ?? SummonerSkillId.ArcaneBurst;
        public SkillData EquippedSkillData => _skillCatalog?.FindActive(EquippedSkill);
        public SummonerSkillDefinition EquippedDefinition => BuildEquippedDefinition();
        public bool IsTargeting => _targeting;
        public float RemainingCooldown =>
            _cooldowns.TryGetValue(EquippedSkill, out float value) ? Mathf.Max(0f, value) : 0f;
        public bool IsReady => RemainingCooldown <= 0f;

        public event Action StateChanged;
        public event Action<SummonerSkillId> SkillCast;

        public void Initialize(
            GameManager gameManager,
            SummonerAttackController summonerAttack,
            RelicProgression relics,
            SkillCatalog skillCatalog,
            Sprite meteorProjectileSprite,
            Sprite[] meteorFrames,
            Sprite[] iceWallFrames,
            Sprite aegisSprite)
        {
            _gameManager = gameManager;
            _summonerAttack = summonerAttack;
            _skillCatalog = skillCatalog;
            if (_relics != null)
                _relics.Changed -= OnRelicChanged;
            _relics = relics;
            if (_relics != null)
                _relics.Changed += OnRelicChanged;
            _skillEffects ??= new CombatEffectService(
                transform,
                rootName: "SummonerSkillEffects");
            _particleEffects ??= new SkillParticleEffectService(
                transform,
                () => _gameManager != null &&
                      !_gameManager.IsGameplayPaused &&
                      _gameManager.Phase == RunPhase.InWave,
                "SummonerSkillParticleEffects");
            _meteorProjectileSprite = meteorProjectileSprite;
            _meteorFrames = meteorFrames;
            _iceWallFrames = iceWallFrames;
            _aegisSprite = aegisSprite;
            BuildTargetPreview();
            StateChanged?.Invoke();
        }

        void Update()
        {
            if (_gameManager == null)
                return;

            if (_gameManager.IsRunOver || _gameManager.Phase != RunPhase.InWave)
                _relicBarrages.Clear();

            if (!_gameManager.IsGameplayPaused && _gameManager.Phase == RunPhase.InWave)
            {
                bool changed = false;
                _cooldownKeys.Clear();
                _cooldownKeys.AddRange(_cooldowns.Keys);
                for (int i = 0; i < _cooldownKeys.Count; i++)
                {
                    SummonerSkillId key = _cooldownKeys[i];
                    float previous = _cooldowns[key];
                    float recoveryMultiplier =
                        _gameManager.SummonerBuffs?.RelicCooldownRecoveryMultiplier ?? 1f;
                    float next = Mathf.Max(
                        0f,
                        previous - Time.deltaTime * recoveryMultiplier);
                    _cooldowns[key] = next;
                    changed |= Mathf.CeilToInt(previous) != Mathf.CeilToInt(next);
                }
                TickIceWalls();
                TickRelicBarrages();
                if (changed)
                    StateChanged?.Invoke();
            }

            if (_targeting && Time.unscaledTime >= _targetingExpiresAt)
                CancelTargeting();
        }

        void OnDestroy()
        {
            if (_relics != null)
                _relics.Changed -= OnRelicChanged;
        }

        public bool PressSkillButton()
        {
            if (_gameManager == null || _gameManager.IsRunOver ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave ||
                !IsReady)
                return false;

            SummonerSkillDefinition definition = EquippedDefinition;
            SkillData skill = EquippedSkillData;
            if (skill == null)
                return false;
            if (definition.Targeting == SummonerSkillTargeting.Instant)
            {
                bool cast = CastAegis(
                    definition,
                    skill.RankProfile(_relics?.EquippedRank ?? 1));
                if (cast)
                    SkillCast?.Invoke(definition.Id);
                return cast;
            }
            if (_targeting)
            {
                CancelTargeting();
                return false;
            }

            _targeting = true;
            _targetingExpiresAt = Time.unscaledTime + TargetingTimeout;
            SetPreviewVisible(false);
            StateChanged?.Invoke();
            return true;
        }

        public bool TryCastAt(Vector3 worldPoint)
        {
            if (!_targeting || !IsReady || _gameManager == null ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave)
                return false;

            SummonerSkillDefinition definition = EquippedDefinition;
            SkillData skill = EquippedSkillData;
            if (skill == null)
                return false;
            SkillRankProfile rankProfile = skill.RankProfile(_relics?.EquippedRank ?? 1);
            float damageMultiplier = 1f;
            float radiusMultiplier = 1f;
            float statusDurationMultiplier = 1f;
            bool amplified = _gameManager.SummonerBuffs != null &&
                _gameManager.SummonerBuffs.GetRelicAmplification(
                    out damageMultiplier,
                    out radiusMultiplier,
                    out statusDurationMultiplier);
            int relicRank = Mathf.Max(1, _relics?.EquippedRank ?? 1);
            bool cast = skill.ExecutionMode switch
            {
                SkillExecutionMode.Meteor => relicRank >= 2
                    ? CastRelicBarrage(
                        worldPoint, definition, relicRank, damageMultiplier,
                        radiusMultiplier, statusDurationMultiplier, skill, rankProfile)
                    : CastMeteor(
                        worldPoint, definition, damageMultiplier,
                        radiusMultiplier, statusDurationMultiplier, skill, rankProfile),
                SkillExecutionMode.IceWall => relicRank >= 2
                    ? CastRelicBarrage(
                        worldPoint, definition, relicRank, damageMultiplier,
                        radiusMultiplier, statusDurationMultiplier, skill, rankProfile)
                    : CastIceWall(
                        worldPoint, definition, damageMultiplier,
                        radiusMultiplier, statusDurationMultiplier, rankProfile),
                SkillExecutionMode.ElementBurst => relicRank >= 2
                    ? CastRelicBarrage(
                        worldPoint, definition, relicRank, damageMultiplier,
                        radiusMultiplier, statusDurationMultiplier, skill, rankProfile)
                    : CastElementBurst(
                        worldPoint, definition, skill.Attribute,
                        damageMultiplier, radiusMultiplier),
                _ => false,
            };
            if (!cast)
                return false;
            if (amplified)
                _gameManager.SummonerBuffs.ConsumeRelicAmplification();
            _targeting = false;
            SetPreviewVisible(false);
            StartCooldown(definition);
            SkillCast?.Invoke(definition.Id);
            return true;
        }

        public void UpdateTargetPreview(Vector3 worldPoint)
        {
            if (!_targeting || _targetPreview == null)
                return;
            SummonerSkillDefinition definition = EquippedDefinition;
            SkillExecutionMode mode =
                EquippedSkillData?.ExecutionMode ?? SkillExecutionMode.Meteor;
            _targetPreview.transform.position = worldPoint;
            _targetPreview.sprite = mode == SkillExecutionMode.IceWall
                ? FirstFrame(_iceWallFrames)
                : FirstFrame(_meteorFrames);
            _targetPreview.transform.localScale = Vector3.one *
                (mode == SkillExecutionMode.IceWall ? 1.2f : definition.Radius * 0.72f);
            if (mode == SkillExecutionMode.IceWall && _gameManager.Summoner != null)
            {
                Vector2 direction = worldPoint - _gameManager.Summoner.position;
                if (direction.sqrMagnitude > 0.001f)
                    _targetPreview.transform.rotation =
                        Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
            }
            else
            {
                _targetPreview.transform.rotation = Quaternion.identity;
            }
            SetPreviewVisible(_targetPreview.sprite != null);
        }

        public void CancelTargeting()
        {
            if (!_targeting)
                return;
            _targeting = false;
            SetPreviewVisible(false);
            StateChanged?.Invoke();
        }

        bool CastRelicBarrage(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            int rank,
            float damageMultiplier,
            float radiusMultiplier,
            float statusDurationMultiplier,
            SkillData skill,
            SkillRankProfile rankProfile)
        {
            if (skill == null || rankProfile == null)
                return false;
            var barrage = new RelicBarrage
            {
                SkillId = definition.Id,
                ExecutionMode = skill.ExecutionMode,
                Rank = rank,
                Center = worldPoint,
                StrikeCount = rankProfile.StrikeCount,
                NextStrikeAt = Time.time,
                StrikeInterval = rankProfile.StrikeInterval,
                DamageMultiplier = definition.DamageMultiplier *
                    damageMultiplier * rankProfile.PerStrikeDamageMultiplier,
                Radius = definition.Radius * radiusMultiplier *
                    rankProfile.PerStrikeRadiusMultiplier,
                StatusDurationMultiplier = statusDurationMultiplier,
                RankProfile = rankProfile,
                Attribute = skill.Attribute,
            };
            _relicBarrages.Add(barrage);
            FireNextRelicBarrageStrike(barrage);
            return true;
        }

        void TickRelicBarrages()
        {
            for (int i = _relicBarrages.Count - 1; i >= 0; i--)
            {
                RelicBarrage barrage = _relicBarrages[i];
                if (barrage.NextStrikeIndex >= barrage.StrikeCount)
                {
                    _relicBarrages.RemoveAt(i);
                    continue;
                }
                if (Time.time >= barrage.NextStrikeAt)
                    FireNextRelicBarrageStrike(barrage);
            }
        }

        void FireNextRelicBarrageStrike(RelicBarrage barrage)
        {
            int strikeIndex = barrage.NextStrikeIndex++;
            Vector3 strikePoint = ResolveRelicBarragePoint(barrage, strikeIndex);
            barrage.NextStrikeAt = Time.time + barrage.StrikeInterval;

            switch (barrage.ExecutionMode)
            {
                case SkillExecutionMode.Meteor:
                    LaunchMeteor(
                        strikePoint,
                        barrage.DamageMultiplier,
                        barrage.Radius,
                        barrage.RankProfile.DamageOverTimeMultiplier,
                        barrage.RankProfile.DamageOverTimeDuration *
                            barrage.StatusDurationMultiplier,
                        barrage.RankProfile.VisualScale,
                        Mathf.Max(1f, barrage.Radius),
                        barrage.Attribute);
                    break;
                case SkillExecutionMode.IceWall:
                    CastIceBarrageStrike(strikePoint, barrage);
                    break;
                case SkillExecutionMode.ElementBurst:
                    CastElementBarrageStrike(strikePoint, barrage, barrage.Attribute);
                    break;
            }
        }

        Vector3 ResolveRelicBarragePoint(RelicBarrage barrage, int strikeIndex)
        {
            if (!barrage.RankProfile.BattlefieldWide)
            {
                if (strikeIndex == 0)
                    return barrage.Center;
                float angle = (strikeIndex - 1) * Mathf.PI * 2f /
                    Mathf.Max(1, barrage.StrikeCount - 1);
                float spread = Mathf.Max(0.55f, barrage.Radius * 0.9f);
                return barrage.Center + new Vector3(
                    Mathf.Cos(angle) * spread,
                    Mathf.Sin(angle) * spread,
                    0f);
            }

            SpriteRenderer background = _gameManager?.GameplayBackground;
            Bounds bounds = background != null && background.sprite != null
                ? background.bounds
                : new Bounds(
                    _gameManager?.Summoner != null
                        ? _gameManager.Summoner.position
                        : Vector3.zero,
                    new Vector3(10f, 7f, 0f));

            // Low-discrepancy placement makes every ★3 cast sweep the whole play field.
            float x01 = Mathf.Repeat(0.5f + strikeIndex * 0.6180339f, 1f);
            float y01 = Mathf.Repeat(0.25f + strikeIndex * 0.381966f, 1f);
            float x = Mathf.Lerp(
                bounds.min.x,
                bounds.max.x,
                Mathf.Lerp(0.08f, 0.92f, x01));
            float y = Mathf.Lerp(
                bounds.min.y,
                bounds.max.y,
                Mathf.Lerp(0.1f, 0.9f, y01));
            return new Vector3(x, y, 0f);
        }

        void CastElementBarrageStrike(
            Vector3 worldPoint,
            RelicBarrage barrage,
            MonsterAttribute attribute)
        {
            float baseDamage = _summonerAttack?.AttackDamage ?? 0f;
            var packet = new DamagePacket(
                this,
                _gameManager.ModifySummonerDamage(
                    baseDamage * barrage.DamageMultiplier),
                attribute);

            void ApplyImpact()
            {
                if (CanResolveRelicBarrage())
                    ApplyArea(worldPoint, barrage.Radius, packet);
            }

            Vector3 origin = _gameManager.Summoner != null
                ? _gameManager.Summoner.position + Vector3.up * 0.25f
                : transform.position;
            if (_particleEffects == null ||
                !_particleEffects.PlayRelicSkill(
                    barrage.SkillId,
                    origin,
                    worldPoint,
                    Mathf.Max(0.65f, barrage.Radius * 0.62f),
                    ApplyImpact))
                ApplyImpact();
        }

        void CastIceBarrageStrike(Vector3 worldPoint, RelicBarrage barrage)
        {
            float baseDamage = _summonerAttack?.AttackDamage ?? 0f;
            var packet = new DamagePacket(
                this,
                _gameManager.ModifySummonerDamage(
                    baseDamage * barrage.DamageMultiplier),
                barrage.Attribute,
                barrage.RankProfile.SlowPercent,
                barrage.RankProfile.SlowDuration *
                    barrage.StatusDurationMultiplier);
            ApplyArea(worldPoint, barrage.Radius, packet);
            _skillEffects?.PlayFrames(
                worldPoint,
                _iceWallFrames,
                Color.white,
                barrage.RankProfile.VisualScale,
                18f);
            _particleEffects?.PlayIceWall(
                worldPoint,
                Vector2.right,
                Mathf.Max(0.45f, barrage.Radius * 0.45f),
                barrage.RankProfile.VisualScale);
        }

        bool CanResolveRelicBarrage() =>
            _gameManager != null &&
            !_gameManager.IsRunOver &&
            !_gameManager.IsGameplayPaused &&
            _gameManager.Phase == RunPhase.InWave;

        bool CastMeteor(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            float damageMultiplier,
            float radiusMultiplier,
            float statusDurationMultiplier,
            SkillData skill,
            SkillRankProfile rankProfile)
        {
            return LaunchMeteor(
                worldPoint,
                definition.DamageMultiplier * damageMultiplier,
                definition.Radius * radiusMultiplier,
                rankProfile?.DamageOverTimeMultiplier ?? skill.DamageOverTime,
                (rankProfile?.DamageOverTimeDuration ?? skill.DamageOverTimeDuration) *
                    statusDurationMultiplier,
                rankProfile?.VisualScale ?? 1f,
                1.8f * radiusMultiplier,
                skill.Attribute);
        }

        public bool ResetCooldownAndAutoCastRelic(System.Random random)
        {
            if (_gameManager == null ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave)
                return false;

            _cooldowns[EquippedSkill] = 0f;
            SetPreviewVisible(false);
            StateChanged?.Invoke();

            BuildMonsterBuffer();
            if (_monsterBuffer.Count == 0)
                return false;

            int index = random?.Next(_monsterBuffer.Count) ?? 0;
            MonsterController target = _monsterBuffer[index];
            _targeting = true;
            _targetingExpiresAt = Time.unscaledTime + TargetingTimeout;
            return TryCastAt(target.transform.position);
        }

        bool LaunchMeteor(
            Vector3 worldPoint,
            float damageMultiplier,
            float radius,
            float dotMultiplier,
            float dotDuration,
            float visualScale,
            float impactScale,
            MonsterAttribute attribute)
        {
            float baseDamage = _summonerAttack?.AttackDamage ?? 0f;
            DamagePacket packet = new(
                this,
                _gameManager.ModifySummonerDamage(baseDamage * damageMultiplier),
                attribute,
                damageOverTime: _gameManager.ModifySummonerDamage(baseDamage * dotMultiplier),
                damageOverTimeDuration: dotDuration);
            CombatProjectileService projectiles = _gameManager.Projectiles;
            if (projectiles == null || _meteorProjectileSprite == null)
            {
                ApplyArea(worldPoint, radius, packet);
                PlayMeteorImpact(worldPoint, impactScale);
                return true;
            }

            projectiles.FireToPoint(
                worldPoint + MeteorLaunchOffset,
                worldPoint,
                _meteorProjectileSprite,
                packet,
                MeteorFallSpeed,
                visualScale,
                radius,
                hitAllInRadius: true,
                onImpact: point => PlayMeteorImpact(point, impactScale),
                tint: Color.white);
            return true;
        }

        void PlayMeteorImpact(Vector3 worldPoint, float scale) =>
            _skillEffects?.PlayFrames(
                worldPoint,
                _meteorFrames,
                Color.white,
                scale,
                18f);

        bool CastElementBurst(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            MonsterAttribute attribute,
            float damageMultiplier,
            float radiusMultiplier)
        {
            float baseDamage = _summonerAttack?.AttackDamage ?? 0f;
            float radius = definition.Radius * radiusMultiplier;
            var packet = new DamagePacket(
                this,
                _gameManager.ModifySummonerDamage(
                    baseDamage * definition.DamageMultiplier * damageMultiplier),
                attribute);

            void ApplyImpact()
            {
                if (_gameManager == null || _gameManager.IsRunOver ||
                    _gameManager.IsGameplayPaused ||
                    _gameManager.Phase != RunPhase.InWave)
                    return;
                ApplyArea(worldPoint, radius, packet);
            }

            Vector3 origin = _gameManager.Summoner != null
                ? _gameManager.Summoner.position + Vector3.up * 0.25f
                : transform.position;
            if (_particleEffects != null &&
                _particleEffects.PlayRelicSkill(
                    definition.Id,
                    origin,
                    worldPoint,
                    Mathf.Max(0.8f, radius * 0.65f),
                    ApplyImpact))
                return true;

            ApplyImpact();
            return true;
        }

        bool CastIceWall(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            float damageMultiplier,
            float radiusMultiplier,
            float statusDurationMultiplier,
            SkillRankProfile rankProfile)
        {
            if (_gameManager.Summoner == null)
                return false;
            Vector2 towardTarget = worldPoint - _gameManager.Summoner.position;
            if (towardTarget.sqrMagnitude <= 0.01f)
                return false;
            Vector2 wallAxis = new(-towardTarget.normalized.y, towardTarget.normalized.x);
            _iceWalls.Add(new IceWallZone
            {
                Center = worldPoint,
                Axis = wallAxis,
                ExpiresAt = Time.time + definition.Duration * statusDurationMultiplier,
                DamageMultiplier = definition.DamageMultiplier * damageMultiplier,
                Radius = definition.Radius * radiusMultiplier,
                Attribute = EquippedSkillData?.Attribute ?? MonsterAttribute.None,
                SlowPercent = rankProfile?.SlowPercent ?? 0f,
                SlowDuration = (rankProfile?.SlowDuration ?? 0f) *
                    statusDurationMultiplier,
            });
            float rotation = Mathf.Atan2(wallAxis.y, wallAxis.x) * Mathf.Rad2Deg - 90f;
            float animationDuration = (_iceWallFrames?.Length ?? 0) / 18f;
            _skillEffects?.PlayFrames(
                worldPoint,
                _iceWallFrames,
                Color.white,
                1.55f,
                18f,
                Mathf.Max(
                    0f,
                    definition.Duration * statusDurationMultiplier - animationDuration),
                rotation);
            _particleEffects?.PlayIceWall(
                worldPoint,
                wallAxis,
                IceWallHalfLength,
                Mathf.Max(0.8f, radiusMultiplier));
            TickIceWalls();
            return true;
        }

        bool CastAegis(
            SummonerSkillDefinition definition,
            SkillRankProfile rankProfile)
        {
            _gameManager.GrantCoreShield(
                _gameManager.MaxCoreHp * (rankProfile?.Strength ?? 0f),
                definition.Duration);
            _skillEffects?.Play(
                _gameManager.Summoner != null ? _gameManager.Summoner.position : transform.position,
                _aegisSprite,
                new Color(1f, 0.82f, 0.32f),
                1.45f);
            _particleEffects?.PlayBuff(
                SummonerBuffId.Aegis,
                _gameManager.Summoner != null
                    ? _gameManager.Summoner.position
                    : transform.position,
                1.1f);
            StartCooldown(definition);
            return true;
        }

        void TickIceWalls()
        {
            float baseDamage = _summonerAttack?.AttackDamage ?? 0f;
            BuildMonsterBuffer();
            for (int zoneIndex = _iceWalls.Count - 1; zoneIndex >= 0; zoneIndex--)
            {
                IceWallZone zone = _iceWalls[zoneIndex];
                if (Time.time >= zone.ExpiresAt)
                {
                    _iceWalls.RemoveAt(zoneIndex);
                    continue;
                }
                for (int i = 0; i < _monsterBuffer.Count; i++)
                {
                    MonsterController monster = _monsterBuffer[i];
                    if (zone.Hit.Contains(monster) ||
                        DistanceToWall(monster.transform.position, zone.Center, zone.Axis) > zone.Radius)
                        continue;
                    zone.Hit.Add(monster);
                    DamagePacket packet = new(
                        this,
                        _gameManager.ModifySummonerDamage(baseDamage * zone.DamageMultiplier),
                        zone.Attribute,
                        zone.SlowPercent,
                        zone.SlowDuration);
                    monster.ApplyDamage(packet);
                }
            }
        }

        void ApplyArea(Vector3 center, float radius, DamagePacket packet)
        {
            BuildMonsterBuffer();
            float radiusSq = radius * radius;
            for (int i = 0; i < _monsterBuffer.Count; i++)
            {
                MonsterController monster = _monsterBuffer[i];
                if ((monster.transform.position - center).sqrMagnitude <= radiusSq)
                    monster.ApplyDamage(packet);
            }
        }

        void BuildMonsterBuffer()
        {
            _monsterBuffer.Clear();
            IReadOnlyCollection<MonsterController> monsters =
                _gameManager.SummonedUnitManager?.Monsters;
            if (monsters == null)
                return;
            foreach (MonsterController monster in monsters)
                if (monster != null && !monster.IsResolved && monster.gameObject.activeInHierarchy)
                    _monsterBuffer.Add(monster);
        }

        static float DistanceToWall(Vector2 point, Vector2 center, Vector2 axis)
        {
            Vector2 offset = point - center;
            float along = Mathf.Clamp(Vector2.Dot(offset, axis), -IceWallHalfLength, IceWallHalfLength);
            return Vector2.Distance(point, center + axis * along);
        }

        void StartCooldown(SummonerSkillDefinition definition)
        {
            _cooldowns[definition.Id] = definition.Cooldown;
            StateChanged?.Invoke();
        }

        SummonerSkillDefinition BuildEquippedDefinition()
        {
            SkillData skill = EquippedSkillData;
            if (skill == null)
                return default;
            RelicDefinition relic = _relics?.EquippedDefinition;
            int rank = Mathf.Max(1, _relics?.EquippedRank ?? 1);
            RelicRankDefinition rankDefinition = relic?.Rank(rank);
            return skill.BuildDefinition(
                rank,
                rankDefinition?.SkillName,
                rankDefinition?.Description);
        }

        void OnRelicChanged()
        {
            CancelTargeting();
            StateChanged?.Invoke();
        }

        void BuildTargetPreview()
        {
            if (_targetPreview != null)
                return;
            GameObject preview = new("SummonerSkillTargetPreview");
            preview.transform.SetParent(transform, false);
            _targetPreview = preview.AddComponent<SpriteRenderer>();
            _targetPreview.sortingOrder = 8;
            _targetPreview.color = new Color(0.5f, 0.9f, 1f, 0.45f);
            preview.SetActive(false);
        }

        void SetPreviewVisible(bool visible)
        {
            if (_targetPreview != null)
                _targetPreview.gameObject.SetActive(visible);
        }

        static Sprite FirstFrame(Sprite[] frames) =>
            frames != null && frames.Length > 0 ? frames[0] : null;
    }
}
