#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CrossDefense.Core;
using CrossDefense.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class MetaCollectionSystemsTests
    {
        readonly List<Object> _runtimeObjects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _runtimeObjects.Count - 1; i >= 0; i--)
                if (_runtimeObjects[i] != null) Object.DestroyImmediate(_runtimeObjects[i]);
            _runtimeObjects.Clear();
        }

        [Test]
        public void SlimeUnlocks_UseApprovedLevelBoundariesAndUpdateDuringRun()
        {
            int level = 1;
            SummonUnitData punch = Unit("punch", 1);
            SummonUnitData water = Unit("water", 2);
            SummonUnitData flame = Unit("flame", 6);
            SummonUnitData ice = Unit("ice", 8);
            SummonUnitData green = Unit("green", 12);
            SummonUnitData buff = Unit("buff", 16);
            SummonUnitData explosion = Unit("explosion", 20);
            SummonUnitData freeze = Unit("freeze", 24);
            var manager = new SummonManager(null,
                new[] { punch, water, flame, ice, green, buff, explosion, freeze },
                currencyChance: 0f,
                directRankOneChance: 0f,
                currencyReward: 0,
                benchCapacity: 12,
                summonerLevelProvider: () => level);

            Assert.That(manager.IsUnitUnlocked("punch"), Is.True);
            Assert.That(manager.IsUnitUnlocked("water"), Is.False);
            level = 2;
            Assert.That(manager.IsUnitUnlocked("water"), Is.True);
            level = 5;
            Assert.That(manager.IsUnitUnlocked("flame"), Is.False);
            level = 6;
            Assert.That(manager.IsUnitUnlocked("flame"), Is.True);
            level = 8;
            Assert.That(manager.IsUnitUnlocked("ice"), Is.True);
            level = 12;
            Assert.That(manager.IsUnitUnlocked("green"), Is.True);
            level = 16;
            Assert.That(manager.IsUnitUnlocked("buff"), Is.True);
            level = 20;
            Assert.That(manager.IsUnitUnlocked("explosion"), Is.True);
            Assert.That(manager.IsUnitUnlocked("freeze"), Is.False);
            level = 24;
            Assert.That(manager.IsUnitUnlocked("freeze"), Is.True);
        }

        [Test]
        public void MonsterCodex_EncounterAndKillsRestoreAndIgnoreUnknownIds()
        {
            MonsterData known = Monster("known");
            MonsterCatalog catalog = Track(MonsterCatalog.CreateRuntime(new[] { known }));
            string saved = null;
            var progression = new MonsterCodexProgression(catalog, save: json => saved = json);

            Assert.That(progression.RecordEncounter(known), Is.True);
            Assert.That(progression.RecordEncounter(known), Is.False);
            Assert.That(progression.RecordKill(known), Is.True);
            Assert.That(progression.RecordKill(known), Is.True);

            var restored = new MonsterCodexProgression(catalog, () => saved);
            MonsterCodexEntry entry = restored.Get("known");
            Assert.That(entry.Encountered, Is.True);
            Assert.That(entry.Kills, Is.EqualTo(2));

            string mixedJson = "{\"entries\":[{\"monsterId\":\"known\",\"encountered\":true,\"kills\":3},{\"monsterId\":\"missing\",\"encountered\":true,\"kills\":99}]}";
            restored = new MonsterCodexProgression(catalog, () => mixedJson);
            Assert.That(restored.Get("known").Kills, Is.EqualTo(3));
            Assert.That(restored.Get("missing").Encountered, Is.False);
        }

        [Test]
        public void Equipment_OwnershipEquipAndPartialInvalidSaveRestore()
        {
            EquipmentCatalog catalog = Track(EquipmentCatalog.CreateRuntimeDefault());
            EquipmentData weaponOne = catalog.Equipment.First(item => item.Slot == EquipmentSlot.Weapon);
            EquipmentData weaponTwo = catalog.Equipment.Last(item => item.Slot == EquipmentSlot.Weapon);
            string saved = null;
            var progression = new EquipmentProgression(catalog, save: json => saved = json);

            Assert.That(progression.Acquire(weaponOne), Is.True);
            Assert.That(progression.Equipped(EquipmentSlot.Weapon), Is.SameAs(weaponOne));
            Assert.That(progression.Acquire(weaponTwo), Is.True);
            Assert.That(progression.Equipped(EquipmentSlot.Weapon), Is.SameAs(weaponOne));
            Assert.That(progression.TryEquip(weaponTwo.EquipmentId), Is.True);

            var restored = new EquipmentProgression(catalog, () => saved);
            Assert.That(restored.IsOwned(weaponOne.EquipmentId), Is.True);
            Assert.That(restored.IsOwned(weaponTwo.EquipmentId), Is.True);
            Assert.That(restored.Equipped(EquipmentSlot.Weapon).EquipmentId, Is.EqualTo(weaponTwo.EquipmentId));

            string mixedJson = $"{{\"ownedIds\":[\"{weaponOne.EquipmentId}\",\"missing\"],\"weaponId\":\"{weaponOne.EquipmentId}\",\"armorId\":\"missing\"}}";
            restored = new EquipmentProgression(catalog, () => mixedJson);
            Assert.That(restored.OwnedIds, Is.EquivalentTo(new[] { weaponOne.EquipmentId }));
            Assert.That(restored.Equipped(EquipmentSlot.Weapon), Is.SameAs(weaponOne));
            Assert.That(restored.Equipped(EquipmentSlot.Armor), Is.Null);
        }

        [Test]
        public void Merchant_OffersAreDeterministicExcludeOwnedAndFallbackWhenComplete()
        {
            EquipmentCatalog equipmentCatalog = Track(EquipmentCatalog.CreateRuntimeDefault());
            MerchantCatalog merchantCatalog = Track(MerchantCatalog.CreateRuntimeDefault(equipmentCatalog));
            var equipment = new EquipmentProgression(equipmentCatalog);
            var relics = new RunRelicInventory();
            var first = new MerchantManager(null, merchantCatalog, equipment, relics);
            var second = new MerchantManager(null, merchantCatalog, equipment, relics);

            first.Open(18, 2026);
            second.Open(18, 2026);
            Assert.That(first.Offers.Select(offer => offer.Id), Is.EqualTo(second.Offers.Select(offer => offer.Id)));
            Assert.That(first.Offers.Count, Is.EqualTo(3));
            Assert.That(first.Offers[0].Category, Is.EqualTo(MerchantProductCategory.Equipment));

            foreach (EquipmentData item in equipmentCatalog.Equipment) equipment.Acquire(item);
            first.Open(18, 2026);
            Assert.That(first.Offers[0].Category, Is.EqualTo(MerchantProductCategory.Consumable));
        }

        [Test]
        public void Merchant_PurchaseIsAtomicAndCannotBeRepeated()
        {
            EquipmentCatalog equipmentCatalog = Track(EquipmentCatalog.CreateRuntimeDefault());
            MerchantCatalog merchantCatalog = Track(MerchantCatalog.CreateRuntimeDefault(equipmentCatalog));
            var equipment = new EquipmentProgression(equipmentCatalog);
            var relics = new RunRelicInventory();
            GameObject host = Track(new GameObject("merchant-test-host"));
            host.SetActive(false);
            GameManager game = host.AddComponent<GameManager>();
            var merchant = new MerchantManager(game, merchantCatalog, equipment, relics);
            merchant.Open(8, 2026);

            Assert.That(game.Gold, Is.Zero);
            Assert.That(merchant.TryPurchase(0), Is.False);
            Assert.That(merchant.Offers[0].Purchased, Is.False);

            FieldInfo goldField = typeof(GameManager).GetField("_gold",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(goldField, Is.Not.Null);
            goldField.SetValue(game, 1000);
            int expectedGold = 1000 - merchant.Offers[0].Price;

            Assert.That(merchant.TryPurchase(0), Is.True);
            Assert.That(game.Gold, Is.EqualTo(expectedGold));
            Assert.That(merchant.Offers[0].Purchased, Is.True);
            Assert.That(equipment.IsOwned(merchant.Offers[0].Id), Is.True);

            Assert.That(merchant.TryPurchase(0), Is.False);
            Assert.That(game.Gold, Is.EqualTo(expectedGold));
        }

        [Test]
        public void RunRelics_DoNotStackDuplicatesAndResetWithNewInventory()
        {
            EquipmentCatalog equipment = Track(EquipmentCatalog.CreateRuntimeDefault());
            MerchantCatalog catalog = Track(MerchantCatalog.CreateRuntimeDefault(equipment));
            RunRelicDefinition damage = catalog.Relics.First(item => item.Effect == RunRelicEffect.AllDamage);
            var inventory = new RunRelicInventory();

            Assert.That(inventory.TryAdd(damage), Is.True);
            Assert.That(inventory.TryAdd(damage), Is.False);
            Assert.That(inventory.DamageMultiplier, Is.EqualTo(1f + damage.Value).Within(0.0001f));

            inventory.Clear();
            Assert.That(inventory.Owned, Is.Empty);
            Assert.That(inventory.DamageMultiplier, Is.EqualTo(1f));
        }

        [Test]
        public void RushClearGold_IsGrantedExactlyOncePerWave()
        {
            GameObject host = Track(new GameObject("rush-bonus-test-host"));
            host.SetActive(false);
            GameManager game = host.AddComponent<GameManager>();

            game.GrantWaveClearGoldBonus(8, 40);
            game.GrantWaveClearGoldBonus(8, 40);
            game.GrantWaveClearGoldBonus(18, 70);

            Assert.That(game.Gold, Is.EqualTo(110));
        }

        [Test]
        public void StageOne_HasExactlyFiveApprovedRushWaves()
        {
            StageTimeline timeline = AssetDatabase.LoadAssetAtPath<StageTimeline>(
                "Assets/Data/StageTimelines/Stage_01.asset");
            int[] expectedWaves = { 8, 18, 28, 38, 48 };
            int[] expectedBonuses = { 40, 70, 100, 130, 160 };

            Assert.That(timeline, Is.Not.Null);
            Assert.That(timeline.Waves.Count, Is.EqualTo(50));
            int[] actual = timeline.Waves.Select((wave, index) => new { wave, index })
                .Where(item => item.wave.IsRush).Select(item => item.index + 1).ToArray();
            Assert.That(actual, Is.EqualTo(expectedWaves));
            for (int i = 0; i < expectedWaves.Length; i++)
            {
                StageWave wave = timeline.Waves[expectedWaves[i] - 1];
                Assert.That(wave.ClearGoldBonus, Is.EqualTo(expectedBonuses[i]));
                Assert.That(wave.PostClearEvent, Is.EqualTo(PostWaveEvent.Merchant));
                Assert.That(wave.MaxLivingMonsters, Is.EqualTo(64));
            }
        }

        SummonUnitData Unit(string id, int unlockLevel)
        {
            SummonUnitData unit = Track(SummonUnitData.CreatePrototype(id, id, SummonUnitRarity.Common));
            unit.ConfigurePrototypeUnlockLevel(unlockLevel);
            return unit;
        }

        MonsterData Monster(string id) => Track(MonsterData.CreatePrototype(
            id, id, MonsterShape.Grunt, MonsterAttribute.None, 10, 1f, 1, 1));

        T Track<T>(T item) where T : Object
        {
            _runtimeObjects.Add(item);
            return item;
        }
    }
}
#endif
