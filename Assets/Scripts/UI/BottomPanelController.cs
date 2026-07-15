using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    /// <summary>하단 패널 — 탭 5개 전환, 벤치/리스트 콘텐츠 구성</summary>
    public class BottomPanelController
    {
        const int BenchSlotCount = 12;
        const int GearSlotCount = 6;
        const float BenchDragThreshold = 22f;

        static readonly string[] TabKeys = { "summon", "upgrade", "skill", "gear", "summoner" };

        readonly Dictionary<string, Button> _tabButtons = new();
        readonly Dictionary<string, VisualElement> _tabPages = new();
        readonly VisualElement _root;
        readonly VisualElement _bench;
        readonly List<VisualElement> _benchSlots = new();
        readonly Label _benchCount;
        readonly Label _summonContractCount;
        readonly Button _summonButton;
        int _summonContracts;
        bool _isSummonAnimating;

        public event Action SummonRequested;
        public event Action<SummonUnitInstance, Vector2> BenchDragStarted;
        public event Action<SummonUnitInstance, Vector2> BenchDragMoved;
        public event Action<SummonUnitInstance, Vector2> BenchDragEnded;
        public bool IsSummonAnimating => _isSummonAnimating;

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

            _bench = root.Q<VisualElement>("bench");
            BuildBenchSlots();
            BuildSlots(root.Q<VisualElement>("gear-slots"), GearSlotCount);
            _benchCount = root.Q<Label>("bench-count");
            _summonContractCount = root.Q<Label>("summon-contract-count");
            _summonButton = root.Q<Button>("summon-button");
            _summonButton.clicked += () => SummonRequested?.Invoke();
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
            _summonContracts = amount;
            _summonContractCount.text = UIFormat.SummonContracts(amount);
            UpdateSummonButtonState();
        }

        public void SetSummonAnimationState(bool isAnimating)
        {
            _isSummonAnimating = isAnimating;
            UpdateSummonButtonState();
        }

        public void SetBench(IReadOnlyList<SummonUnitInstance> units)
        {
            for (int i = 0; i < _benchSlots.Count; i++)
            {
                var slot = _benchSlots[i];
                slot.Clear();
                if (units == null || i >= units.Count)
                    continue;

                var instance = units[i];
                var card = new VisualElement();
                card.AddToClassList("bench-card");
                var name = new Label(instance.Unit == null ? "알 수 없는 유닛" : instance.Unit.DisplayName);
                name.AddToClassList("bench-card__name");
                var rank = new Label(instance.Rank <= 0 ? "기본" : new string('★', instance.Rank));
                rank.AddToClassList("bench-card__rank");
                card.Add(rank);
                card.Add(name);
                RegisterBenchDrag(card, instance);
                slot.Add(card);
            }

            SetBenchUsage(units == null ? 0 : units.Count);
        }

        void RegisterBenchDrag(VisualElement card, SummonUnitInstance instance)
        {
            Vector2 pressPosition = default;
            bool dragging = false;
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 0) return;
                card.CapturePointer(evt.pointerId);
                pressPosition = evt.position;
                dragging = false;
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (!card.HasPointerCapture(evt.pointerId)) return;
                if (!dragging && Vector2.Distance(pressPosition, evt.position) >= BenchDragThreshold)
                {
                    dragging = true;
                    BenchDragStarted?.Invoke(instance, evt.position);
                }
                if (!dragging) return;
                BenchDragMoved?.Invoke(instance, evt.position);
                evt.StopPropagation();
            });
            card.RegisterCallback<PointerUpEvent>(evt =>
            {
                if (!card.HasPointerCapture(evt.pointerId)) return;
                card.ReleasePointer(evt.pointerId);
                if (dragging)
                    BenchDragEnded?.Invoke(instance, evt.position);
                dragging = false;
                evt.StopPropagation();
            });
        }

        void UpdateSummonButtonState()
        {
            bool canSummon = _summonContracts > 0 && !_isSummonAnimating;
            _summonButton.SetEnabled(canSummon);
            _summonButton.EnableInClassList("btn--disabled", !canSummon);
        }

        void BuildBenchSlots()
        {
            if (_bench == null) return;
            for (int i = 0; i < BenchSlotCount; i++)
            {
                var slot = new VisualElement();
                slot.AddToClassList("bench-slot");
                _bench.Add(slot);
                _benchSlots.Add(slot);
            }
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
