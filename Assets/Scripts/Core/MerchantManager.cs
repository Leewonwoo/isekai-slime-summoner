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
        public RelicDefinition Relic { get; }
        public RunRelicDefinition Trophy { get; }
        public RelicFamily RelicFamily { get; }
        public int RelicTargetRank { get; }
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
        public MerchantOffer(RelicDefinition item, int targetRank)
        {
            Category = MerchantProductCategory.Relic; Relic = item;
            RelicFamily = item.Family;
            RelicTargetRank = Mathf.Clamp(targetRank, 1, item.MaxRank);
            RelicRankDefinition rank = item.Rank(RelicTargetRank);
            Id = $"{item.Id}-{RelicTargetRank}";
            DisplayName = $"{rank.DisplayName} ★{RelicTargetRank}";
            Description = RelicTargetRank == 1
                ? $"신규 신물 획득 · {rank.SkillName}\n{rank.Description}"
                : $"신물 승급 ★{RelicTargetRank - 1} → ★{RelicTargetRank} · {rank.SkillName}\n{rank.Description}";
            Price = rank.Price;
        }
        public MerchantOffer(RunRelicDefinition item)
        {
            Category = MerchantProductCategory.Trophy; Trophy = item;
            Id = item.Id; DisplayName = item.DisplayName; Description = item.Description; Price = item.Price;
        }
    }

    public sealed class MerchantManager
    {
        const int OfferCount = 3;
        readonly GameManager _game;
        readonly MerchantCatalog _catalog;
        readonly EquipmentProgression _equipment;
        readonly RelicProgression _relics;
        readonly RunRelicInventory _trophies;
        readonly List<MerchantOffer> _offers = new(OfferCount);
        readonly System.Random _random;
        public IReadOnlyList<MerchantOffer> Offers => _offers;
        public bool IsOpen { get; private set; }
        public event Action Changed;

        public MerchantManager(GameManager game, MerchantCatalog catalog,
            EquipmentProgression equipment, RelicProgression relics, RunRelicInventory trophies,
            System.Random random = null)
        {
            _game = game; _catalog = catalog; _equipment = equipment;
            _relics = relics; _trophies = trophies;
            _random = random ?? new System.Random(unchecked(
                Environment.TickCount ^ Guid.NewGuid().GetHashCode()));
        }

        public void Open(int waveNumber, int stageSeed)
        {
            _offers.Clear();
            List<MerchantOffer> candidates = BuildOfferPool();
            int offerCount = Mathf.Min(OfferCount, candidates.Count);
            for (int i = 0; i < offerCount; i++)
            {
                int candidateIndex = _random.Next(candidates.Count);
                _offers.Add(candidates[candidateIndex]);
                candidates.RemoveAt(candidateIndex);
            }
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
            if (offer.Relic != null) return _relics.TryAcquire(offer.RelicFamily);
            if (offer.Trophy != null) return _trophies.TryAdd(offer.Trophy);
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

        List<MerchantOffer> BuildOfferPool()
        {
            var candidates = new List<MerchantOffer>();

            if (_catalog?.EquipmentCatalog?.Equipment != null)
                foreach (EquipmentData item in _catalog.EquipmentCatalog.Equipment)
                    if (item != null && _equipment != null && !_equipment.IsOwned(item.EquipmentId))
                        candidates.Add(new MerchantOffer(item));

            if (_relics?.Catalog?.Relics != null)
                foreach (RelicDefinition relic in _relics.Catalog.Relics)
                    if (relic != null && _relics.CanAcquire(relic.Family))
                        candidates.Add(new MerchantOffer(
                            relic,
                            _relics.Rank(relic.Family) + 1));

            if (_catalog?.Consumables != null)
                foreach (ConsumableDefinition consumable in _catalog.Consumables)
                    if (consumable != null)
                        candidates.Add(new MerchantOffer(consumable));

            if (_catalog?.Relics != null && _trophies != null)
                foreach (RunRelicDefinition trophy in _catalog.Relics)
                    if (trophy != null && !_trophies.Contains(trophy.Id))
                        candidates.Add(new MerchantOffer(trophy));

            return candidates;
        }
    }
}
