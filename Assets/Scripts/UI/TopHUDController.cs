using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>상단 HUD — 웨이브/코어 HP/재화 표시 전용 (데이터 바인딩만)</summary>
    public class TopHUDController
    {
        readonly Label _waveLabel;
        readonly Label _goldValue;
        readonly Label _gemValue;
        readonly VisualElement _coreHpFill;

        public TopHUDController(VisualElement root)
        {
            _waveLabel = root.Q<Label>("wave-label");
            _coreHpFill = root.Q<VisualElement>("core-hp-fill");
            _goldValue = root.Q<Label>("gold-value");
            _gemValue = root.Q<Label>("gem-value");
        }

        public void SetWave(int current, int total) => _waveLabel.text = UIFormat.Wave(current, total);

        public void SetCoreHp(float current, float max)
        {
            // 게이지 fill 너비 — ui-guidelines §5 인라인 스타일 허용 예외 ③
            _coreHpFill.style.width = Length.Percent(Mathf.Clamp01(current / max) * 100f);
        }

        public void SetGold(int value) => _goldValue.text = UIFormat.Gold(value);

        public void SetGems(int value) => _gemValue.text = UIFormat.Gold(value);
    }
}
