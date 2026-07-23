using DG.Tweening;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>상단 HUD — 소환사 프로필/스테이지/재화 표시 전용 (데이터 바인딩만)</summary>
    public class TopHUDController
    {
        readonly Label _summonerNickname;
        readonly Label _summonerLevel;
        readonly Label _stageName;
        readonly Label _goldValue;
        readonly Label _gemValue;
        Tweener _goldTween;
        int _displayedGold;
        bool _goldInitialized;

        public TopHUDController(VisualElement root)
        {
            _summonerNickname = root.Q<Label>("summoner-nickname");
            _summonerLevel = root.Q<Label>("summoner-level");
            _stageName = root.Q<Label>("stage-name");
            _goldValue = root.Q<Label>("gold-value");
            _gemValue = root.Q<Label>("gem-value");
        }

        public void SetSummonerProfile(string nickname, int level)
        {
            _summonerNickname.text = nickname;
            _summonerLevel.text = $"Lv.{level}";
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
            Vector2 panelPosition = _goldValue.worldBound.center;
            float panelWidth = _goldValue.panel?.visualTree.resolvedStyle.width ?? Screen.width;
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

        public void SetGems(int value) => _gemValue.text = UIFormat.Gems(value);
    }
}
