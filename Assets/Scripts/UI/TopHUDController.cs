using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>필드 스테이지와 강화 섹션 골드 표시 데이터 바인딩.</summary>
    public class TopHUDController
    {
        readonly Label _stageName;
        readonly Label _goldValue;
        readonly VisualElement _upgradeTabButton;
        Tweener _goldTween;
        int _displayedGold;
        bool _goldInitialized;

        public TopHUDController(VisualElement root)
        {
            _stageName = root.Q<Label>("stage-name");
            _goldValue = root.Q<Label>("gold-value");
            _upgradeTabButton = root.Q<VisualElement>("tab-upgrade");
        }

        public void SetStageName(string stageName) => _stageName.text = stageName;

        public void SetGold(int value)
        {
            value = Mathf.Max(0, value);
            _goldTween?.Kill();
            if (!_goldInitialized)
            {
                _goldInitialized = true;
                _displayedGold = value;
                _goldValue.text = UIFormat.Gold(value);
                return;
            }

            _goldTween = DOTween.To(
                    () => _displayedGold,
                    current =>
                    {
                        _displayedGold = current;
                        _goldValue.text = UIFormat.Gold(current);
                    },
                    value,
                    0.3f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true);
        }

        public Vector2 GetGoldScreenPosition()
        {
            VisualElement target = IsDisplayed(_goldValue) ? _goldValue : _upgradeTabButton;
            Vector2 panelPosition = target.worldBound.center;
            float panelWidth = target.panel?.visualTree.resolvedStyle.width ?? Screen.width;
            float pixelsPerPoint = panelWidth > 0f && !float.IsNaN(panelWidth)
                ? Screen.width / panelWidth
                : 1f;
            return new Vector2(
                panelPosition.x * pixelsPerPoint,
                Screen.height - panelPosition.y * pixelsPerPoint);
        }

        public void Dispose()
        {
            _goldTween?.Kill();
            _goldTween = null;
        }

        static bool IsDisplayed(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }
    }
}
