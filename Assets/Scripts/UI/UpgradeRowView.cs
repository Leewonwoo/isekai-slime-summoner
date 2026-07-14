using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>UpgradeRow.uxml 템플릿 1개 인스턴스의 뷰 — 강화/스킬/소환사 3탭 공용</summary>
    public class UpgradeRowView
    {
        readonly Label _name;
        readonly Label _values;
        readonly Button _action;
        readonly VisualElement _icon;
        string _iconClass;

        public UpgradeRowView(VisualElement row)
        {
            _name = row.Q<Label>("row-name");
            _values = row.Q<Label>("row-values");
            _action = row.Q<Button>("row-action");
            _icon = row.Q<VisualElement>("row-icon");
        }

        public void Bind(string name, string values, string actionText, string iconClass = null)
        {
            _name.text = name;
            _values.text = values;
            _action.text = actionText;

            if (!string.IsNullOrEmpty(_iconClass))
                _icon.RemoveFromClassList(_iconClass);
            _iconClass = iconClass;
            if (!string.IsNullOrEmpty(_iconClass))
                _icon.AddToClassList(_iconClass);
        }

        /// <summary>재화 부족 시 회색 비활성 — 버튼을 숨기지 않는다 (SPEC §4.5)</summary>
        public void SetAffordable(bool affordable)
        {
            _action.SetEnabled(affordable);
            _action.EnableInClassList("btn--disabled", !affordable);
        }
    }
}
