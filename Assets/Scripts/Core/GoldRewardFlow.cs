using System;
using DG.Tweening;
using UnityEngine;

namespace CrossDefense.Core
{
    /// <summary>PoolBoss 골드 오브를 처치 지점에서 HUD 골드 앵커까지 운반한다.</summary>
    public sealed class GoldRewardFlow
    {
        static Sprite _goldSprite;

        readonly Camera _camera;
        readonly Func<Vector2> _screenTargetProvider;
        readonly Action<int> _onArrived;
        readonly Transform _template;

        public GoldRewardFlow(
            Camera camera,
            Func<Vector2> screenTargetProvider,
            Action<int> onArrived)
        {
            _camera = camera != null ? camera : Camera.main;
            _screenTargetProvider = screenTargetProvider;
            _onArrived = onArrived;
            Sprite goldSprite = CreateGoldSprite();
            _template = RuntimePoolService.GetOrCreateTemplate(
                "CrossDefenseGoldReward",
                gameObject =>
                {
                    var renderer = gameObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = goldSprite;
                    renderer.color = new Color(1f, 0.78f, 0.15f, 1f);
                    renderer.sortingOrder = 20;
                    gameObject.AddComponent<GoldRewardOrb>();
                },
                24,
                192);
        }

        public void Present(Vector3 worldOrigin, int amount)
        {
            if (amount <= 0) return;
            if (_camera == null)
            {
                _onArrived?.Invoke(amount);
                return;
            }

            Transform spawned = RuntimePoolService.Spawn(_template, worldOrigin, Quaternion.identity);
            var orb = spawned != null ? spawned.GetComponent<GoldRewardOrb>() : null;
            if (orb == null)
            {
                if (spawned != null) RuntimePoolService.Despawn(spawned);
                _onArrived?.Invoke(amount);
                return;
            }

            orb.Play(_camera, _screenTargetProvider, amount, _onArrived);
        }

        static Sprite CreateGoldSprite()
        {
            if (_goldSprite != null) return _goldSprite;
            const int size = 12;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "RuntimeGoldRewardTexture",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            Vector2 center = new((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radiusSq = 5f * 5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distanceSq = ((Vector2)new Vector2(x, y) - center).sqrMagnitude;
                pixels[y * size + x] = distanceSq <= radiusSq
                    ? new Color32(255, 216, 54, 255)
                    : new Color32(0, 0, 0, 0);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            _goldSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), Vector2.one * 0.5f, 24f);
            _goldSprite.name = "RuntimeGoldRewardSprite";
            _goldSprite.hideFlags = HideFlags.HideAndDontSave;
            return _goldSprite;
        }
    }

    [DisallowMultipleComponent]
    public sealed class GoldRewardOrb : MonoBehaviour
    {
        Sequence _sequence;
        int _amount;
        Action<int> _onArrived;

        public void Play(
            Camera worldCamera,
            Func<Vector2> screenTargetProvider,
            int amount,
            Action<int> onArrived)
        {
            _sequence?.Kill();
            _amount = amount;
            _onArrived = onArrived;
            transform.localScale = Vector3.one * 0.55f;

            Vector3 origin = transform.position;
            Vector2 scatter = UnityEngine.Random.insideUnitCircle.normalized * 0.45f;
            Vector3 burstPosition = origin + new Vector3(scatter.x, scatter.y, 0f);
            Vector2 screenTarget = screenTargetProvider?.Invoke() ??
                new Vector2(Screen.width * 0.78f, Screen.height * 0.95f);
            float depth = Mathf.Abs(worldCamera.transform.position.z - origin.z);
            Vector3 worldTarget = worldCamera.ScreenToWorldPoint(
                new Vector3(screenTarget.x, screenTarget.y, depth));
            worldTarget.z = origin.z;

            _sequence = DOTween.Sequence()
                .Append(transform.DOMove(burstPosition, 0.16f).SetEase(Ease.OutQuad))
                .AppendInterval(0.05f)
                .Append(transform.DOMove(worldTarget, 0.48f).SetEase(Ease.InCubic))
                .Join(transform.DOScale(0.16f, 0.48f).SetEase(Ease.InQuad))
                .OnComplete(Complete);
        }

        void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        void Complete()
        {
            int amount = _amount;
            var callback = _onArrived;
            _amount = 0;
            _onArrived = null;
            _sequence = null;
            RuntimePoolService.Despawn(transform);
            callback?.Invoke(amount);
        }
    }
}
