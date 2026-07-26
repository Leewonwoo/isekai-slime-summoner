using System;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public readonly struct SlimeLevelUpViewModel
    {
        public bool IsAvailable { get; }
        public int CurrentLevel { get; }
        public int MaxLevel { get; }
        public int Cost { get; }
        public float CurrentDamageMultiplier { get; }
        public float NextDamageMultiplier { get; }
        public float CurrentAttackSpeedMultiplier { get; }
        public float NextAttackSpeedMultiplier { get; }
        public bool CanPurchase { get; }
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;

        public SlimeLevelUpViewModel(
            int currentLevel,
            int maxLevel,
            int cost,
            float currentDamageMultiplier,
            float nextDamageMultiplier,
            float currentAttackSpeedMultiplier,
            float nextAttackSpeedMultiplier,
            bool canPurchase)
        {
            IsAvailable = true;
            CurrentLevel = Mathf.Max(1, currentLevel);
            MaxLevel = Mathf.Max(CurrentLevel, maxLevel);
            Cost = Mathf.Max(0, cost);
            CurrentDamageMultiplier = Mathf.Max(0f, currentDamageMultiplier);
            NextDamageMultiplier = Mathf.Max(0f, nextDamageMultiplier);
            CurrentAttackSpeedMultiplier = Mathf.Max(0f, currentAttackSpeedMultiplier);
            NextAttackSpeedMultiplier = Mathf.Max(0f, nextAttackSpeedMultiplier);
            CanPurchase = canPurchase;
        }
    }

    /// <summary>벤치 슬롯 탭으로 여는 소환수 상세 팝업.</summary>
    public sealed class SummonUnitDetailView
    {
        readonly VisualElement _overlay;
        readonly VisualElement _backdrop;
        readonly Button _closeButton;
        readonly Image _icon;
        readonly Label _name;
        readonly Label _rank;
        readonly Label _rarity;
        readonly Label _attribute;
        readonly Label _attackStyle;
        readonly Label _damage;
        readonly Label _attackSpeed;
        readonly Label _attackRange;
        readonly Label _moveSpeed;
        readonly Label _star3Skill;
        readonly VisualElement _upgradeSection;
        readonly Label _upgradeLevel;
        readonly Label _upgradeValues;
        readonly Button _upgradeButton;
        readonly LongPressRepeater _upgradeRepeater;

        public bool IsOpen => _overlay != null && !_overlay.ClassListContains("hidden");
        public SummonUnitInstance CurrentInstance { get; private set; }
        public int CurrentQuantity { get; private set; }
        public string CurrentUnitId => CurrentInstance?.Unit?.UnitId;

        public event Action<string> LevelUpRequested;

        public SummonUnitDetailView(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("unit-detail-overlay");
            _backdrop = root.Q<VisualElement>("unit-detail-backdrop");
            _closeButton = root.Q<Button>("unit-detail-close-button");
            _icon = root.Q<Image>("unit-detail-icon");
            _name = root.Q<Label>("unit-detail-name");
            _rank = root.Q<Label>("unit-detail-rank");
            _rarity = root.Q<Label>("unit-detail-rarity");
            _attribute = root.Q<Label>("unit-detail-attribute");
            _attackStyle = root.Q<Label>("unit-detail-attack-style");
            _damage = root.Q<Label>("unit-detail-damage");
            _attackSpeed = root.Q<Label>("unit-detail-attack-speed");
            _attackRange = root.Q<Label>("unit-detail-attack-range");
            _moveSpeed = root.Q<Label>("unit-detail-move-speed");
            _star3Skill = root.Q<Label>("unit-detail-star3-skill");
            _upgradeSection = root.Q<VisualElement>("unit-detail-upgrade");
            _upgradeLevel = root.Q<Label>("unit-detail-upgrade-level");
            _upgradeValues = root.Q<Label>("unit-detail-upgrade-values");
            _upgradeButton = root.Q<Button>("unit-detail-upgrade-button");

            if (_closeButton != null)
                _closeButton.clicked += Hide;
            _backdrop?.RegisterCallback<PointerDownEvent>(OnBackdropPressed);
            if (_upgradeButton != null)
                _upgradeRepeater = new LongPressRepeater(_upgradeButton, OnLevelUpPressed);
        }

        public void Show(
            SummonUnitInstance instance,
            int quantity,
            SlimeLevelUpViewModel levelUp)
        {
            var data = instance?.Unit;
            if (data == null || _overlay == null) return;
            CurrentInstance = instance;
            CurrentQuantity = Mathf.Max(1, quantity);

            if (_icon != null)
            {
                _icon.sprite = data.WorldSpriteAtRank(instance.Rank);
                _icon.scaleMode = ScaleMode.ScaleToFit;
            }
            if (_name != null) _name.text = data.DisplayName;
            if (_rank != null)
            {
                string rankName = SummonRank.FormatStars(instance.Rank);
                _rank.text = $"Lv.{instance.Level} · {rankName} · 보유 x{CurrentQuantity:N0}";
            }
            if (_rarity != null) _rarity.text = RarityName(data.Rarity);
            if (_attribute != null) _attribute.text = AttributeName(data.Attribute);
            if (_attackStyle != null) _attackStyle.text = AttackStyleName(data.AttackStyle);
            if (_damage != null)
                _damage.text = (data.DamageAtRank(instance.Rank) * instance.DamageMultiplier).ToString("0.#");
            if (_attackSpeed != null)
                _attackSpeed.text =
                    $"{data.AttacksPerSecondAtRank(instance.Rank) * instance.AttackSpeedMultiplier:0.##}/초";
            if (_attackRange != null) _attackRange.text = data.AttackRange.ToString("0.##");
            if (_moveSpeed != null) _moveSpeed.text = data.MoveSpeed.ToString("0.##");
            if (_star3Skill != null)
            {
                _star3Skill.text = instance.Rank >= SummonRank.MaxInternalRank
                    ? $"{data.Star3SkillName} · {data.Star3SkillCooldown:0.#}초"
                    : $"★3 해금 · {data.Star3SkillName}";
            }
            BindLevelUp(levelUp);

            _overlay.RemoveFromClassList("hidden");
        }

        public void Hide() => _overlay?.AddToClassList("hidden");

        public void Dispose()
        {
            if (_closeButton != null)
                _closeButton.clicked -= Hide;
            _backdrop?.UnregisterCallback<PointerDownEvent>(OnBackdropPressed);
            _upgradeRepeater?.Dispose();
        }

        void BindLevelUp(SlimeLevelUpViewModel levelUp)
        {
            _upgradeSection?.EnableInClassList("hidden", !levelUp.IsAvailable);
            if (!levelUp.IsAvailable)
                return;

            if (_upgradeLevel != null)
                _upgradeLevel.text = levelUp.IsMaxLevel
                    ? $"슬라임 Lv.{levelUp.CurrentLevel:N0} · MAX"
                    : $"슬라임 Lv.{levelUp.CurrentLevel:N0} → Lv.{levelUp.CurrentLevel + 1:N0}";
            if (_upgradeValues != null)
            {
                _upgradeValues.text =
                    $"공격 {UIFormat.PercentDelta(levelUp.CurrentDamageMultiplier, levelUp.NextDamageMultiplier)}" +
                    $" · 공속 {UIFormat.PercentDelta(levelUp.CurrentAttackSpeedMultiplier, levelUp.NextAttackSpeedMultiplier)}";
            }
            if (_upgradeButton == null)
                return;
            _upgradeButton.text = levelUp.IsMaxLevel ? "MAX" : $"{levelUp.Cost:N0} G";
            _upgradeButton.SetEnabled(levelUp.CanPurchase);
            _upgradeButton.EnableInClassList("btn--disabled", !levelUp.CanPurchase);
        }

        void OnLevelUpPressed()
        {
            string unitId = CurrentUnitId;
            if (!string.IsNullOrWhiteSpace(unitId))
                LevelUpRequested?.Invoke(unitId);
        }

        void OnBackdropPressed(PointerDownEvent evt)
        {
            Hide();
            evt.StopPropagation();
        }

        static string RarityName(SummonUnitRarity rarity) => rarity switch
        {
            SummonUnitRarity.Rare => "희귀",
            SummonUnitRarity.Legendary => "전설",
            _ => "일반",
        };

        static string AttributeName(MonsterAttribute attribute) => attribute switch
        {
            MonsterAttribute.Fire => "화염",
            MonsterAttribute.Ice => "빙결",
            MonsterAttribute.Nature => "자연",
            MonsterAttribute.Lightning => "전기",
            MonsterAttribute.Water => "물",
            MonsterAttribute.Wind => "바람",
            _ => "무속성",
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
