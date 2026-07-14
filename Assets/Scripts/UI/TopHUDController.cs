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

        public void SetGold(int value) => _goldValue.text = UIFormat.Gold(value);

        public void SetGems(int value) => _gemValue.text = UIFormat.Gems(value);
    }
}
