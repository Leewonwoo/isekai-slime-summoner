using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class MonsterCodexModalController
    {
        sealed class SlotView
        {
            public MonsterData Monster;
            public Button Button;
            public Image Image;
            public Label Name;
            public Label Unknown;
        }

        readonly VisualElement _overlay;
        readonly VisualElement _grid;
        readonly Button _close;
        readonly Image _detailImage;
        readonly Label _detailName;
        readonly Label _detailAttribute;
        readonly Label _detailStats;
        readonly List<SlotView> _slots = new();
        MonsterCatalog _catalog;
        MonsterCodexProgression _progression;
        string _selectedId;
        const string HiddenClass = "modal-overlay--hidden";
        public bool IsVisible => !_overlay.ClassListContains(HiddenClass);
        public event Action CloseRequested;

        public MonsterCodexModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("codex-overlay");
            _grid = root.Q<VisualElement>("codex-grid");
            _close = root.Q<Button>("codex-close-button");
            _detailImage = root.Q<Image>("codex-detail-image");
            _detailName = root.Q<Label>("codex-detail-name");
            _detailAttribute = root.Q<Label>("codex-detail-attribute");
            _detailStats = root.Q<Label>("codex-detail-stats");
            _close.clicked += OnClose;
        }

        public void Bind(MonsterCatalog catalog, MonsterCodexProgression progression)
        {
            if (_progression != null) _progression.Changed -= OnProgressionChanged;
            _catalog = catalog;
            _progression = progression;
            if (_progression != null) _progression.Changed += OnProgressionChanged;
            BuildGrid();
        }

        public void Show()
        {
            Refresh();
            _overlay.RemoveFromClassList(HiddenClass);
        }

        public void Hide() => _overlay.AddToClassList(HiddenClass);

        public void Dispose()
        {
            _close.clicked -= OnClose;
            if (_progression != null) _progression.Changed -= OnProgressionChanged;
        }

        void BuildGrid()
        {
            _grid.Clear();
            _slots.Clear();
            if (_catalog?.Monsters == null) return;
            foreach (MonsterData monster in _catalog.Monsters)
            {
                if (monster == null) continue;
                var button = new Button { name = $"codex-{monster.MonsterId}" };
                button.AddToClassList("codex-slot");
                var image = new Image { pickingMode = PickingMode.Ignore };
                image.AddToClassList("codex-slot__image");
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList("codex-slot__name");
                var unknown = new Label("?") { pickingMode = PickingMode.Ignore };
                unknown.AddToClassList("codex-slot__unknown");
                button.Add(image); button.Add(name); button.Add(unknown);
                var slot = new SlotView { Monster = monster, Button = button, Image = image, Name = name, Unknown = unknown };
                button.clicked += () => Select(slot.Monster);
                _slots.Add(slot);
                _grid.Add(button);
            }
            Refresh();
        }

        void Refresh()
        {
            foreach (SlotView slot in _slots)
            {
                MonsterCodexEntry entry = _progression?.Get(slot.Monster.MonsterId) ?? default;
                bool known = entry.Encountered;
                slot.Image.sprite = known ? slot.Monster.Sprite : null;
                slot.Image.EnableInClassList("hidden", !known);
                slot.Name.text = known ? slot.Monster.DisplayName : string.Empty;
                slot.Name.EnableInClassList("hidden", !known);
                slot.Unknown.EnableInClassList("hidden", known);
                slot.Button.EnableInClassList("codex-slot--selected", _selectedId == slot.Monster.MonsterId);
            }
            if (!string.IsNullOrWhiteSpace(_selectedId))
                Select(_catalog?.Find(_selectedId));
        }

        void Select(MonsterData monster)
        {
            if (monster == null) return;
            _selectedId = monster.MonsterId;
            MonsterCodexEntry entry = _progression?.Get(monster.MonsterId) ?? default;
            foreach (SlotView slot in _slots)
                slot.Button.EnableInClassList("codex-slot--selected", slot.Monster.MonsterId == _selectedId);
            if (!entry.Encountered)
            {
                _detailImage.sprite = null;
                _detailName.text = "?";
                _detailAttribute.text = "아직 조우하지 않은 몬스터";
                _detailStats.text = string.Empty;
                return;
            }
            _detailImage.sprite = monster.Sprite;
            _detailName.text = monster.DisplayName;
            _detailAttribute.text = $"속성 · {AttributeName(monster.Attribute)}";
            _detailStats.text =
                $"HP {monster.BaseHp:N0} · 공격 {monster.ContactDamage:N0} · 초당 공격 {monster.AttacksPerSecond:0.##}\n" +
                $"속도 {monster.MoveSpeed:0.##} · 사거리 {monster.AttackRange:0.##} · 골드 {monster.RewardGold:N0}\n" +
                $"누적 처치 {entry.Kills:N0}";
        }

        void OnProgressionChanged(string _) => Refresh();
        void OnClose() => CloseRequested?.Invoke();

        static string AttributeName(MonsterAttribute attribute) => attribute switch
        {
            MonsterAttribute.Fire => "화염",
            MonsterAttribute.Ice => "빙결",
            MonsterAttribute.Nature => "자연",
            _ => "무",
        };
    }
}
