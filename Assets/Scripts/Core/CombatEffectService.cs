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

        public CombatEffectService(Transform parent, Func<bool> canPlay = null)
        {
            var rootObject = new GameObject("CombatEffects");
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

        void Update()
        {
            if (_playing && (_service == null || !_service.CanPlay))
                _service?.Release(this);
        }

        public void ResetForPool()
        {
            _playing = false;
            _sequence?.Kill(false);
            _sequence = null;
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
