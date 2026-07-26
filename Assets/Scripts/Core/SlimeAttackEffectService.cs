using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>
    /// 슬라임 종류와 성급에 따라 발사·근접 타격·명중·회복 연출을 만드는 풀링 기반 서비스.
    /// 별도 프리팹 없이 ParticleSystem과 LineRenderer를 런타임에 구성한다.
    /// </summary>
    public sealed class SlimeAttackEffectService
    {
        readonly Transform _root;
        readonly Transform _template;
        readonly Func<bool> _canPlay;
        static Material _sharedMaterial;

        public SlimeAttackEffectService(
            Transform parent,
            Func<bool> canPlay = null,
            string rootName = "SlimeAttackEffects")
        {
            var rootObject = new GameObject(rootName);
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            _canPlay = canPlay;
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseSlimeAttackEffect",
                ConfigureTemplate,
                32,
                256);
        }

        internal bool CanPlay => _canPlay?.Invoke() ?? true;

        public void PlayLaunch(
            string unitId,
            MonsterAttribute attribute,
            SummonAttackStyle attackStyle,
            int rank,
            Vector3 origin,
            Vector3 target)
        {
            if (!CanPlay)
                return;
            SlimeAttackEffectController effect = Spawn(origin);
            effect?.Play(
                this,
                SlimeAttackEffectPhase.Launch,
                ResolveKind(unitId, attackStyle),
                attribute,
                SummonRank.Clamp(rank),
                origin,
                target);
        }

        public void PlayMelee(
            string unitId,
            MonsterAttribute attribute,
            int rank,
            Vector3 origin,
            Vector3 target)
        {
            if (!CanPlay)
                return;
            SlimeAttackEffectController effect = Spawn(origin);
            effect?.Play(
                this,
                SlimeAttackEffectPhase.Melee,
                ResolveKind(unitId, SummonAttackStyle.Melee),
                attribute,
                SummonRank.Clamp(rank),
                origin,
                target);
        }

        public void PlayImpact(
            string unitId,
            MonsterAttribute attribute,
            SummonAttackStyle attackStyle,
            int rank,
            Vector3 position)
        {
            if (!CanPlay)
                return;
            SlimeAttackEffectController effect = Spawn(position);
            effect?.Play(
                this,
                SlimeAttackEffectPhase.Impact,
                ResolveKind(unitId, attackStyle),
                attribute,
                SummonRank.Clamp(rank),
                position,
                position);
        }

        public void PlaySupport(
            int rank,
            Vector3 origin,
            Vector3 target)
        {
            if (!CanPlay)
                return;
            SlimeAttackEffectController effect = Spawn(origin);
            effect?.Play(
                this,
                SlimeAttackEffectPhase.Support,
                SlimeAttackEffectKind.Buff,
                MonsterAttribute.None,
                SummonRank.Clamp(rank),
                origin,
                target);
        }

        internal void Release(SlimeAttackEffectController effect)
        {
            if (effect == null)
                return;
            effect.ResetForPool();
            RuntimePoolService.Despawn(effect.transform);
        }

        SlimeAttackEffectController Spawn(Vector3 position)
        {
            Transform spawned = RuntimePoolService.Spawn(
                _template,
                position,
                Quaternion.identity,
                _root);
            return spawned != null
                ? spawned.GetComponent<SlimeAttackEffectController>()
                : null;
        }

        static SlimeAttackEffectKind ResolveKind(
            string unitId,
            SummonAttackStyle attackStyle) =>
            unitId switch
            {
                "punch-slime" => SlimeAttackEffectKind.Punch,
                "watergun-slime" => SlimeAttackEffectKind.Water,
                "flame-slime" => SlimeAttackEffectKind.Flame,
                "ice-slime" => SlimeAttackEffectKind.Ice,
                "green-slime" => SlimeAttackEffectKind.Nature,
                "buff-slime" => SlimeAttackEffectKind.Buff,
                "explosion-slime" => SlimeAttackEffectKind.Explosion,
                "freeze-slime" => SlimeAttackEffectKind.Freeze,
                _ => attackStyle switch
                {
                    SummonAttackStyle.Melee => SlimeAttackEffectKind.Punch,
                    SummonAttackStyle.Area => SlimeAttackEffectKind.Explosion,
                    SummonAttackStyle.Support => SlimeAttackEffectKind.Buff,
                    SummonAttackStyle.Piercing => SlimeAttackEffectKind.Freeze,
                    _ => SlimeAttackEffectKind.Neutral,
                },
            };

        static void ConfigureTemplate(GameObject gameObject)
        {
            var particles = gameObject.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.stopAction = ParticleSystemStopAction.None;
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystemRenderer renderer =
                gameObject.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = SharedMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sortingOrder = 11;

            var line = gameObject.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.sharedMaterial = SharedMaterial();
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 3;
            line.numCornerVertices = 3;
            line.sortingOrder = 12;

            gameObject.AddComponent<SlimeAttackEffectController>();
        }

        static Material SharedMaterial()
        {
            if (_sharedMaterial != null)
                return _sharedMaterial;
            Shader shader = Shader.Find("Sprites/Default") ??
                            Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                return null;
            _sharedMaterial = new Material(shader)
            {
                name = "RuntimeSlimeAttackParticleMaterial",
                hideFlags = HideFlags.HideAndDontSave,
            };
            return _sharedMaterial;
        }
    }

    internal enum SlimeAttackEffectKind
    {
        Neutral,
        Punch,
        Water,
        Flame,
        Ice,
        Nature,
        Buff,
        Explosion,
        Freeze,
    }

    internal enum SlimeAttackEffectPhase
    {
        Launch,
        Melee,
        Impact,
        Support,
    }

    [DisallowMultipleComponent]
    public sealed class SlimeAttackEffectController : MonoBehaviour
    {
        static readonly Color NeutralColor = new(0.72f, 0.86f, 1f, 1f);
        static readonly Color PunchColor = new(0.88f, 0.94f, 1f, 1f);
        static readonly Color WaterColor = new(0.18f, 0.72f, 1f, 1f);
        static readonly Color FlameColor = new(1f, 0.32f, 0.08f, 1f);
        static readonly Color IceColor = new(0.45f, 0.88f, 1f, 1f);
        static readonly Color NatureColor = new(0.32f, 1f, 0.38f, 1f);
        static readonly Color LightningColor = new(0.76f, 0.58f, 1f, 1f);
        static readonly Color WindColor = new(0.68f, 1f, 0.86f, 1f);
        static readonly Color BuffColor = new(0.78f, 0.5f, 1f, 1f);
        static readonly Color ExplosionColor = new(1f, 0.55f, 0.08f, 1f);
        static readonly Color FreezeColor = new(0.62f, 0.72f, 1f, 1f);
        static readonly Color GoldColor = new(1f, 0.84f, 0.3f, 1f);

        ParticleSystem _particles;
        ParticleSystemRenderer _renderer;
        LineRenderer _line;
        SlimeAttackEffectService _service;
        float _elapsed;
        float _releaseAt;
        float _lineFadeAt;
        Color _lineStartColor;
        Color _lineEndColor;
        bool _playing;

        void Awake()
        {
            _particles = GetComponent<ParticleSystem>();
            _renderer = GetComponent<ParticleSystemRenderer>();
            _line = GetComponent<LineRenderer>();
        }

        void Update()
        {
            if (!_playing)
                return;
            if (_service == null || !_service.CanPlay)
            {
                _service?.Release(this);
                return;
            }

            _elapsed += Time.deltaTime;
            if (_line.enabled && _lineFadeAt > 0f)
            {
                float alpha = 1f - Mathf.Clamp01(_elapsed / _lineFadeAt);
                Color start = _lineStartColor;
                Color end = _lineEndColor;
                start.a *= alpha;
                end.a *= alpha;
                _line.startColor = start;
                _line.endColor = end;
            }
            if (_elapsed >= _releaseAt)
                _service.Release(this);
        }

        internal void Play(
            SlimeAttackEffectService service,
            SlimeAttackEffectPhase phase,
            SlimeAttackEffectKind kind,
            MonsterAttribute attribute,
            int rank,
            Vector3 origin,
            Vector3 target)
        {
            ResetForPool();
            _service = service;
            _playing = true;
            _releaseAt = phase == SlimeAttackEffectPhase.Support ? 1.1f : 0.85f;
            transform.position = origin;
            ConfigureBase();

            Color primary = KindColor(kind, attribute);
            Color secondary = SecondaryColor(kind);
            int clampedRank = SummonRank.Clamp(rank);
            switch (phase)
            {
                case SlimeAttackEffectPhase.Launch:
                    PlayLaunch(kind, clampedRank, origin, target, primary, secondary);
                    break;
                case SlimeAttackEffectPhase.Melee:
                    PlayMelee(clampedRank, origin, target, primary, secondary);
                    break;
                case SlimeAttackEffectPhase.Impact:
                    PlayImpact(kind, clampedRank, origin, primary, secondary);
                    break;
                case SlimeAttackEffectPhase.Support:
                    PlaySupport(clampedRank, origin, target);
                    break;
            }
            _particles.Play(true);
        }

        void ConfigureBase()
        {
            _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 0.7f;
            main.startSpeed = 0f;
            main.startSize = 0.1f;
            main.maxParticles = 220;
            main.gravityModifier = 0f;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = _particles.emission;
            emission.enabled = false;
            var shape = _particles.shape;
            shape.enabled = false;
            var velocity = _particles.velocityOverLifetime;
            velocity.enabled = false;
            var color = _particles.colorOverLifetime;
            color.enabled = true;
            color.color = new ParticleSystem.MinMaxGradient(FadeGradient());

            _renderer.renderMode = ParticleSystemRenderMode.Billboard;
            _renderer.alignment = ParticleSystemRenderSpace.View;
            _renderer.sortingOrder = 11;
            _line.enabled = false;
            _line.positionCount = 0;
        }

        void PlayLaunch(
            SlimeAttackEffectKind kind,
            int rank,
            Vector3 origin,
            Vector3 target,
            Color primary,
            Color secondary)
        {
            Vector2 direction = Direction(origin, target);
            Vector2 perpendicular = new(-direction.y, direction.x);
            int count = RankCount(6, rank);
            float speed = 0.8f + rank * 0.35f;
            float size = 0.055f + rank * 0.014f;

            switch (kind)
            {
                case SlimeAttackEffectKind.Water:
                    SetGravity(0.65f);
                    for (int i = 0; i < count + 3; i++)
                    {
                        Vector3 velocity = (Vector3)(
                            direction * UnityEngine.Random.Range(0.7f, 1.8f) +
                            perpendicular * UnityEngine.Random.Range(-0.7f, 0.7f));
                        Emit(origin, velocity, Alternate(primary, Color.white, i),
                            UnityEngine.Random.Range(size, size * 1.6f), 0.45f);
                    }
                    break;
                case SlimeAttackEffectKind.Flame:
                case SlimeAttackEffectKind.Explosion:
                    for (int i = 0; i < count + 4; i++)
                    {
                        Vector3 velocity = (Vector3)(
                            direction * UnityEngine.Random.Range(speed, speed + 1.2f) +
                            perpendicular * UnityEngine.Random.Range(-0.65f, 0.65f));
                        Emit(origin, velocity, Alternate(primary, secondary, i),
                            UnityEngine.Random.Range(size, size * 1.7f), 0.4f);
                    }
                    if (kind == SlimeAttackEffectKind.Explosion)
                        EmitRing(origin, primary, RankCount(7, rank), 0.2f, rank, 0.35f);
                    break;
                case SlimeAttackEffectKind.Ice:
                case SlimeAttackEffectKind.Freeze:
                    for (int i = 0; i < count + 2; i++)
                    {
                        Vector3 position = origin +
                            (Vector3)(perpendicular * UnityEngine.Random.Range(-0.12f, 0.12f));
                        Vector3 velocity = (Vector3)(
                            direction * UnityEngine.Random.Range(1.1f, 2.5f) +
                            perpendicular * UnityEngine.Random.Range(-0.4f, 0.4f));
                        Emit(position, velocity, Alternate(primary, Color.white, i),
                            UnityEngine.Random.Range(size * 0.8f, size * 1.4f), 0.5f);
                    }
                    if (kind == SlimeAttackEffectKind.Freeze || rank >= 2)
                        ConfigureAttackLine(origin, origin + (Vector3)direction * (0.65f + rank * 0.18f),
                            primary, Color.white, 0.065f + rank * 0.018f, 0.24f);
                    break;
                case SlimeAttackEffectKind.Nature:
                    SetGravity(-0.12f);
                    for (int i = 0; i < count + 2; i++)
                    {
                        Vector3 velocity = (Vector3)(
                            direction * UnityEngine.Random.Range(0.6f, 1.5f) +
                            perpendicular * UnityEngine.Random.Range(-0.8f, 0.8f));
                        Emit(origin, velocity, Alternate(primary, secondary, i),
                            UnityEngine.Random.Range(size, size * 1.5f), 0.58f);
                    }
                    break;
                default:
                    EmitCone(origin, direction, perpendicular, primary, secondary, count, speed, size);
                    break;
            }

            AddRankAccent(origin, primary, secondary, rank, 0.24f);
        }

        void PlayMelee(
            int rank,
            Vector3 origin,
            Vector3 target,
            Color primary,
            Color secondary)
        {
            Vector2 direction = Direction(origin, target);
            Vector2 perpendicular = new(-direction.y, direction.x);
            float distance = Mathf.Min(1.15f, Vector3.Distance(origin, target));
            Vector3 end = origin + (Vector3)direction * distance;
            Vector3 middle = Vector3.Lerp(origin, end, 0.55f) +
                             (Vector3)perpendicular * (0.22f + rank * 0.05f);
            ConfigureLine(
                new[] { origin, middle, end },
                primary,
                rank >= 2 ? Color.white : secondary,
                0.075f + rank * 0.028f,
                0.22f);

            int count = RankCount(8, rank);
            for (int i = 0; i < count; i++)
            {
                float t = UnityEngine.Random.Range(0.15f, 1f);
                Vector3 point = Vector3.Lerp(origin, end, t) +
                                (Vector3)perpendicular * Mathf.Sin(t * Mathf.PI) * 0.16f;
                Emit(
                    point,
                    (Vector3)(direction * UnityEngine.Random.Range(1f, 2.7f) +
                              perpendicular * UnityEngine.Random.Range(-0.8f, 0.8f)),
                    Alternate(primary, secondary, i),
                    UnityEngine.Random.Range(0.055f, 0.11f + rank * 0.015f),
                    0.38f);
            }
            EmitRadial(end, primary, RankCount(7, rank), 0.8f, 2.5f, 0.07f + rank * 0.015f, 0.42f);
            AddRankAccent(end, primary, secondary, rank, 0.34f);
        }

        void PlayImpact(
            SlimeAttackEffectKind kind,
            int rank,
            Vector3 position,
            Color primary,
            Color secondary)
        {
            int count = RankCount(kind == SlimeAttackEffectKind.Explosion ? 18 : 10, rank);
            float minSpeed = kind == SlimeAttackEffectKind.Explosion ? 1.7f : 0.9f;
            float maxSpeed = kind == SlimeAttackEffectKind.Explosion ? 4.6f : 3f;
            float size = 0.07f + rank * 0.018f;

            switch (kind)
            {
                case SlimeAttackEffectKind.Water:
                    SetGravity(1.3f);
                    for (int i = 0; i < count + 5; i++)
                    {
                        Vector2 horizontal = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 velocity = new(
                            horizontal.x * UnityEngine.Random.Range(0.7f, 2.4f),
                            UnityEngine.Random.Range(1.1f, 3.4f),
                            0f);
                        Emit(position, velocity, Alternate(primary, Color.white, i),
                            UnityEngine.Random.Range(size * 0.75f, size * 1.35f), 0.6f);
                    }
                    break;
                case SlimeAttackEffectKind.Flame:
                    SetGravity(-0.3f);
                    EmitRadial(position, primary, count, minSpeed, maxSpeed, size, 0.55f);
                    EmitUpward(position, secondary, RankCount(6, rank), 1.2f, 3.4f, size, 0.62f);
                    break;
                case SlimeAttackEffectKind.Ice:
                case SlimeAttackEffectKind.Freeze:
                    SetGravity(0.45f);
                    EmitShards(position, primary, count + 4, rank, kind == SlimeAttackEffectKind.Freeze);
                    break;
                case SlimeAttackEffectKind.Nature:
                    SetGravity(-0.18f);
                    EmitRadial(position, primary, count, 0.5f, 2.2f, size, 0.75f);
                    EmitUpward(position, secondary, RankCount(5, rank), 0.6f, 1.8f, size * 0.8f, 0.8f);
                    break;
                case SlimeAttackEffectKind.Explosion:
                    EmitRadial(position, primary, count, minSpeed, maxSpeed, size * 1.2f, 0.58f);
                    EmitRadial(position, secondary, RankCount(8, rank), 0.8f, 2.4f, size, 0.48f);
                    EmitRing(position, primary, RankCount(12, rank), 0.4f + rank * 0.1f, rank, 0.55f);
                    break;
                default:
                    EmitRadial(position, primary, count, minSpeed, maxSpeed, size, 0.5f);
                    break;
            }

            AddRankAccent(position, primary, secondary, rank, 0.42f);
        }

        void PlaySupport(int rank, Vector3 origin, Vector3 target)
        {
            Vector2 direction = Direction(origin, target);
            Vector2 perpendicular = new(-direction.y, direction.x);
            Vector3 middle = Vector3.Lerp(origin, target, 0.5f) +
                             (Vector3)perpendicular * (0.12f + rank * 0.04f);
            ConfigureLine(
                new[] { origin, middle, target },
                rank >= 2 ? Color.white : BuffColor,
                GoldColor,
                0.045f + rank * 0.018f,
                0.48f);

            int streamCount = RankCount(10, rank);
            for (int i = 0; i < streamCount; i++)
            {
                float t = streamCount <= 1 ? 1f : i / (float)(streamCount - 1);
                Vector3 point = Vector3.Lerp(origin, target, t);
                point += (Vector3)perpendicular * Mathf.Sin(t * Mathf.PI) * 0.12f;
                Emit(
                    point,
                    Vector3.up * UnityEngine.Random.Range(0.25f, 0.7f),
                    Alternate(BuffColor, GoldColor, i),
                    UnityEngine.Random.Range(0.055f, 0.1f + rank * 0.012f),
                    0.7f);
            }
            EmitUpward(target, new Color(0.3f, 1f, 0.48f), RankCount(10, rank),
                0.65f, 1.8f, 0.075f + rank * 0.012f, 0.78f);
            EmitRing(target, GoldColor, RankCount(8, rank), 0.26f + rank * 0.05f, rank, 0.7f);
            AddRankAccent(origin, BuffColor, GoldColor, rank, 0.28f);
        }

        void AddRankAccent(
            Vector3 position,
            Color primary,
            Color secondary,
            int rank,
            float radius)
        {
            if (rank >= 1)
                EmitRing(position, secondary, 8 + rank * 3, radius, rank, 0.46f);
            if (rank >= 2)
            {
                EmitRing(position, Color.white, 14, radius * 1.45f, rank, 0.58f);
                EmitRadial(position, Color.white, 7, 0.6f, 2f, 0.055f, 0.38f);
            }
        }

        void EmitCone(
            Vector3 origin,
            Vector2 direction,
            Vector2 perpendicular,
            Color primary,
            Color secondary,
            int count,
            float speed,
            float size)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 velocity = (Vector3)(
                    direction * UnityEngine.Random.Range(speed, speed + 1.3f) +
                    perpendicular * UnityEngine.Random.Range(-0.65f, 0.65f));
                Emit(origin, velocity, Alternate(primary, secondary, i),
                    UnityEngine.Random.Range(size, size * 1.45f), 0.4f);
            }
        }

        void EmitShards(
            Vector3 center,
            Color color,
            int count,
            int rank,
            bool piercing)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                if (direction.sqrMagnitude <= 0.001f)
                    direction = Vector2.up;
                float speed = UnityEngine.Random.Range(
                    piercing ? 1.6f : 0.9f,
                    piercing ? 4f : 3f);
                Emit(
                    center,
                    (Vector3)(direction * speed) + Vector3.up * UnityEngine.Random.Range(0.2f, 1f),
                    Alternate(color, Color.white, i),
                    UnityEngine.Random.Range(0.05f, 0.1f + rank * 0.018f),
                    UnityEngine.Random.Range(0.45f, 0.72f));
            }
        }

        void EmitUpward(
            Vector3 center,
            Color color,
            int count,
            float minSpeed,
            float maxSpeed,
            float size,
            float lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 position = center + (Vector3)UnityEngine.Random.insideUnitCircle * 0.12f;
                Vector3 velocity = new(
                    UnityEngine.Random.Range(-0.65f, 0.65f),
                    UnityEngine.Random.Range(minSpeed, maxSpeed),
                    0f);
                Emit(position, velocity, Alternate(color, Color.white, i),
                    UnityEngine.Random.Range(size * 0.75f, size * 1.25f), lifetime);
            }
        }

        void EmitRadial(
            Vector3 center,
            Color color,
            int count,
            float minSpeed,
            float maxSpeed,
            float size,
            float lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
                if (direction.sqrMagnitude <= 0.001f)
                    direction = Vector2.up;
                Emit(
                    center,
                    (Vector3)(direction * UnityEngine.Random.Range(minSpeed, maxSpeed)),
                    Alternate(color, Color.white, i),
                    UnityEngine.Random.Range(size * 0.75f, size * 1.25f),
                    UnityEngine.Random.Range(lifetime * 0.78f, lifetime * 1.15f));
            }
        }

        void EmitRing(
            Vector3 center,
            Color color,
            int count,
            float radius,
            int rank,
            float lifetime)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)Mathf.Max(1, count) * Mathf.PI * 2f;
                Vector2 radial = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Emit(
                    center + (Vector3)(radial * radius),
                    (Vector3)(radial * (0.35f + rank * 0.18f)),
                    Alternate(color, Color.white, i),
                    0.045f + rank * 0.012f,
                    lifetime);
            }
        }

        void ConfigureAttackLine(
            Vector3 origin,
            Vector3 target,
            Color start,
            Color end,
            float width,
            float fadeAt)
        {
            Vector3 middle = Vector3.Lerp(origin, target, 0.5f);
            ConfigureLine(new[] { origin, middle, target }, start, end, width, fadeAt);
        }

        void ConfigureLine(
            Vector3[] points,
            Color start,
            Color end,
            float width,
            float fadeAt)
        {
            _line.enabled = true;
            _line.positionCount = points.Length;
            _line.SetPositions(points);
            _line.startWidth = width;
            _line.endWidth = width * 0.35f;
            _lineStartColor = start;
            _lineEndColor = end;
            _line.startColor = start;
            _line.endColor = end;
            _lineFadeAt = fadeAt;
        }

        void Emit(
            Vector3 position,
            Vector3 velocity,
            Color color,
            float size,
            float lifetime)
        {
            var parameters = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startColor = color,
                startSize = Mathf.Max(0.02f, size),
                startLifetime = Mathf.Max(0.05f, lifetime),
            };
            _particles.Emit(parameters, 1);
        }

        void SetGravity(float gravity)
        {
            var main = _particles.main;
            main.gravityModifier = gravity;
        }

        static int RankCount(int baseCount, int rank) =>
            Mathf.Max(1, Mathf.RoundToInt(baseCount * (1f + SummonRank.Clamp(rank) * 0.55f)));

        static Vector2 Direction(Vector3 origin, Vector3 target)
        {
            Vector2 direction = target - origin;
            return direction.sqrMagnitude <= 0.001f ? Vector2.right : direction.normalized;
        }

        static Color KindColor(
            SlimeAttackEffectKind kind,
            MonsterAttribute attribute) =>
            kind switch
            {
                SlimeAttackEffectKind.Punch => PunchColor,
                SlimeAttackEffectKind.Water => WaterColor,
                SlimeAttackEffectKind.Flame => FlameColor,
                SlimeAttackEffectKind.Ice => IceColor,
                SlimeAttackEffectKind.Nature => NatureColor,
                SlimeAttackEffectKind.Buff => BuffColor,
                SlimeAttackEffectKind.Explosion => ExplosionColor,
                SlimeAttackEffectKind.Freeze => FreezeColor,
                _ => attribute switch
                {
                    MonsterAttribute.Fire => FlameColor,
                    MonsterAttribute.Ice => IceColor,
                    MonsterAttribute.Nature => NatureColor,
                    MonsterAttribute.Lightning => LightningColor,
                    MonsterAttribute.Water => WaterColor,
                    MonsterAttribute.Wind => WindColor,
                    _ => NeutralColor,
                },
            };

        static Color SecondaryColor(SlimeAttackEffectKind kind) =>
            kind switch
            {
                SlimeAttackEffectKind.Flame => new Color(1f, 0.86f, 0.18f, 1f),
                SlimeAttackEffectKind.Explosion => new Color(1f, 0.88f, 0.24f, 1f),
                SlimeAttackEffectKind.Nature => new Color(0.8f, 1f, 0.25f, 1f),
                SlimeAttackEffectKind.Buff => GoldColor,
                SlimeAttackEffectKind.Water => new Color(0.72f, 0.95f, 1f, 1f),
                SlimeAttackEffectKind.Ice => new Color(0.86f, 0.98f, 1f, 1f),
                SlimeAttackEffectKind.Freeze => new Color(0.9f, 0.92f, 1f, 1f),
                _ => Color.white,
            };

        static Color Alternate(Color primary, Color secondary, int index) =>
            index % 3 == 0 ? secondary : primary;

        static Gradient FadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f),
                },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(0.82f, 0.7f),
                    new GradientAlphaKey(0f, 1f),
                });
            return gradient;
        }

        internal void ResetForPool()
        {
            _playing = false;
            _elapsed = 0f;
            _releaseAt = 0f;
            _lineFadeAt = 0f;
            _lineStartColor = Color.white;
            _lineEndColor = Color.white;
            _service = null;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (_particles != null)
                _particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (_line != null)
            {
                _line.enabled = false;
                _line.positionCount = 0;
            }
        }
    }
}
