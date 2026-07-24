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
        const float MeteorVisualScale = 1.15f;
        static readonly Vector3 MeteorLaunchOffset = new(3.2f, 4.8f, 0f);

        sealed class IceWallZone
        {
            public Vector2 Center;
            public Vector2 Axis;
            public float ExpiresAt;
            public float DamageMultiplier;
            public float Radius;
            public float SlowDuration;
            public readonly HashSet<MonsterController> Hit = new();
        }

        readonly Dictionary<SummonerSkillId, float> _cooldowns = new();
        readonly List<IceWallZone> _iceWalls = new();
        readonly List<MonsterController> _monsterBuffer = new(64);
        readonly List<SummonerSkillId> _cooldownKeys = new(3);

        GameManager _gameManager;
        SummonerAttackController _summonerAttack;
        SummonerSkillLoadout _loadout;
        CombatEffectService _skillEffects;
        Sprite _meteorProjectileSprite;
        Sprite[] _meteorFrames;
        Sprite[] _iceWallFrames;
        Sprite _aegisSprite;
        SpriteRenderer _targetPreview;
        bool _targeting;
        float _targetingExpiresAt;
        int _lastOverdriveTargetInstanceId;

        public SummonerSkillLoadout Loadout => _loadout;
        public SummonerSkillId EquippedSkill =>
            _loadout?.EquippedSkill ?? SummonerSkillId.Meteor;
        public SummonerSkillDefinition EquippedDefinition =>
            SummonerSkillCatalog.Get(EquippedSkill);
        public bool IsTargeting => _targeting;
        public float RemainingCooldown =>
            _cooldowns.TryGetValue(EquippedSkill, out float value) ? Mathf.Max(0f, value) : 0f;
        public bool IsReady => RemainingCooldown <= 0f;

        public event Action StateChanged;

        public void Initialize(
            GameManager gameManager,
            SummonerAttackController summonerAttack,
            SummonerSkillLoadout loadout,
            Sprite meteorProjectileSprite,
            Sprite[] meteorFrames,
            Sprite[] iceWallFrames,
            Sprite aegisSprite)
        {
            _gameManager = gameManager;
            _summonerAttack = summonerAttack;
            if (_loadout != null)
                _loadout.Changed -= OnEquippedSkillChanged;
            _loadout = loadout;
            if (_loadout != null)
                _loadout.Changed += OnEquippedSkillChanged;
            _skillEffects ??= new CombatEffectService(
                transform,
                rootName: "SummonerSkillEffects");
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
                if (changed)
                    StateChanged?.Invoke();
            }

            if (_targeting && Time.unscaledTime >= _targetingExpiresAt)
                CancelTargeting();
        }

        void OnDestroy()
        {
            if (_loadout != null)
                _loadout.Changed -= OnEquippedSkillChanged;
        }

        public bool PressSkillButton()
        {
            if (_gameManager == null || _gameManager.IsRunOver ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave ||
                !IsReady)
                return false;

            SummonerSkillDefinition definition = EquippedDefinition;
            if (definition.Targeting == SummonerSkillTargeting.Instant)
                return CastAegis(definition);
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
            float damageMultiplier = 1f;
            float radiusMultiplier = 1f;
            float statusDurationMultiplier = 1f;
            bool amplified = _gameManager.SummonerBuffs != null &&
                _gameManager.SummonerBuffs.GetRelicAmplification(
                    out damageMultiplier,
                    out radiusMultiplier,
                    out statusDurationMultiplier);
            bool cast = definition.Id switch
            {
                SummonerSkillId.Meteor => CastMeteor(
                    worldPoint,
                    definition,
                    damageMultiplier,
                    radiusMultiplier,
                    statusDurationMultiplier),
                SummonerSkillId.IceWall => CastIceWall(
                    worldPoint,
                    definition,
                    damageMultiplier,
                    radiusMultiplier,
                    statusDurationMultiplier),
                _ => false,
            };
            if (!cast)
                return false;
            if (amplified)
                _gameManager.SummonerBuffs.ConsumeRelicAmplification();
            _targeting = false;
            SetPreviewVisible(false);
            StartCooldown(definition);
            return true;
        }

        public void UpdateTargetPreview(Vector3 worldPoint)
        {
            if (!_targeting || _targetPreview == null)
                return;
            SummonerSkillDefinition definition = EquippedDefinition;
            _targetPreview.transform.position = worldPoint;
            _targetPreview.sprite = definition.Id == SummonerSkillId.IceWall
                ? FirstFrame(_iceWallFrames)
                : FirstFrame(_meteorFrames);
            _targetPreview.transform.localScale = Vector3.one *
                (definition.Id == SummonerSkillId.IceWall ? 1.2f : definition.Radius * 0.72f);
            if (definition.Id == SummonerSkillId.IceWall && _gameManager.Summoner != null)
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

        bool CastMeteor(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            float damageMultiplier,
            float radiusMultiplier,
            float statusDurationMultiplier)
        {
            return LaunchMeteor(
                worldPoint,
                definition.DamageMultiplier * damageMultiplier,
                definition.Radius * radiusMultiplier,
                0.3f,
                3f * statusDurationMultiplier,
                MeteorVisualScale,
                1.8f * radiusMultiplier);
        }

        public bool TryCastOverdriveMeteor(
            DopamineBalanceData balance,
            System.Random random)
        {
            if (balance == null || _gameManager == null ||
                _gameManager.IsGameplayPaused || _gameManager.Phase != RunPhase.InWave)
                return false;

            BuildMonsterBuffer();
            if (_monsterBuffer.Count == 0)
                return false;

            int index = random?.Next(_monsterBuffer.Count) ?? 0;
            if (_monsterBuffer.Count > 1 &&
                _monsterBuffer[index].GetInstanceID() == _lastOverdriveTargetInstanceId)
            {
                int offset = 1 + (random?.Next(_monsterBuffer.Count - 1) ?? 0);
                index = (index + offset) % _monsterBuffer.Count;
            }

            MonsterController target = _monsterBuffer[index];
            _lastOverdriveTargetInstanceId = target.GetInstanceID();
            double angle = (random?.NextDouble() ?? 0d) * Math.PI * 2d;
            double distance = Math.Sqrt(random?.NextDouble() ?? 0d) * balance.MeteorTargetJitter;
            Vector3 jitter = new(
                (float)(Math.Cos(angle) * distance),
                (float)(Math.Sin(angle) * distance),
                0f);
            return LaunchMeteor(
                target.transform.position + jitter,
                balance.MeteorDamageMultiplier,
                balance.MeteorRadius,
                0f,
                0f,
                0.9f,
                1.35f);
        }

        bool LaunchMeteor(
            Vector3 worldPoint,
            float damageMultiplier,
            float radius,
            float dotMultiplier,
            float dotDuration,
            float visualScale,
            float impactScale)
        {
            float baseDamage = _summonerAttack != null ? _summonerAttack.AttackDamage : 12f;
            DamagePacket packet = new(
                this,
                _gameManager.ModifySummonerDamage(baseDamage * damageMultiplier),
                MonsterAttribute.Fire,
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

        bool CastIceWall(
            Vector3 worldPoint,
            SummonerSkillDefinition definition,
            float damageMultiplier,
            float radiusMultiplier,
            float statusDurationMultiplier)
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
                SlowDuration = 2.5f * statusDurationMultiplier,
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
            TickIceWalls();
            return true;
        }

        bool CastAegis(SummonerSkillDefinition definition)
        {
            _gameManager.GrantCoreShield(_gameManager.MaxCoreHp * 0.35f, definition.Duration);
            _skillEffects?.Play(
                _gameManager.Summoner != null ? _gameManager.Summoner.position : transform.position,
                _aegisSprite,
                new Color(1f, 0.82f, 0.32f),
                1.45f);
            StartCooldown(definition);
            return true;
        }

        void TickIceWalls()
        {
            float baseDamage = _summonerAttack != null ? _summonerAttack.AttackDamage : 12f;
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
                        MonsterAttribute.Ice,
                        0.6f,
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

        void OnEquippedSkillChanged(SummonerSkillId _)
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
