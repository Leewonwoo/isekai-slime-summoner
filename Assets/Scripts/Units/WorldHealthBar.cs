using UnityEngine;

namespace CrossDefense.Units
{
    public enum WorldHealthBarProfile
    {
        Summoner,
        SummonedUnit,
        Monster,
    }

    /// <summary>
    /// Pooled world actors share a single runtime sprite and update their bar only when HP changes.
    /// The child root cancels the actor scale so rank and monster size do not distort bar thickness.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldHealthBar : MonoBehaviour
    {
        const int BackgroundOrderOffset = 20;
        const int FillOrderOffset = 21;

        static readonly Color BackgroundColor = new Color32(43, 31, 35, 235);
        static readonly Color SummonerColor = new Color32(72, 211, 105, 255);
        static readonly Color SummonedUnitColor = new Color32(78, 211, 111, 255);
        static readonly Color MonsterColor = new Color32(226, 74, 78, 255);

        SpriteRenderer _ownerRenderer;
        Transform _barRoot;
        SpriteRenderer _backgroundRenderer;
        SpriteRenderer _fillRenderer;
        float _barWidth;
        float _barHeight;
        float _gap;
        float _widthFactor;
        float _minWidth;
        float _maxWidth;
        float _normalizedHealth = -1f;
        bool _hasHealth;
        bool _requestedVisible = true;

        public static WorldHealthBar GetOrAdd(GameObject owner)
        {
            if (!owner.TryGetComponent(out WorldHealthBar healthBar))
                healthBar = owner.AddComponent<WorldHealthBar>();
            return healthBar;
        }

        public static float GetNormalizedHealth(float current, float maximum)
        {
            if (maximum <= 0f) return 0f;
            return Mathf.Clamp01(current / maximum);
        }

        public void Configure(SpriteRenderer ownerRenderer, WorldHealthBarProfile profile)
        {
            _ownerRenderer = ownerRenderer;
            EnsureRenderers();
            ApplyProfile(profile);
            ApplySorting();
            RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_ownerRenderer == null || _ownerRenderer.sprite == null) return;
            EnsureRenderers();

            Bounds bounds = _ownerRenderer.bounds;
            _barWidth = Mathf.Clamp(bounds.size.x * _widthFactor, _minWidth, _maxWidth);

            Vector3 ownerScale = transform.lossyScale;
            float inverseX = 1f / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.x));
            float inverseY = 1f / Mathf.Max(0.0001f, Mathf.Abs(ownerScale.y));
            Vector3 worldAnchor = new Vector3(bounds.center.x, bounds.max.y + _gap, transform.position.z);
            _barRoot.localPosition = transform.InverseTransformPoint(worldAnchor);
            _barRoot.localRotation = Quaternion.identity;
            _barRoot.localScale = new Vector3(inverseX, inverseY, 1f);

            const float border = 0.018f;
            _backgroundRenderer.transform.localScale = new Vector3(
                _barWidth + border * 2f,
                _barHeight + border * 2f,
                1f);
            ApplyFill(_normalizedHealth < 0f ? 1f : _normalizedHealth);
        }

        public void SetHealth(float current, float maximum)
        {
            EnsureRenderers();
            _hasHealth = maximum > 0f;
            float normalized = GetNormalizedHealth(current, maximum);
            if (!Mathf.Approximately(_normalizedHealth, normalized))
            {
                _normalizedHealth = normalized;
                ApplyFill(normalized);
            }
            ApplyVisibility();
        }

        public void SetVisible(bool visible)
        {
            _requestedVisible = visible;
            ApplyVisibility();
        }

        public void ResetForPool()
        {
            _hasHealth = false;
            _requestedVisible = true;
            _normalizedHealth = -1f;
            if (_barRoot != null)
                _barRoot.gameObject.SetActive(false);
        }

        void ApplyProfile(WorldHealthBarProfile profile)
        {
            switch (profile)
            {
                case WorldHealthBarProfile.Summoner:
                    _widthFactor = 0.72f;
                    _minWidth = 0.9f;
                    _maxWidth = 1.4f;
                    _barHeight = 0.11f;
                    _gap = 0.12f;
                    SetFillColor(SummonerColor);
                    break;
                case WorldHealthBarProfile.Monster:
                    _widthFactor = 0.9f;
                    _minWidth = 0.52f;
                    _maxWidth = 0.95f;
                    _barHeight = 0.075f;
                    _gap = 0.08f;
                    SetFillColor(MonsterColor);
                    break;
                default:
                    _widthFactor = 0.9f;
                    _minWidth = 0.52f;
                    _maxWidth = 0.9f;
                    _barHeight = 0.075f;
                    _gap = 0.08f;
                    SetFillColor(SummonedUnitColor);
                    break;
            }
        }

        void EnsureRenderers()
        {
            if (_barRoot != null) return;

            var root = new GameObject("HealthBar");
            _barRoot = root.transform;
            _barRoot.SetParent(transform, false);

            _backgroundRenderer = CreatePart("Background", BackgroundColor);
            _fillRenderer = CreatePart("Fill", SummonedUnitColor);
            _barRoot.gameObject.SetActive(false);
        }

        SpriteRenderer CreatePart(string objectName, Color color)
        {
            var part = new GameObject(objectName);
            part.transform.SetParent(_barRoot, false);
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeBarSprite.Shared;
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            return renderer;
        }

        void SetFillColor(Color color)
        {
            if (_fillRenderer != null)
                _fillRenderer.color = color;
        }

        void ApplySorting()
        {
            if (_ownerRenderer == null || _backgroundRenderer == null || _fillRenderer == null) return;
            _backgroundRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
            _fillRenderer.sortingLayerID = _ownerRenderer.sortingLayerID;
            _backgroundRenderer.sortingOrder = _ownerRenderer.sortingOrder + BackgroundOrderOffset;
            _fillRenderer.sortingOrder = _ownerRenderer.sortingOrder + FillOrderOffset;
        }

        void ApplyFill(float normalized)
        {
            if (_fillRenderer == null) return;
            float fillWidth = _barWidth * Mathf.Clamp01(normalized);
            _fillRenderer.enabled = fillWidth > 0.0001f;
            _fillRenderer.transform.localScale = new Vector3(fillWidth, _barHeight, 1f);
            _fillRenderer.transform.localPosition = new Vector3(
                -(_barWidth - fillWidth) * 0.5f,
                0f,
                0f);
        }

        void ApplyVisibility()
        {
            if (_barRoot != null)
                _barRoot.gameObject.SetActive(_hasHealth && _requestedVisible);
        }

        static class RuntimeBarSprite
        {
            static Sprite _shared;

            public static Sprite Shared
            {
                get
                {
                    if (_shared != null) return _shared;
                    _shared = Sprite.Create(
                        Texture2D.whiteTexture,
                        new Rect(0f, 0f, 1f, 1f),
                        new Vector2(0.5f, 0.5f),
                        1f);
                    _shared.name = "RuntimeHealthBarSprite";
                    _shared.hideFlags = HideFlags.HideAndDontSave;
                    return _shared;
                }
            }
        }
    }
}
