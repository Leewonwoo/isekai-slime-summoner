using System;
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine.UIElements;

namespace CrossDefense.UI
{
    public sealed class MerchantModalController
    {
        sealed class Card
        {
            public Label Category;
            public Label Name;
            public Label Description;
            public Label State;
            public Button Buy;
        }

        readonly VisualElement _overlay;
        readonly Button _close;
        readonly List<Card> _cards = new(3);
        MerchantManager _merchant;
        public bool IsVisible => !_overlay.ClassListContains("hidden");
        public event Action CloseRequested;
        public event Action<int> PurchaseRequested;

        public MerchantModalController(VisualElement root)
        {
            _overlay = root.Q<VisualElement>("merchant-overlay");
            _close = root.Q<Button>("merchant-close-button");
            _close.clicked += OnClose;
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                VisualElement cardRoot = root.Q<VisualElement>($"merchant-card-{i}");
                var card = new Card
                {
                    Category = cardRoot.Q<Label>("merchant-card-category"),
                    Name = cardRoot.Q<Label>("merchant-card-name"),
                    Description = cardRoot.Q<Label>("merchant-card-description"),
                    State = cardRoot.Q<Label>("merchant-card-state"),
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
            _overlay.EnableInClassList("hidden", !open);
            if (!open) return;
            for (int i = 0; i < _cards.Count; i++)
            {
                MerchantOffer offer = i < _merchant.Offers.Count ? _merchant.Offers[i] : null;
                Card card = _cards[i];
                if (offer == null) continue;
                card.Category.text = CategoryName(offer.Category);
                card.Name.text = offer.DisplayName;
                card.Description.text = offer.Description;
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
            MerchantProductCategory.Equipment => "영구 장비",
            MerchantProductCategory.Relic => "런 유물",
            _ => "소모품",
        };
    }
}
