using UnityEngine;

namespace CrossDefense.Units
{
    public enum UnitOutlineState
    {
        None,
        Selected,
        ValidPlacement,
        InvalidPlacement,
        MergeTarget,
    }

    /// <summary>Animated Sprite Outline 셰이더를 선택·배치·머지 피드백에 사용한다.</summary>
    [DisallowMultipleComponent]
    public sealed class AnimatedOutlineFeedback : MonoBehaviour
    {
        static readonly int OutlineColor1 = Shader.PropertyToID("_OutlineColor1");
        static readonly int OutlineColor2 = Shader.PropertyToID("_OutlineColor2");
        static readonly int OutlineWidth1 = Shader.PropertyToID("_OutlineWidth1");
        static readonly int OutlineWidth2 = Shader.PropertyToID("_OutlineWidth2");
        static readonly int OutlineWeight1 = Shader.PropertyToID("_OutlineWeight1");
        static readonly int OutlineWeight2 = Shader.PropertyToID("_OutlineWeight2");
        static readonly int OutlineAccuracy = Shader.PropertyToID("_OutlineAccuracy");
        static readonly int OutlineFlowSpeed1 = Shader.PropertyToID("_OutlineFlowSpeed1");
        static readonly int OutlineFlowSpeed2 = Shader.PropertyToID("_OutlineFlowSpeed2");
        static readonly int MaskTexture1 = Shader.PropertyToID("_MaskTexture1");
        static readonly int MaskTexture2 = Shader.PropertyToID("_MaskTexture2");
        static readonly int MaskUvScale1 = Shader.PropertyToID("_MaskUVScale1");
        static readonly int MaskUvScale2 = Shader.PropertyToID("_MaskUVScale2");

        static Material _sharedMaterial;
        static Texture2D _noiseTexture;

        SpriteRenderer _renderer;
        MaterialPropertyBlock _properties;
        UnitOutlineState _state;

        public UnitOutlineState State => _state;

        void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _properties = new MaterialPropertyBlock();
            if (_renderer != null && TryGetSharedMaterial(out var material))
                _renderer.sharedMaterial = material;
            SetState(UnitOutlineState.None);
        }

        public void SetState(UnitOutlineState state)
        {
            _state = state;
            if (_renderer == null) return;

            _renderer.GetPropertyBlock(_properties);
            Color primary;
            Color secondary;
            float width;
            float weight;
            switch (state)
            {
                case UnitOutlineState.Selected:
                    primary = new Color(0.25f, 0.9f, 1f, 1f);
                    secondary = Color.white;
                    width = 0.22f;
                    weight = 2.5f;
                    break;
                case UnitOutlineState.ValidPlacement:
                    primary = new Color(0.25f, 1f, 0.4f, 1f);
                    secondary = new Color(0.8f, 1f, 0.4f, 1f);
                    width = 0.25f;
                    weight = 3f;
                    break;
                case UnitOutlineState.InvalidPlacement:
                    primary = new Color(1f, 0.18f, 0.15f, 1f);
                    secondary = new Color(1f, 0.65f, 0.2f, 1f);
                    width = 0.28f;
                    weight = 3.2f;
                    break;
                case UnitOutlineState.MergeTarget:
                    primary = new Color(1f, 0.72f, 0.1f, 1f);
                    secondary = new Color(0.75f, 0.3f, 1f, 1f);
                    width = 0.32f;
                    weight = 3.8f;
                    break;
                default:
                    primary = Color.clear;
                    secondary = Color.clear;
                    width = 0f;
                    weight = 0f;
                    break;
            }

            _properties.SetColor(OutlineColor1, primary);
            _properties.SetColor(OutlineColor2, secondary);
            _properties.SetFloat(OutlineWidth1, width);
            _properties.SetFloat(OutlineWidth2, width * 1.45f);
            _properties.SetFloat(OutlineWeight1, weight);
            _properties.SetFloat(OutlineWeight2, weight * 0.75f);
            _properties.SetFloat(OutlineAccuracy, 6f);
            _properties.SetFloat(OutlineFlowSpeed1, 0.8f);
            _properties.SetFloat(OutlineFlowSpeed2, -0.55f);
            _properties.SetFloat(MaskUvScale1, 0.8f);
            _properties.SetFloat(MaskUvScale2, 1.15f);
            _renderer.SetPropertyBlock(_properties);
        }

        static bool TryGetSharedMaterial(out Material material)
        {
            if (_sharedMaterial != null)
            {
                material = _sharedMaterial;
                return true;
            }

            var shader = Shader.Find("AnimatedSpriteOutline/Outline Light");
            if (shader == null)
            {
                material = null;
                return false;
            }

            _sharedMaterial = new Material(shader) { name = "CrossDefense Animated Unit Outline" };
            _noiseTexture = BuildNoiseTexture();
            _sharedMaterial.SetTexture(MaskTexture1, _noiseTexture);
            _sharedMaterial.SetTexture(MaskTexture2, _noiseTexture);
            material = _sharedMaterial;
            return true;
        }

        static Texture2D BuildNoiseTexture()
        {
            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "CrossDefense Outline Noise",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Point,
            };
            var colors = new Color32[size * size];
            var random = new System.Random(2707);
            for (int i = 0; i < colors.Length; i++)
            {
                byte value = (byte)random.Next(32, 256);
                colors[i] = new Color32(value, value, value, value);
            }
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return texture;
        }
    }
}
