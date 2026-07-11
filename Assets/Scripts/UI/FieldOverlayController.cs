using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>필드 오버레이 — 방향 예고 배지 4개 + 스킬 플로팅 버튼.
    /// 배지 위치는 현재 USS 고정, 스파이크 B에서 월드 앵커 동기화로 교체 예정.</summary>
    public class FieldOverlayController
    {
        readonly Dictionary<Direction, VisualElement> _badges = new();
        readonly Dictionary<Direction, Label> _badgeLabels = new();
        readonly Button _skillButton;

        public FieldOverlayController(VisualElement root)
        {
            CacheBadge(root, Direction.North, "badge-north");
            CacheBadge(root, Direction.East, "badge-east");
            CacheBadge(root, Direction.South, "badge-south");
            CacheBadge(root, Direction.West, "badge-west");
            _skillButton = root.Q<Button>("skill-button");
        }

        void CacheBadge(VisualElement root, Direction dir, string elementName)
        {
            var badge = root.Q<VisualElement>(elementName);
            _badges[dir] = badge;
            _badgeLabels[dir] = badge.Q<Label>();
        }

        public void SetBadge(Direction dir, int count, ThreatLevel threat)
        {
            _badgeLabels[dir].text = UIFormat.Badge(dir, count);
            var badge = _badges[dir];
            badge.EnableInClassList("badge--danger", threat == ThreatLevel.Danger);
            badge.EnableInClassList("badge--warning", threat == ThreatLevel.Normal);
            badge.EnableInClassList("badge--muted", threat == ThreatLevel.None);
        }

        public void SetSkillButtonVisible(bool visible) => _skillButton.EnableInClassList("hidden", !visible);
    }
}
