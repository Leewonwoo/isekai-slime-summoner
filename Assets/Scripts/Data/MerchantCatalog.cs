using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum MerchantProductCategory { Equipment, Relic, Consumable, Trophy }
    public enum ConsumableEffect { HealCorePercent, SummonContracts, RandomSlime }
    public enum RunRelicEffect { AllDamage, AllAttackSpeed, GoldReward, SummonerMaxHp, WaveContract, JackpotChance }

    [Serializable]
    public sealed class ConsumableDefinition
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] string description;
        [SerializeField] ConsumableEffect effect;
        [SerializeField] float value;
        [SerializeField] int price;
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public ConsumableEffect Effect => effect;
        public float Value => value;
        public int Price => Mathf.Max(1, price);

        public ConsumableDefinition(string itemId, string title, string detail,
            ConsumableEffect itemEffect, float amount, int goldPrice)
        {
            id = itemId; displayName = title; description = detail;
            effect = itemEffect; value = amount; price = goldPrice;
        }
    }

    [Serializable]
    public sealed class RunRelicDefinition
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] string description;
        [SerializeField] RunRelicEffect effect;
        [SerializeField] float value;
        [SerializeField] int price;
        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public RunRelicEffect Effect => effect;
        public float Value => Mathf.Max(0f, value);
        public int Price => Mathf.Max(1, price);

        public RunRelicDefinition(string itemId, string title, string detail,
            RunRelicEffect relicEffect, float amount, int goldPrice)
        {
            id = itemId; displayName = title; description = detail;
            effect = relicEffect; value = amount; price = goldPrice;
        }
    }

    [CreateAssetMenu(fileName = "MerchantCatalog", menuName = "Isekai Slime Summoner/Data/Merchant Catalog", order = 43)]
    public sealed class MerchantCatalog : ScriptableObject
    {
        [SerializeField] EquipmentCatalog equipmentCatalog;
        [SerializeField] List<ConsumableDefinition> consumables = new();
        [SerializeField] List<RunRelicDefinition> relics = new();
        public EquipmentCatalog EquipmentCatalog => equipmentCatalog;
        public IReadOnlyList<ConsumableDefinition> Consumables => consumables;
        public IReadOnlyList<RunRelicDefinition> Relics => relics;

        public RunRelicDefinition FindRelic(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || relics == null)
                return null;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null && relics[i].Id == id)
                    return relics[i];
            return null;
        }

        public static MerchantCatalog CreateRuntimeDefault(EquipmentCatalog equipment)
        {
            var catalog = CreateInstance<MerchantCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.equipmentCatalog = equipment;
            catalog.consumables = new List<ConsumableDefinition>
            {
                new("heal-core", "응급 치료", "소환사 최대 HP의 35% 회복", ConsumableEffect.HealCorePercent, 0.35f, 35),
                new("contracts", "계약서 묶음", "용병 계약서 2장 획득", ConsumableEffect.SummonContracts, 2f, 45),
                new("random-slime", "슬라임 알", "해금된 무작위 ★1 슬라임 획득", ConsumableEffect.RandomSlime, 1f, 60),
            };
            catalog.relics = new List<RunRelicDefinition>
            {
                new("relic-power", "광전사의 인장", "모든 공격력 +20%", RunRelicEffect.AllDamage, 0.20f, 90),
                new("relic-haste", "가속 토템", "모든 공격속도 +15%", RunRelicEffect.AllAttackSpeed, 0.15f, 90),
                new("relic-gold", "황금 자석", "처치 골드 +25%", RunRelicEffect.GoldReward, 0.25f, 80),
                new("relic-vitality", "생명의 돌", "소환사 최대 HP +25%", RunRelicEffect.SummonerMaxHp, 0.25f, 80),
                new("relic-contract", "계약의 인장", "일반 웨이브 계약서 보상 +1", RunRelicEffect.WaveContract, 1f, 100),
                new("relic-luck", "행운 부적", "★2 직행 확률 +5%", RunRelicEffect.JackpotChance, 0.05f, 100),
            };
            return catalog;
        }
    }
}
