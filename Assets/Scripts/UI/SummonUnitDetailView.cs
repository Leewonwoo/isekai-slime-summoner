using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
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

        public bool IsOpen => _overlay != null && !_overlay.ClassListContains("hidden");

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

            if (_closeButton != null)
                _closeButton.clicked += Hide;
            _backdrop?.RegisterCallback<PointerDownEvent>(OnBackdropPressed);
        }

        public void Show(SummonUnitInstance instance, int quantity = 1)
        {
            var data = instance?.Unit;
            if (data == null || _overlay == null) return;

            if (_icon != null)
            {
                _icon.sprite = data.Icon != null ? data.Icon : data.WorldSprite;
                _icon.scaleMode = ScaleMode.ScaleToFit;
            }
            if (_name != null) _name.text = data.DisplayName;
            if (_rank != null)
            {
                string rankName = instance.Rank <= 0 ? "기본" : $"★{instance.Rank}";
                _rank.text = $"Lv.{instance.Level} · {rankName} · 보유 x{Mathf.Max(1, quantity):N0}";
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

            _overlay.RemoveFromClassList("hidden");
        }

        public void Hide() => _overlay?.AddToClassList("hidden");

        public void Dispose()
        {
            if (_closeButton != null)
                _closeButton.clicked -= Hide;
            _backdrop?.UnregisterCallback<PointerDownEvent>(OnBackdropPressed);
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
