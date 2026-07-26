using System;
using CrossDefense.Data;
using CrossDefense.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CrossDefense.Tests.EditMode
{
    public sealed class GameplayIconLibraryTests
    {
        [Test]
        public void GeneratedCatalogIcons_LoadForEveryGameplayDefinition()
        {
            foreach (PermanentTraitType type in Enum.GetValues(typeof(PermanentTraitType)))
                AssertIcon(GameplayIconLibrary.Trait(type), $"trait {type}");

            EquipmentCatalog equipment = AssetDatabase.LoadAssetAtPath<EquipmentCatalog>(
                "Assets/Data/EquipmentCatalog_Default.asset");
            Assert.That(equipment, Is.Not.Null);
            foreach (EquipmentData item in equipment.Equipment)
                AssertIcon(GameplayIconLibrary.Equipment(item), $"equipment {item.EquipmentId}");

            foreach (RelicFamily family in Enum.GetValues(typeof(RelicFamily)))
            {
                if (family == RelicFamily.None)
                    continue;
                for (int rank = 1; rank <= 3; rank++)
                    AssertIcon(GameplayIconLibrary.Relic(family, rank), $"relic {family} rank {rank}");
            }

            foreach (SummonerSkillId id in Enum.GetValues(typeof(SummonerSkillId)))
                AssertIcon(GameplayIconLibrary.Skill(id), $"skill {id}");
            foreach (SummonerBuffId id in Enum.GetValues(typeof(SummonerBuffId)))
                AssertIcon(GameplayIconLibrary.Buff(id), $"buff {id}");

            RunRewardCatalog rewards = RunRewardCatalog.CreateRuntimeDefault();
            try
            {
                foreach (RunRewardDefinition reward in rewards.Rewards)
                    AssertIcon(GameplayIconLibrary.Reward(reward), $"reward {reward.RewardId}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rewards);
            }

            MerchantCatalog merchant = AssetDatabase.LoadAssetAtPath<MerchantCatalog>(
                "Assets/Data/MerchantCatalog_Default.asset");
            Assert.That(merchant, Is.Not.Null);
            foreach (RunRelicDefinition loot in merchant.Relics)
                AssertIcon(GameplayIconLibrary.Loot(loot.Id), $"loot {loot.Id}");
        }

        static void AssertIcon(Sprite sprite, string label)
        {
            Assert.That(sprite, Is.Not.Null, $"Missing generated icon for {label}.");
            Assert.That(sprite.rect.width, Is.EqualTo(128f), $"{label} width");
            Assert.That(sprite.rect.height, Is.EqualTo(128f), $"{label} height");
        }
    }
}
