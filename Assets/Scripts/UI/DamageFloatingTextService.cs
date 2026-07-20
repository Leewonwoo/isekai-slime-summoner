using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace CrossDefense.UI
{
    /// <summary>
    /// Dedicated batched uGUI layer for pooled combat numbers.
    /// It intentionally owns no raycaster, layout component, animator, coroutine, or per-entry canvas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DamageFloatingTextService : MonoBehaviour
    {
        const int PrewarmCount = 24;
        const int MaxEntryCount = 64;
        const int SortingOrder = 1000;
        const int FontSize = 44;
        const float Lifetime = 0.72f;
        const float RiseDistance = 84f;
        const float HorizontalJitter = 14f;
        const float FadeStart = 0.58f;

        static readonly Vector2 ReferenceResolution = new(1080f, 1920f);
        static readonly Vector2 TextSize = new(240f, 90f);
        static readonly Color DealtColor = new Color32(240, 234, 214, 255);
        static readonly Color ReceivedColor = new Color32(255, 82, 82, 255);
        static readonly Color ShadowColor = new(0f, 0f, 0f, 0.72f);
        static readonly string[] SmallIntegerTextCache = new string[1000];

        readonly List<Entry> _entries = new(MaxEntryCount);

        Camera _worldCamera;
        Canvas _canvas;
        RectTransform _root;
        Font _sharedFont;
        int _sequence;
        bool _initialized;

        public int ActiveCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _entries.Count; i++)
                {
                    if (_entries[i].Active)
                        count++;
                }
                return count;
            }
        }

        public int CreatedCount => _entries.Count;
        public int Capacity => MaxEntryCount;
        public Canvas OverlayCanvas => _canvas;

        public static DamageFloatingTextService GetOrAdd(GameObject host)
        {
            if (host == null) return null;
            if (!host.TryGetComponent(out DamageFloatingTextService service))
                service = host.AddComponent<DamageFloatingTextService>();
            return service;
        }

        public void Initialize(Camera worldCamera)
        {
            if (worldCamera != null)
                _worldCamera = worldCamera;
            if (_initialized)
                return;

            _sharedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_sharedFont == null)
            {
                Debug.LogError("[CrossDefense] 데미지 플로팅 텍스트용 내장 폰트를 불러오지 못했습니다.", this);
                return;
            }

            BuildCanvas();
            for (int i = 0; i < PrewarmCount; i++)
                _entries.Add(CreateEntry(i));
            Canvas.ForceUpdateCanvases();
            _initialized = true;
        }

        public bool Show(Vector3 worldPosition, float amount, DamageTextKind kind)
        {
            if (amount <= 0f)
                return false;
            if (!_initialized)
                Initialize(_worldCamera != null ? _worldCamera : Camera.main);
            if (!_initialized || _root == null)
                return false;
            if (_worldCamera == null)
                _worldCamera = Camera.main;
            if (_worldCamera == null)
                return false;

            Vector3 screenPoint = _worldCamera.WorldToScreenPoint(worldPosition);
            if (screenPoint.z <= 0f ||
                screenPoint.x < 0f || screenPoint.x > Screen.width ||
                screenPoint.y < 0f || screenPoint.y > Screen.height)
                return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _root,
                    screenPoint,
                    null,
                    out Vector2 localPoint))
                return false;

            Entry entry = AcquireEntry();
            float jitter = NextHorizontalJitter();
            entry.StartPosition = localPoint + new Vector2(jitter, 0f);
            entry.Elapsed = 0f;
            entry.Active = true;
            entry.Text.text = FormatDamage(amount);
            entry.Text.color = kind == DamageTextKind.Received ? ReceivedColor : DealtColor;
            entry.Rect.anchoredPosition = entry.StartPosition;
            entry.Rect.localScale = Vector3.one * 0.82f;
            entry.GameObject.SetActive(true);
            entry.Renderer.SetAlpha(1f);
            return true;
        }

        void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            for (int i = 0; i < _entries.Count; i++)
            {
                Entry entry = _entries[i];
                if (!entry.Active)
                    continue;

                entry.Elapsed += deltaTime;
                if (entry.Elapsed >= Lifetime)
                {
                    Deactivate(entry);
                    continue;
                }

                float progress = Mathf.Clamp01(entry.Elapsed / Lifetime);
                float easedRise = 1f - (1f - progress) * (1f - progress);
                entry.Rect.anchoredPosition =
                    entry.StartPosition + Vector2.up * (RiseDistance * easedRise);

                float scale;
                if (progress < 0.18f)
                    scale = Mathf.LerpUnclamped(0.82f, 1.08f, progress / 0.18f);
                else
                    scale = Mathf.Lerp(1.08f, 1f, Mathf.InverseLerp(0.18f, 0.38f, progress));
                entry.Rect.localScale = Vector3.one * scale;

                float alpha = 1f - Mathf.InverseLerp(FadeStart, 1f, progress);
                entry.Renderer.SetAlpha(alpha);
            }
        }

        void OnDisable()
        {
            for (int i = 0; i < _entries.Count; i++)
                Deactivate(_entries[i]);
        }

        void BuildCanvas()
        {
            var canvasObject = new GameObject(
                "DamageFloatingTextCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _root = canvasObject.GetComponent<RectTransform>();
            _root.SetParent(transform, false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;

            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.overrideSorting = true;
            _canvas.sortingOrder = SortingOrder;
            _canvas.pixelPerfect = false;
            _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.None;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
        }

        Entry CreateEntry(int index)
        {
            var textObject = new GameObject(
                $"DamageText_{index:00}",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text),
                typeof(Shadow));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(_root, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = TextSize;

            Text text = textObject.GetComponent<Text>();
            text.font = _sharedFont;
            text.material = _sharedFont.material;
            text.fontSize = FontSize;
            text.fontStyle = FontStyle.Normal;
            text.alignment = TextAnchor.MiddleCenter;
            text.alignByGeometry = true;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.resizeTextForBestFit = false;
            text.supportRichText = false;
            text.raycastTarget = false;
            text.maskable = false;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = ShadowColor;
            shadow.effectDistance = new Vector2(2f, -2f);
            shadow.useGraphicAlpha = true;

            CanvasRenderer canvasRenderer = textObject.GetComponent<CanvasRenderer>();
            canvasRenderer.cullTransparentMesh = true;
            textObject.SetActive(false);
            return new Entry(textObject, rect, text, canvasRenderer);
        }

        Entry AcquireEntry()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (!_entries[i].Active)
                    return _entries[i];
            }

            if (_entries.Count < MaxEntryCount)
            {
                Entry created = CreateEntry(_entries.Count);
                _entries.Add(created);
                return created;
            }

            Entry oldest = _entries[0];
            for (int i = 1; i < _entries.Count; i++)
            {
                if (_entries[i].Elapsed > oldest.Elapsed)
                    oldest = _entries[i];
            }
            return oldest;
        }

        float NextHorizontalJitter()
        {
            _sequence = unchecked(_sequence + 1);
            uint hash = unchecked((uint)_sequence * 747796405u + 2891336453u);
            hash = ((hash >> ((int)(hash >> 28) + 4)) ^ hash) * 277803737u;
            hash = (hash >> 22) ^ hash;
            float normalized = (hash & 0xffffu) / 65535f;
            return Mathf.Lerp(-HorizontalJitter, HorizontalJitter, normalized);
        }

        static string FormatDamage(float amount)
        {
            float safeAmount = Mathf.Max(0f, amount);
            int rounded = Mathf.RoundToInt(safeAmount);
            if (Mathf.Abs(safeAmount - rounded) <= 0.05f)
            {
                if (rounded >= 0 && rounded < SmallIntegerTextCache.Length)
                    return SmallIntegerTextCache[rounded] ??=
                        rounded.ToString(CultureInfo.InvariantCulture);
                return rounded.ToString("N0", CultureInfo.InvariantCulture);
            }
            return safeAmount.ToString("0.#", CultureInfo.InvariantCulture);
        }

        static void Deactivate(Entry entry)
        {
            if (entry == null || !entry.Active)
                return;
            entry.Active = false;
            entry.Elapsed = 0f;
            entry.GameObject.SetActive(false);
        }

        sealed class Entry
        {
            public readonly GameObject GameObject;
            public readonly RectTransform Rect;
            public readonly Text Text;
            public readonly CanvasRenderer Renderer;
            public Vector2 StartPosition;
            public float Elapsed;
            public bool Active;

            public Entry(
                GameObject gameObject,
                RectTransform rect,
                Text text,
                CanvasRenderer renderer)
            {
                GameObject = gameObject;
                Rect = rect;
                Text = text;
                Renderer = renderer;
            }
        }
    }
}
