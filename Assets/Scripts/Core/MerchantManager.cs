using System;
using System.Collections.Generic;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.Core
{
    public sealed class MerchantOffer
    {
        public MerchantProductCategory Category { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public int Price { get; }
        public EquipmentData Equipment { get; }
        public ConsumableDefinition Consumable { get; }
        public RunRelicDefinition Relic { get; }
        public bool Purchased { get; internal set; }

        public MerchantOffer(EquipmentData item)
        {
            Category = MerchantProductCategory.Equipment; Equipment = item;
            Id = item.EquipmentId; DisplayName = item.DisplayName; Description = item.Description; Price = item.Price;
        }
        public MerchantOffer(ConsumableDefinition item)
        {
            Category = MerchantProductCategory.Consumable; Consumable = item;
            Id = item.Id; DisplayName = item.DisplayName; Description = item.Description; Price = item.Price;
        }
        public MerchantOffer(RunRelicDefinition item)
        {
            Category = MerchantProductCategory.Relic; Relic = item;
            Id = item.Id; DisplayName = item.DisplayName; Description = item.Description; Price = item.Price;
        }
    }

    public sealed class MerchantManager
    {
        readonly GameManager _game;
        readonly MerchantCatalog _catalog;
        readonly EquipmentProgression _equipment;
        readonly RunRelicInventory _relics;
        readonly List<MerchantOffer> _offers = new(3);
        public IReadOnlyList<MerchantOffer> Offers => _offers;
        public bool IsOpen { get; private set; }
        public event Action Changed;

        public MerchantManager(GameManager game, MerchantCatalog catalog,
            EquipmentProgression equipment, RunRelicInventory relics)
        {
            _game = game; _catalog = catalog; _equipment = equipment; _relics = relics;
        }

        public void Open(int waveNumber, int stageSeed)
        {
            _offers.Clear();
            var random = new System.Random(unchecked(stageSeed * 397 ^ waveNumber * 7919));
            EquipmentData equipment = PickEquipment(random);
            if (equipment != null) _offers.Add(new MerchantOffer(equipment));
            else _offers.Add(new MerchantOffer(Pick(_catalog.Consumables, random)));
            _offers.Add(new MerchantOffer(Pick(_catalog.Consumables, random)));
            RunRelicDefinition relic = PickRelic(random);
            if (relic != null) _offers.Add(new MerchantOffer(relic));
            else _offers.Add(new MerchantOffer(Pick(_catalog.Consumables, random)));
            IsOpen = true;
            Changed?.Invoke();
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Changed?.Invoke();
        }

        public bool CanPurchase(int index, out string reason)
        {
            reason = string.Empty;
            if (!IsOpen || index < 0 || index >= _offers.Count) { reason = "상점이 닫혀 있습니다"; return false; }
            MerchantOffer offer = _offers[index];
            if (offer.Purchased) { reason = "품절"; return false; }
            if (_game.Gold < offer.Price) { reason = "골드 부족"; return false; }
            if (offer.Consumable?.Effect == ConsumableEffect.HealCorePercent && _game.CoreHp >= _game.MaxCoreHp)
            { reason = "HP 최대"; return false; }
            if (offer.Consumable?.Effect == ConsumableEffect.RandomSlime && _game.SummonManager.IsBenchFull)
            { reason = "슬롯 부족"; return false; }
            return true;
        }

        public bool TryPurchase(int index)
        {
            if (!CanPurchase(index, out _)) return false;
            MerchantOffer offer = _offers[index];
            if (!_game.TrySpendGold(offer.Price)) return false;
            bool applied = Apply(offer);
            if (!applied)
            {
                _game.AddGold(offer.Price);
                return false;
            }
            offer.Purchased = true;
            Changed?.Invoke();
            return true;
        }

        bool Apply(MerchantOffer offer)
        {
            if (offer.Equipment != null) return _equipment.Acquire(offer.Equipment);
            if (offer.Relic != null) return _relics.TryAdd(offer.Relic);
            if (offer.Consumable == null) return false;
            switch (offer.Consumable.Effect)
            {
                case ConsumableEffect.HealCorePercent:
                    _game.HealCore(_game.MaxCoreHp * offer.Consumable.Value);
                    return true;
                case ConsumableEffect.SummonContracts:
                    _game.AddSummonContracts(Mathf.RoundToInt(offer.Consumable.Value));
                    return true;
                case ConsumableEffect.RandomSlime:
                    return _game.SummonManager.TryGrantRandomReward(0, out _);
                default: return false;
            }
        }

        EquipmentData PickEquipment(System.Random random)
        {
            var candidates = new List<EquipmentData>();
            if (_catalog?.EquipmentCatalog?.Equipment != null)
                foreach (EquipmentData item in _catalog.EquipmentCatalog.Equipment)
                    if (item != null && !_equipment.IsOwned(item.EquipmentId)) candidates.Add(item);
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }

        RunRelicDefinition PickRelic(System.Random random)
        {
            var candidates = new List<RunRelicDefinition>();
            if (_catalog?.Relics != null)
                foreach (RunRelicDefinition relic in _catalog.Relics)
                    if (relic != null && !_relics.Contains(relic.Id)) candidates.Add(relic);
            return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
        }

        static T Pick<T>(IReadOnlyList<T> source, System.Random random) where T : class =>
            source == null || source.Count == 0 ? null : source[random.Next(source.Count)];
    }
}
