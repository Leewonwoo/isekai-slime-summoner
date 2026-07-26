using System;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>
    /// 신물 공격과 소환사 버프에 공용으로 사용하는 2D 월드 파티클 연출 서비스.
    /// 한 개의 PoolBoss 템플릿을 재사용하고, 스킬별 이동·낙하·회전 패턴만 코드로 구성한다.
    /// </summary>
    public sealed class SkillParticleEffectService
    {
        readonly Transform _root;
        readonly Transform _template;
        readonly Func<bool> _canPlay;
        static Material _sharedMaterial;

        public SkillParticleEffectService(
            Transform parent,
            Func<bool> canPlay = null,
            string rootName = "SkillParticleEffects")
        {
            var rootObject = new GameObject(rootName);
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            _canPlay = canPlay;
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseSkillParticleEffect",
                ConfigureTemplate,
                20,
                160);
        }

        internal bool CanPlay => _canPlay?.Invoke() ?? true;

        public bool PlayRelicSkill(
            SummonerSkillId skillId,
            Vector3 origin,
            Vector3 target,
            float scale,
            Action onImpact)
        {
            SkillParticlePreset preset = skillId switch
            {
                SummonerSkillId.ArcaneBurst => SkillParticlePreset.ArcaneProjectile,
                SummonerSkillId.LightningStrike => SkillParticlePreset.LightningStrike,
                SummonerSkillId.WaterBurst => SkillParticlePreset.WaterProjectile,
                SummonerSkillId.Gale => SkillParticlePreset.GaleVortex,
                _ => SkillParticlePreset.None,
            };
            if (preset == SkillParticlePreset.None || !CanPlay)
                return false;

            SkillParticleEffectController effect = Spawn(target);
            if (effect == null)
                return false;
            effect.PlayTargeted(this, preset, origin, target, Mathf.Max(0.5f, scale), onImpact);
            return true;
        }

        public void PlayIceWall(
            Vector3 center,
            Vector2 wallAxis,
            float halfLength,
            float scale)
        {
            if (!CanPlay)
                return;
            SkillParticleEffectController effect = Spawn(center);
            effect?.PlayIceWall(
                this,
                center,
                wallAxis,
                Mathf.Max(0.25f, halfLength),
                Mathf.Max(0.5f, scale));
        }

        public void PlayBuff(
            SummonerBuffId buffId,
            Vector3 position,
            float scale = 1f)
        {
            if (!CanPlay)
                return;
            SkillParticleEffectController effect = Spawn(position);
            effect?.PlayBuff(this, buffId, position, Mathf.Max(0.5f, scale));
        }

        internal void Release(SkillParticleEffectController effect)
        {
            if (effect == null)
                return;
            effect.ResetForPool();
            RuntimePoolService.Despawn(effect.transform);
        }

        SkillParticleEffectController Spawn(Vector3 position)
        {
            Transform spawned = RuntimePoolService.Spawn(
                _template,
                position,
                Quaternion.identity,
                _root);
            return spawned != null
                ? spawned.GetComponent<SkillParticleEffectController>()
                : null;
        }

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
            renderer.sortingOrder = 12;

            var line = gameObject.AddComponent<LineRenderer>();
            line.enabled = false;
            line.useWorldSpace = true;
            line.sharedMaterial = SharedMaterial();
            line.textureMode = LineTextureMode.Stretch;
            line.numCapVertices = 2;
            line.numCornerVertices = 2;
            line.sortingOrder = 13;

            gameObject.AddComponent<SkillParticleEffectController>();
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
                name = "RuntimeSkillParticleMaterial",
                hideFlags = HideFlags.HideAndDontSave,
            };
            return _sharedMaterial;
        }
    }

    internal enum SkillParticlePreset
    {
        None,
        ArcaneProjectile,
        LightningStrike,
        WaterProjectile,
        GaleVortex,
        IceWall,
        Aegis,
        LifeBlessing,
        LegionCommand,
        ElementalResonance,
        TimeAcceleration,
    }

    [DisallowMultipleComponent]
    public sealed class SkillParticleEffectController : MonoBehaviour
    {
        ParticleSystem _particles;
        ParticleSystemRenderer _renderer;
        LineRenderer _line;
        SkillParticleEffectService _service;
        SkillParticlePreset _preset;
        Vector3 _origin;
        Vector3 _target;
        Action _onImpact;
        float _scale;
        float _elapsed;
        float _impactAt;
        float _releaseAt;
        float _emissionAccumulator;
        bool _playing;
        bool _impacted;

        static readonly Color ArcaneColor = new(0.72f, 0.42f, 1f, 1f);
        static readonly Color LightningColor = new(1f, 0.92f, 0.3f, 1f);
        static readonly Color WaterColor = new(0.2f, 0.72f, 1f, 1f);
        static readonly Color GaleColor = new(0.45f, 1f, 0.72f, 1f);
        static readonly Color IceColor = new(0.58f, 0.9f, 1f, 1f);
        static readonly Color AegisColor = new(1f, 0.78f, 0.22f, 1f);
        static readonly Color LifeColor = new(0.3f, 1f, 0.48f, 1f);
        static readonly Color CommandColor = new(1f, 0.42f, 0.18f, 1f);
        static readonly Color ResonanceColor = new(0.72f, 0.48f, 1f, 1f);
        static readonly Color TimeColor = new(0.35f, 0.9f, 1f, 1f);

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

            float deltaTime = Time.deltaTime;
            _elapsed += deltaTime;
            switch (_preset)
            {
                case SkillParticlePreset.ArcaneProjectile:
                case SkillParticlePreset.WaterProjectile:
                    TickProjectile(deltaTime);
                    break;
                case SkillParticlePreset.LightningStrike:
                    TickLightning();
                    break;
                case SkillParticlePreset.GaleVortex:
                    TickGale(deltaTime);
                    break;
            }

            if (!_impacted && _elapsed >= _impactAt)
                Impact();
            if (_elapsed >= _releaseAt)
                _service.Release(this);
        }

        internal void PlayTargeted(
            SkillParticleEffectService service,
            SkillParticlePreset preset,
            Vector3 origin,
            Vector3 target,
            float scale,
            Action onImpact)
        {
            ResetForPool();
            _service = service;
            _preset = preset;
            _origin = origin;
            _target = target;
            _scale = scale;
            _onImpact = onImpact;
            _playing = true;
            transform.position = preset == SkillParticlePreset.LightningStrike ||
                                 preset == SkillParticlePreset.GaleVortex
                ? target
                : origin;
            ConfigureBase();

            switch (preset)
            {
                case SkillParticlePreset.ArcaneProjectile:
                    _impactAt = 0.32f;
                    _releaseAt = 1.15f;
                    EmitRadial(origin, ArcaneColor, 8, 0.3f, 0.9f, 0.14f, 0.4f);
                    break;
                case SkillParticlePreset.WaterProjectile:
                    _impactAt = 0.38f;
                    _releaseAt = 1.35f;
                    SetGravity(0.15f);
                    EmitRadial(origin, WaterColor, 7, 0.2f, 0.7f, 0.12f, 0.35f);
                    break;
                case SkillParticlePreset.LightningStrike:
                    _impactAt = 0.2f;
                    _releaseAt = 0.85f;
                    ConfigureLightningLine();
                    EmitLightningDescent();
                    break;
                case SkillParticlePreset.GaleVortex:
                    _impactAt = 0.34f;
                    _releaseAt = 1.25f;
                    EmitVortexRing(18);
                    break;
            }
            _particles.Play(true);
        }

        internal void PlayIceWall(
            SkillParticleEffectService service,
            Vector3 center,
            Vector2 wallAxis,
            float halfLength,
            float scale)
        {
            ResetForPool();
            _service = service;
            _preset = SkillParticlePreset.IceWall;
            _scale = scale;
            _playing = true;
            _impacted = true;
            _impactAt = 0f;
            _releaseAt = 1.2f;
            transform.position = center;
            ConfigureBase();
            SetGravity(0.55f);

            Vector2 perpendicular = new(-wallAxis.y, wallAxis.x);
            const int count = 26;
            for (int i = 0; i < count; i++)
            {
                float t = count <= 1 ? 0f : i / (float)(count - 1);
                Vector3 position = center + (Vector3)(wallAxis * Mathf.Lerp(-halfLength, halfLength, t));
                position += (Vector3)(perpendicular * UnityEngine.Random.Range(-0.12f, 0.12f));
                Vector3 velocity = Vector3.up * UnityEngine.Random.Range(1.5f, 3.4f) +
                                   (Vector3)(perpendicular * UnityEngine.Random.Range(-0.8f, 0.8f));
                Emit(position, velocity, Alternate(IceColor, Color.white, i), 0.1f * scale, 0.65f);
            }
            _particles.Play(true);
        }

        internal void PlayBuff(
            SkillParticleEffectService service,
            SummonerBuffId buffId,
            Vector3 position,
            float scale)
        {
            ResetForPool();
            _service = service;
            _preset = buffId switch
            {
                SummonerBuffId.Aegis => SkillParticlePreset.Aegis,
                SummonerBuffId.LifeBlessing => SkillParticlePreset.LifeBlessing,
                SummonerBuffId.LegionCommand => SkillParticlePreset.LegionCommand,
                SummonerBuffId.ElementalResonance => SkillParticlePreset.ElementalResonance,
                SummonerBuffId.TimeAcceleration => SkillParticlePreset.TimeAcceleration,
                _ => SkillParticlePreset.None,
            };
            _scale = scale;
            _playing = _preset != SkillParticlePreset.None;
            _impacted = true;
            _impactAt = 0f;
            _releaseAt = 1.15f;
            transform.position = position;
            ConfigureBase();

            switch (_preset)
            {
                case SkillParticlePreset.Aegis:
                    EmitOrbitRing(position, AegisColor, 24, 0.58f * scale, 1.2f);
                    EmitRadial(position, Color.white, 10, 0.25f, 1.1f, 0.09f, 0.45f);
                    break;
                case SkillParticlePreset.LifeBlessing:
                    for (int i = 0; i < 18; i++)
                    {
                        Vector3 offset = new(
                            UnityEngine.Random.Range(-0.35f, 0.35f) * scale,
                            UnityEngine.Random.Range(-0.15f, 0.2f) * scale,
                            0f);
                        Emit(
                            position + offset,
                            Vector3.up * UnityEngine.Random.Range(0.8f, 1.8f),
                            Alternate(LifeColor, Color.white, i),
                            UnityEngine.Random.Range(0.07f, 0.13f) * scale,
                            UnityEngine.Random.Range(0.55f, 0.95f));
                    }
                    break;
                case SkillParticlePreset.LegionCommand:
                    EmitRadial(position, CommandColor, 24, 1.4f, 3f, 0.09f * scale, 0.65f);
                    EmitOrbitRing(position, AegisColor, 12, 0.42f * scale, 0.8f);
                    break;
                case SkillParticlePreset.ElementalResonance:
                    EmitOrbitRing(position, ResonanceColor, 30, 0.62f * scale, 1.4f);
                    EmitRadial(position, TimeColor, 12, 0.4f, 1.1f, 0.08f * scale, 0.7f);
                    break;
                case SkillParticlePreset.TimeAcceleration:
                    EmitOrbitRing(position, TimeColor, 28, 0.72f * scale, 1.8f);
                    EmitOrbitRing(position, Color.white, 14, 0.42f * scale, -1.2f);
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
            main.startLifetime = 0.8f;
            main.startSpeed = 0f;
            main.startSize = 0.12f;
            main.maxParticles = 180;
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
            _renderer.sortingOrder = 12;
            _line.enabled = false;
            _line.positionCount = 0;
        }

        void TickProjectile(float deltaTime)
        {
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _impactAt));
            float eased = 1f - Mathf.Pow(1f - progress, 2f);
            Vector3 next = Vector3.Lerp(_origin, _target, eased);
            if (_preset == SkillParticlePreset.ArcaneProjectile)
                next += Vector3.up * (Mathf.Sin(progress * Mathf.PI) * 0.45f);
            transform.position = next;

            _emissionAccumulator += deltaTime;
            while (_emissionAccumulator >= 0.025f)
            {
                _emissionAccumulator -= 0.025f;
                Color color = _preset == SkillParticlePreset.ArcaneProjectile
                    ? ArcaneColor
                    : WaterColor;
                Emit(
                    next + (Vector3)UnityEngine.Random.insideUnitCircle * 0.08f,
                    (Vector3)UnityEngine.Random.insideUnitCircle * 0.35f,
                    color,
                    UnityEngine.Random.Range(0.08f, 0.15f) * _scale,
                    UnityEngine.Random.Range(0.25f, 0.5f));
            }
        }

        void TickLightning()
        {
            if (!_line.enabled)
                return;
            Vector3 start = _target + Vector3.up * (3.4f * _scale);
            int last = _line.positionCount - 1;
            for (int i = 0; i < _line.positionCount; i++)
            {
                float t = last <= 0 ? 0f : i / (float)last;
                Vector3 point = Vector3.Lerp(start, _target, t);
                if (i > 0 && i < last)
                    point.x += UnityEngine.Random.Range(-0.16f, 0.16f) * _scale;
                _line.SetPosition(i, point);
            }
            Color color = LightningColor;
            color.a = 1f - Mathf.Clamp01(_elapsed / Mathf.Max(0.01f, _impactAt + 0.15f));
            _line.startColor = color;
            _line.endColor = Color.white * color.a;
        }

        void TickGale(float deltaTime)
        {
            _emissionAccumulator += deltaTime;
            while (_emissionAccumulator >= 0.045f)
            {
                _emissionAccumulator -= 0.045f;
                float angle = _elapsed * 8f + UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector2 radial = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new(-radial.y, radial.x);
                Vector3 position = _target + (Vector3)(radial * UnityEngine.Random.Range(0.3f, 0.8f) * _scale);
                Vector3 velocity = (Vector3)(tangent * UnityEngine.Random.Range(1.8f, 3.2f) +
                                             radial * UnityEngine.Random.Range(-1.2f, -0.3f));
                Emit(position, velocity, GaleColor, 0.09f * _scale, 0.5f);
            }
        }

        void Impact()
        {
            _impacted = true;
            transform.position = _target;
            switch (_preset)
            {
                case SkillParticlePreset.ArcaneProjectile:
                    EmitRadial(_target, ArcaneColor, 28, 1.2f, 3.6f, 0.11f * _scale, 0.7f);
                    EmitOrbitRing(_target, Color.white, 16, 0.48f * _scale, 1.1f);
                    break;
                case SkillParticlePreset.WaterProjectile:
                    SetGravity(1.4f);
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 horizontal = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 velocity = new(
                            horizontal.x * UnityEngine.Random.Range(0.8f, 2.8f),
                            UnityEngine.Random.Range(1.4f, 3.8f),
                            0f);
                        Emit(
                            _target,
                            velocity,
                            Alternate(WaterColor, Color.white, i),
                            UnityEngine.Random.Range(0.07f, 0.14f) * _scale,
                            UnityEngine.Random.Range(0.45f, 0.85f));
                    }
                    break;
                case SkillParticlePreset.LightningStrike:
                    EmitRadial(_target, LightningColor, 34, 1.8f, 5.2f, 0.08f * _scale, 0.42f);
                    break;
                case SkillParticlePreset.GaleVortex:
                    EmitRadial(_target, GaleColor, 26, 1.2f, 3.4f, 0.09f * _scale, 0.65f);
                    break;
            }
            Action callback = _onImpact;
            _onImpact = null;
            callback?.Invoke();
        }

        void ConfigureLightningLine()
        {
            _line.enabled = true;
            _line.positionCount = 8;
            _line.startWidth = 0.11f * _scale;
            _line.endWidth = 0.035f * _scale;
            _line.startColor = LightningColor;
            _line.endColor = Color.white;
            TickLightning();
        }

        void EmitLightningDescent()
        {
            for (int i = 0; i < 16; i++)
            {
                float t = i / 15f;
                Vector3 position = Vector3.Lerp(
                    _target + Vector3.up * (3.4f * _scale),
                    _target,
                    t);
                position.x += UnityEngine.Random.Range(-0.12f, 0.12f) * _scale;
                Emit(
                    position,
                    Vector3.down * UnityEngine.Random.Range(4f, 8f),
                    Alternate(LightningColor, Color.white, i),
                    0.07f * _scale,
                    0.22f);
            }
        }

        void EmitVortexRing(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector2 radial = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new(-radial.y, radial.x);
                Emit(
                    _target + (Vector3)(radial * 0.75f * _scale),
                    (Vector3)(tangent * 2.8f - radial * 0.8f),
                    Alternate(GaleColor, Color.white, i),
                    0.1f * _scale,
                    0.7f);
            }
        }

        void EmitOrbitRing(
            Vector3 center,
            Color color,
            int count,
            float radius,
            float tangentSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * Mathf.PI * 2f;
                Vector2 radial = new(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 tangent = new(-radial.y, radial.x);
                Emit(
                    center + (Vector3)(radial * radius),
                    (Vector3)(tangent * tangentSpeed + radial * 0.25f),
                    Alternate(color, Color.white, i),
                    UnityEngine.Random.Range(0.07f, 0.12f) * _scale,
                    UnityEngine.Random.Range(0.65f, 1f));
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
                    UnityEngine.Random.Range(lifetime * 0.75f, lifetime * 1.15f));
            }
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
                    new GradientAlphaKey(1f, 0.12f),
                    new GradientAlphaKey(0.78f, 0.72f),
                    new GradientAlphaKey(0f, 1f),
                });
            return gradient;
        }

        internal void ResetForPool()
        {
            _playing = false;
            _impacted = false;
            _elapsed = 0f;
            _impactAt = 0f;
            _releaseAt = 0f;
            _emissionAccumulator = 0f;
            _onImpact = null;
            _service = null;
            _preset = SkillParticlePreset.None;
            _origin = Vector3.zero;
            _target = Vector3.zero;
            _scale = 1f;
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
