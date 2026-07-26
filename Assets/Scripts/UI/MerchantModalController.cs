using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class MerchantModalController
    {
        const int CardCount = 3;

        sealed class Card
        {
            public VisualElement Root;
            public Label Category;
            public Label Name;
            public Label Description;
            public Label State;
            public VisualElement Icon;
            public Button Buy;
        }

        readonly VisualElement _overlay;
        readonly Button _close;
        readonly List<Card> _cards = new(CardCount);
        MerchantManager _merchant;
        const string HiddenClass = "modal-overlay--hidden";
        public bool IsVisible => !_overlay.ClassListContains(HiddenClass);
        public event Action CloseRequested;
        public event Action<int> PurchaseRequested;

        public MerchantModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("merchant-overlay");
            _close = root.Q<Button>("merchant-close-button");
            _close.clicked += OnClose;
            for (int i = 0; i < CardCount; i++)
            {
                int index = i;
                VisualElement cardRoot = root.Q<VisualElement>($"merchant-card-{i}");
                var card = new Card
                {
                    Root = cardRoot,
                    Category = cardRoot.Q<Label>("merchant-card-category"),
                    Name = cardRoot.Q<Label>("merchant-card-name"),
                    Description = cardRoot.Q<Label>("merchant-card-description"),
                    State = cardRoot.Q<Label>("merchant-card-state"),
                    Icon = cardRoot.Q<VisualElement>("merchant-card-icon"),
                    Buy = cardRoot.Q<Button>("merchant-card-buy"),
                };
                card.Buy.clicked += () => PurchaseRequested?.Invoke(index);
                _cards.Add(card);
            }
        }

        public void Bind(MerchantManager merchant)
        {
            if (_merchant != null) _merchant.Changed -= Refresh;
            _merchant = merchant;
            if (_merchant != null) _merchant.Changed += Refresh;
            Refresh();
        }

        public void Dispose()
        {
            _close.clicked -= OnClose;
            if (_merchant != null) _merchant.Changed -= Refresh;
        }

        public void Refresh()
        {
            bool open = _merchant?.IsOpen ?? false;
            _overlay.EnableInClassList(HiddenClass, !open);
            if (!open) return;
            for (int i = 0; i < _cards.Count; i++)
            {
                MerchantOffer offer = i < _merchant.Offers.Count ? _merchant.Offers[i] : null;
                Card card = _cards[i];
                card.Root.style.display = offer == null ? DisplayStyle.None : DisplayStyle.Flex;
                if (offer == null) continue;
                card.Category.text = CategoryName(offer.Category);
                card.Name.text = offer.DisplayName;
                card.Description.text = offer.Description;
                Sprite icon = GameplayIconLibrary.MerchantOffer(offer);
                card.Icon.style.backgroundImage = icon != null
                    ? new StyleBackground(icon)
                    : StyleKeyword.Null;
                bool canBuy = _merchant.CanPurchase(i, out string reason);
                card.State.text = offer.Purchased ? "품절" : canBuy ? string.Empty : reason;
                card.Buy.text = offer.Purchased ? "품절" : $"{offer.Price:N0} G";
                card.Buy.SetEnabled(canBuy);
                card.Buy.EnableInClassList("btn--disabled", !canBuy);
            }
        }

        void OnClose() => CloseRequested?.Invoke();
        static string CategoryName(MerchantProductCategory category) => category switch
        {
            MerchantProductCategory.Equipment => "장비",
            MerchantProductCategory.Relic => "신물",
            MerchantProductCategory.Trophy => "전리품",
            _ => "소모품",
        };
    }
}
