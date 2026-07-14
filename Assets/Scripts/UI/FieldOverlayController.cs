using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>필드 오버레이 — 필드 상단 웨이브 상태와 스킬 플로팅 버튼.</summary>
    public class FieldOverlayController
    {
        readonly Label _waveLabel;
        readonly Label _remainingMonstersLabel;
        readonly Button _skillButton;

        public FieldOverlayController(VisualElement root)
        {
            _waveLabel = root.Q<Label>("wave-label");
            _remainingMonstersLabel = root.Q<Label>("remaining-monsters-label");
            _skillButton = root.Q<Button>("skill-button");
        }

        public void SetWave(int current, int remainingMonsters)
        {
            _waveLabel.text = UIFormat.Wave(current);
            _remainingMonstersLabel.text = UIFormat.RemainingMonsters(remainingMonsters);
        }

        public void SetSkillButtonVisible(bool visible) => _skillButton.EnableInClassList("hidden", !visible);
    }
}
