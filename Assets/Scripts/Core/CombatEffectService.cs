using System;
using DG.Tweening;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>정적 스프라이트에 DOTween을 적용해 공용 월드 전투 이펙트를 풀링한다.</summary>
    public sealed class CombatEffectService
    {
        readonly Transform _root;
        readonly Transform _template;
        readonly Func<bool> _canPlay;

        public CombatEffectService(
            Transform parent,
            Func<bool> canPlay = null,
            string rootName = "CombatEffects")
        {
            var rootObject = new GameObject(rootName);
            _root = rootObject.transform;
            _root.SetParent(parent, false);
            _canPlay = canPlay;
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseCombatEffect",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 9;
                    gameObject.AddComponent<CombatEffectController>();
                },
                16,
                128);
        }

        internal bool CanPlay => _canPlay?.Invoke() ?? true;

        public void Play(
            Vector3 position,
            Sprite sprite,
            Color color,
            float scale,
            float rotationDegrees = 0f)
        {
            if (sprite == null || !CanPlay)
                return;

            Transform spawned = RuntimePoolService.Spawn(
                _template,
                position,
                Quaternion.Euler(0f, 0f, rotationDegrees),
                _root);
            if (spawned == null)
                return;

            spawned.GetComponent<CombatEffectController>().Play(
                this,
                sprite,
                color,
                Mathf.Max(0.1f, scale));
        }

        public void PlayFrames(
            Vector3 position,
            Sprite[] frames,
            Color color,
            float scale,
            float framesPerSecond = 18f,
            float holdLastFrameSeconds = 0f,
            float rotationDegrees = 0f)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null || !CanPlay)
                return;

            Transform spawned = RuntimePoolService.Spawn(
                _template,
                position,
                Quaternion.Euler(0f, 0f, rotationDegrees),
                _root);
            if (spawned == null)
                return;
            spawned.GetComponent<CombatEffectController>().PlayFrames(
                this,
                frames,
                color,
                Mathf.Max(0.1f, scale),
                Mathf.Max(1f, framesPerSecond),
                Mathf.Max(0f, holdLastFrameSeconds));
        }

        internal void Release(CombatEffectController effect)
        {
            if (effect == null)
                return;
            effect.ResetForPool();
            RuntimePoolService.Despawn(effect.transform);
        }
    }

    [DisallowMultipleComponent]
    public sealed class CombatEffectController : MonoBehaviour
    {
        SpriteRenderer _renderer;
        CombatEffectService _service;
        Sequence _sequence;
        bool _playing;
        Sprite[] _frames;
        float _framesPerSecond;
        float _frameElapsed;
        float _holdLastFrameSeconds;
        bool _frameMode;

        void Awake() => _renderer = GetComponent<SpriteRenderer>();

        public void Play(
            CombatEffectService service,
            Sprite sprite,
            Color color,
            float scale)
        {
            ResetForPool();
            _service = service;
            _playing = true;
            _renderer.sprite = sprite;
            color.a = 1f;
            _renderer.color = color;
            transform.localScale = Vector3.one * (scale * 0.35f);

            _sequence = DOTween.Sequence()
                .SetTarget(this)
                .Append(transform.DOScale(scale * 1.12f, 0.16f).SetEase(Ease.OutBack))
                .Append(transform.DOScale(scale, 0.08f).SetEase(Ease.OutQuad))
                .AppendInterval(0.08f)
                .Append(_renderer.DOFade(0f, 0.24f).SetEase(Ease.InQuad))
                .Join(transform.DOScale(scale * 1.28f, 0.24f).SetEase(Ease.OutQuad))
                .OnComplete(() => _service?.Release(this));
        }

        public void PlayFrames(
            CombatEffectService service,
            Sprite[] frames,
            Color color,
            float scale,
            float framesPerSecond,
            float holdLastFrameSeconds)
        {
            ResetForPool();
            _service = service;
            _playing = true;
            _frameMode = true;
            _frames = frames;
            _framesPerSecond = framesPerSecond;
            _holdLastFrameSeconds = holdLastFrameSeconds;
            _frameElapsed = 0f;
            color.a = 1f;
            _renderer.color = color;
            _renderer.sprite = frames[0];
            transform.localScale = Vector3.one * scale;
        }

        void Update()
        {
            if (_playing && (_service == null || !_service.CanPlay))
            {
                _service?.Release(this);
                return;
            }
            if (!_playing || !_frameMode || _frames == null || _frames.Length == 0)
                return;

            _frameElapsed += Time.deltaTime;
            int frameIndex = Mathf.FloorToInt(_frameElapsed * _framesPerSecond);
            if (frameIndex < _frames.Length)
            {
                _renderer.sprite = _frames[Mathf.Clamp(frameIndex, 0, _frames.Length - 1)];
                return;
            }
            float animationDuration = _frames.Length / _framesPerSecond;
            _renderer.sprite = _frames[_frames.Length - 1];
            if (_frameElapsed >= animationDuration + _holdLastFrameSeconds)
                _service?.Release(this);
        }

        public void ResetForPool()
        {
            _playing = false;
            _sequence?.Kill(false);
            _sequence = null;
            _frames = null;
            _frameMode = false;
            _frameElapsed = 0f;
            _holdLastFrameSeconds = 0f;
            _service = null;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
            if (_renderer == null)
                return;
            _renderer.sprite = null;
            _renderer.color = Color.white;
        }
    }
}
