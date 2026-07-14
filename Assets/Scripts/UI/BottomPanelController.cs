using System.Collections.Generic;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>하단 패널 — 탭 5개 전환, 벤치/리스트 콘텐츠 구성</summary>
    public class BottomPanelController
    {
        const int BenchSlotCount = 12;
        const int GearSlotCount = 6;

        static readonly string[] TabKeys = { "summon", "upgrade", "skill", "gear", "summoner" };

        readonly Dictionary<string, Button> _tabButtons = new();
        readonly Dictionary<string, VisualElement> _tabPages = new();
        readonly VisualElement _root;
        readonly Label _benchCount;
        readonly Label _summonContractCount;
        readonly Button _summonButton;

        public BottomPanelController(VisualElement root, VisualTreeAsset upgradeRowTemplate)
        {
            _root = root;
            foreach (var key in TabKeys)
            {
                var captured = key;
                var button = root.Q<Button>($"tab-{key}");
                _tabButtons[key] = button;
                _tabPages[key] = root.Q<VisualElement>($"page-{key}");
                button.clicked += () => SelectTab(captured);
            }

            BuildSlots(root.Q<VisualElement>("bench"), BenchSlotCount);
            BuildSlots(root.Q<VisualElement>("gear-slots"), GearSlotCount);
            _benchCount = root.Q<Label>("bench-count");
            _summonContractCount = root.Q<Label>("summon-contract-count");
            _summonButton = root.Q<Button>("summon-button");
            SetBenchUsage(0);
            SetSummonContracts(0);

            // 스캐폴딩 더미 행 — 실제 데이터 바인딩은 코어 루프 이후 교체
            BuildDummyRows(root.Q<ScrollView>("upgrade-list"), upgradeRowTemplate,
                new[] { "공격력", "공격 속도", "코어 회복", "치명타 확률" }, "120 G",
                new[] { "row__icon--atk", "row__icon--aspd", "row__icon--hp", "row__icon--crit" });
            BuildDummyRows(root.Q<ScrollView>("skill-list"), upgradeRowTemplate,
                new[] { "메테오", "급속 소환", "코어 실드" }, "개방");
            BuildDummyRows(root.Q<ScrollView>("summoner-list"), upgradeRowTemplate,
                new[] { "공격력 보정", "소환 확률", "EXP 획득량" }, "5 SP");
        }

        public void SelectTab(string key)
        {
            foreach (var tab in TabKeys)
            {
                bool active = tab == key;
                _tabButtons[tab].EnableInClassList("tab-bar__button--active", active);
                _tabPages[tab].EnableInClassList("hidden", !active);
            }
        }

        public void SetRedDot(string tabKey, bool on) =>
            _root.Q<VisualElement>($"reddot-{tabKey}")?.EnableInClassList("hidden", !on);

        public void SetBenchUsage(int used) =>
            _benchCount.text = UIFormat.Capacity(used, BenchSlotCount);

        public void SetSummonContracts(int amount)
        {
            _summonContractCount.text = UIFormat.SummonContracts(amount);
            bool canSummon = amount > 0;
            _summonButton.SetEnabled(canSummon);
            _summonButton.EnableInClassList("btn--disabled", !canSummon);
        }

        static void BuildSlots(VisualElement container, int count)
        {
            if (container == null) return;
            for (int i = 0; i < count; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("bench-slot");
                container.Add(slot);
            }
        }

        static void BuildDummyRows(
            ScrollView list,
            VisualTreeAsset template,
            string[] names,
            string actionText,
            string[] iconClasses = null)
        {
            if (list == null || template == null) return;
            for (int i = 0; i < names.Length; i++)
            {
                var row = template.Instantiate();
                string iconClass = iconClasses != null && i < iconClasses.Length ? iconClasses[i] : null;
                new UpgradeRowView(row).Bind($"{names[i]} Lv.1", UIFormat.Delta(10, 12), actionText, iconClass);
                list.Add(row);
            }
        }
    }
}
