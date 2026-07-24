using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class SlimeCodexModalController
    {
        const string HiddenClass = "modal-overlay--hidden";

        sealed class SlotView
        {
            public SummonUnitData Unit;
            public Button Button;
            public Image Image;
            public Label Name;
            public Label Unknown;
            public Label Unlock;
        }

        readonly VisualElement _overlay;
        readonly VisualElement _grid;
        readonly Button _close;
        readonly Image _detailImage;
        readonly Label _detailName;
        readonly Label _detailAttribute;
        readonly Label _detailStats;
        readonly List<SlotView> _slots = new();
        IReadOnlyList<SummonUnitData> _units;
        SummonerProgression _progression;
        string _selectedId;

        public bool IsVisible => !_overlay.ClassListContains(HiddenClass);
        public event Action CloseRequested;

        public SlimeCodexModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("slime-codex-overlay");
            _grid = root.Q<VisualElement>("slime-codex-grid");
            _close = root.Q<Button>("slime-codex-close-button");
            _detailImage = root.Q<Image>("slime-codex-detail-image");
            _detailName = root.Q<Label>("slime-codex-detail-name");
            _detailAttribute = root.Q<Label>("slime-codex-detail-attribute");
            _detailStats = root.Q<Label>("slime-codex-detail-stats");
            _close.clicked += OnClose;
        }

        public void Bind(IReadOnlyList<SummonUnitData> units, SummonerProgression progression)
        {
            if (_progression != null)
                _progression.Changed -= OnProgressionChanged;
            _units = units;
            _progression = progression;
            if (_progression != null)
                _progression.Changed += OnProgressionChanged;
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
            if (_progression != null)
                _progression.Changed -= OnProgressionChanged;
        }

        void BuildGrid()
        {
            _grid.Clear();
            _slots.Clear();
            if (_units == null)
                return;

            foreach (SummonUnitData unit in _units)
            {
                if (unit == null)
                    continue;
                var button = new Button { name = $"slime-codex-{unit.UnitId}" };
                button.AddToClassList("slime-codex-slot");
                var image = new Image { pickingMode = PickingMode.Ignore };
                image.AddToClassList("slime-codex-slot__image");
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList("slime-codex-slot__name");
                var unknown = new Label("?") { pickingMode = PickingMode.Ignore };
                unknown.AddToClassList("slime-codex-slot__unknown");
                var unlock = new Label { pickingMode = PickingMode.Ignore };
                unlock.AddToClassList("slime-codex-slot__unlock");
                button.Add(image);
                button.Add(name);
                button.Add(unknown);
                button.Add(unlock);
                var slot = new SlotView
                {
                    Unit = unit,
                    Button = button,
                    Image = image,
                    Name = name,
                    Unknown = unknown,
                    Unlock = unlock,
                };
                button.clicked += () => Select(slot.Unit);
                _slots.Add(slot);
                _grid.Add(button);
            }
            Refresh();
        }

        void Refresh()
        {
            int level = _progression?.Snapshot.Level ?? 1;
            foreach (SlotView slot in _slots)
            {
                bool unlocked = slot.Unit.IsUnlockedAtLevel(level);
                slot.Image.sprite = unlocked ? slot.Unit.WorldSpriteAtRank(0) : null;
                slot.Image.EnableInClassList("hidden", !unlocked);
                slot.Name.text = unlocked ? slot.Unit.DisplayName : string.Empty;
                slot.Name.EnableInClassList("hidden", !unlocked);
                slot.Unknown.EnableInClassList("hidden", unlocked);
                slot.Unlock.text = unlocked ? AttributeName(slot.Unit.Attribute) : $"Lv.{slot.Unit.UnlockLevel} 해금";
                slot.Button.EnableInClassList(
                    "slime-codex-slot--selected",
                    _selectedId == slot.Unit.UnitId);
            }

            if (!string.IsNullOrWhiteSpace(_selectedId))
                Select(Find(_selectedId));
        }

        void Select(SummonUnitData unit)
        {
            if (unit == null)
                return;

            _selectedId = unit.UnitId;
            foreach (SlotView slot in _slots)
                slot.Button.EnableInClassList(
                    "slime-codex-slot--selected",
                    slot.Unit.UnitId == _selectedId);

            int level = _progression?.Snapshot.Level ?? 1;
            if (!unit.IsUnlockedAtLevel(level))
            {
                _detailImage.sprite = null;
                _detailName.text = "?";
                _detailAttribute.text = $"소환사 Lv.{unit.UnlockLevel} 해금";
                _detailStats.text = string.Empty;
                return;
            }

            _detailImage.sprite = unit.WorldSpriteAtRank(0);
            _detailName.text = unit.DisplayName;
            _detailAttribute.text =
                $"{AttributeName(unit.Attribute)} · {RarityName(unit.Rarity)} · {AttackStyleName(unit.AttackStyle)}";
            string star3 = unit.HasStar3Skill
                ? $"★3 {unit.Star3SkillName} · {unit.Star3SkillCooldown:0.#}초"
                : "★3 스킬 없음";
            _detailStats.text =
                $"★1 HP {unit.BaseMaxHp:N0} · 공격 {unit.BaseDamage:N0} · 초당 공격 {unit.AttacksPerSecond:0.##}\n" +
                $"사거리 {unit.AttackRange:0.##} · 이동 {unit.MoveSpeed:0.##}\n" +
                star3;
        }

        SummonUnitData Find(string unitId)
        {
            if (_units == null)
                return null;
            foreach (SummonUnitData unit in _units)
                if (unit != null && unit.UnitId == unitId)
                    return unit;
            return null;
        }

        void OnProgressionChanged(SummonerProgressionSnapshot _) => Refresh();
        void OnClose() => CloseRequested?.Invoke();

        static string AttributeName(MonsterAttribute attribute) => attribute switch
        {
            MonsterAttribute.Fire => "화염",
            MonsterAttribute.Ice => "빙결",
            MonsterAttribute.Nature => "자연",
            _ => "무",
        };

        static string RarityName(SummonUnitRarity rarity) => rarity switch
        {
            SummonUnitRarity.Rare => "희귀",
            SummonUnitRarity.Legendary => "전설",
            _ => "일반",
        };

        static string AttackStyleName(SummonAttackStyle style) => style switch
        {
            SummonAttackStyle.Projectile => "원거리",
            SummonAttackStyle.Area => "범위",
            SummonAttackStyle.Support => "지원",
            SummonAttackStyle.Piercing => "관통",
            _ => "근접",
        };
    }
}
