using UnityEngine;

namespace CrossDefense.Units
{
    /// <summary>
    /// 지원 슬라임의 실제 버프 반경을 공유 런타임 스프라이트로 표시한다.
    /// 부모의 성급 스케일을 상쇄해 월드 반경이 데이터와 일치하도록 유지한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SupportAuraVisual : MonoBehaviour
    {
        const int SortingOrderOffset = -2;
        const float PulseDuration = 0.62f;
        const float PulseRetriggerDelay = 0.22f;
        const float PulseStartScale = 0.84f;
        static readonly Color AuraColor = new Color32(129, 224, 151, 150);

        SpriteRenderer _ownerRenderer;
        Transform _auraRoot;
        SpriteRenderer _auraRenderer;
        float _worldRadius;
        float _pulseStartedAt;
        float _nextPulseAllowedAt;
        bool _isConfigured;
        bool _isPulseActive;

        void Awake()
        {
            _ownerRenderer = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (!_isPulseActive)
                return;

            float normalized = (Time.time - _pulseStartedAt) / PulseDuration;
            if (normalized >= 1f)
            {
                StopPulse();
                return;
            }

            ApplyPulse(Mathf.Clamp01(normalized));
        }

        public void Configure(SpriteRenderer ownerRenderer, float worldRadius, bool supportUnit)
        {
            if (ownerRenderer != null)
                _ownerRenderer = ownerRenderer;

            _worldRadius = Mathf.Max(0f, worldRadius);
            _isConfigured = supportUnit && _worldRadius > 0f;
            StopPulse();
            if (!_isConfigured)
                return;

            EnsureRenderer();
            ApplySorting();
            RefreshScale();
        }

        public void PlayPulse()
        {
            if (!_isConfigured || Time.time < _nextPulseAllowedAt)
                return;

            EnsureRenderer();
            ApplySorting();
            RefreshScale();
            _pulseStartedAt = Time.time;
            _nextPulseAllowedAt = Time.time + PulseRetriggerDelay;
            _isPulseActive = true;
            _auraRenderer.enabled = true;
            ApplyPulse(0f);
        }

        public void ResetForPool()
        {
            _worldRadius = 0f;
            _isConfigured = false;
            _nextPulseAllowedAt = 0f;
            StopPulse();
        }

        void EnsureRenderer()
        {
            if (_auraRenderer != null)
                return;

            var root = new GameObject("SupportAura");
            _auraRoot = root.transform;
            _auraRoot.SetParent(transform, false);
            _auraRoot.localPosition = Vector3.zero;
            _auraRoot.localRotation = Quaternion.identity;

            _auraRenderer = root.AddComponent<SpriteRenderer>();
            _auraRenderer.sprite = RuntimeAuraSprite.Shared;
            _auraRenderer.color = AuraColor;
            _auraRenderer.drawMode = SpriteDrawMode.Simple;
            _auraRenderer.maskInteraction = SpriteMaskInteraction.None;
            _auraRenderer.enabled = false;
        }

        void ApplySorting()
        {
            if (_ownerRenderer == null || _auraRenderer == null)
                return;

            _auraRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
            _auraRenderer.sortingOrder = _ownerRenderer.sortingOrder + SortingOrderOffset;
        }

        void RefreshScale()
        {
            if (_auraRoot == null || _worldRadius <= 0f)
                return;

            Vector3 ownerScale = transform.lossyScale;
            float inverseX = 1f / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.x));
            float inverseY = 1f / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.y));

            // 공유 스프라이트의 기본 지름은 2월드 유닛이므로 scale 1이 반지름 1이다.
            _auraRoot.localScale = new Vector3(
                _worldRadius * inverseX,
                _worldRadius * inverseY,
                1f);
        }

        void ApplyPulse(float normalized)
        {
            if (_auraRenderer == null)
                return;

            float easedExpansion = 1f - (1f - normalized) * (1f - normalized);
            float scale = Mathf.Lerp(PulseStartScale, 1f, easedExpansion);
            _auraRenderer.transform.localScale = Vector3.one * scale;

            Color color = AuraColor;
            color.a *= Mathf.Sin(normalized * Mathf.PI);
            _auraRenderer.color = color;
        }

        void StopPulse()
        {
            _isPulseActive = false;
            if (_auraRenderer == null)
                return;

            _auraRenderer.enabled = false;
            _auraRenderer.color = AuraColor;
            _auraRenderer.transform.localScale = Vector3.one;
        }

        static class RuntimeAuraSprite
        {
            const int TextureSize = 128;
            const float RingStart = 0.91f;
            static Sprite _shared;

            public static Sprite Shared
            {
                get
                {
                    if (_shared != null)
                        return _shared;

                    var texture = new Texture2D(
                        TextureSize,
                        TextureSize,
                        TextureFormat.RGBA32,
                        false)
                    {
                        name = "RuntimeSupportAuraTexture",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp,
                        hideFlags = HideFlags.HideAndDontSave,
                    };

                    var pixels = new Color32[TextureSize * TextureSize];
                    float center = (TextureSize - 1) * 0.5f;
                    float radius = center;
                    for (int y = 0; y < TextureSize; y++)
                    {
                        for (int x = 0; x < TextureSize; x++)
                        {
                            float normalizedX = (x - center) / radius;
                            float normalizedY = (y - center) / radius;
                            float distance = Mathf.Sqrt(
                                normalizedX * normalizedX +
                                normalizedY * normalizedY);
                            byte alpha = distance > 1f
                                ? (byte)0
                                : distance >= RingStart
                                    ? (byte)210
                                    : (byte)28;
                            pixels[y * TextureSize + x] = new Color32(255, 255, 255, alpha);
                        }
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply(false, true);

                    _shared = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, TextureSize, TextureSize),
                        new Vector2(0.5f, 0.5f),
                        TextureSize * 0.5f);
                    _shared.name = "RuntimeSupportAuraSprite";
                    _shared.hideFlags = HideFlags.HideAndDontSave;
                    return _shared;
                }
            }
        }
    }
}
