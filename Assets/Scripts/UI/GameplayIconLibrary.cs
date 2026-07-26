using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEngine;

namespace CrossDefense.UI
{
    /// <summary>
    /// 생성된 카탈로그 아이콘을 데이터 ID와 연결하는 단일 런타임 진입점.
    /// 기존 에셋의 직렬화 필드가 비어 있어도 Resources 경로로 안전하게 보완한다.
    /// </summary>
    public static class GameplayIconLibrary
    {
        const string Root = "CatalogIcons/";
        static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite Trait(PermanentTraitType type) =>
            Load($"Traits/icon_trait_{Snake(type.ToString())}");

        public static Sprite Equipment(EquipmentData item)
        {
            if (item == null)
                return null;
            if (item.Icon != null)
                return item.Icon;
            return Load($"Equipment/icon_equipment_{NormalizeId(item.EquipmentId)}");
        }

        public static Sprite Relic(RelicFamily family, int rank)
        {
            string[] paths = family switch
            {
                RelicFamily.Fire => new[]
                {
                    "fire_red_staff", "fire_extinction_staff", "fire_armageddon",
                },
                RelicFamily.Ice => new[]
                {
                    "ice_knuckle", "ice_cold_snap", "ice_blizzard_horn",
                },
                RelicFamily.Lightning => new[]
                {
                    "lightning_shuriken_sword", "lightning_thunder_sword", "lightning_kusanagi",
                },
                RelicFamily.Water => new[]
                {
                    "water_fizz_spear", "water_kraken", "water_poseidon_spear",
                },
                RelicFamily.Wind => new[]
                {
                    "wind_gale_fan", "wind_sky_splitter_fan", "wind_palm_leaf_fan",
                },
                _ => null,
            };
            if (paths == null)
                return null;
            int index = Mathf.Clamp(rank, 1, paths.Length) - 1;
            return Load($"Relics/icon_relic_{paths[index]}");
        }

        public static Sprite Skill(SummonerSkillId id)
        {
            string name = id switch
            {
                SummonerSkillId.Meteor => "meteor_v2",
                SummonerSkillId.IceWall => "ice_wall_v2",
                SummonerSkillId.Aegis => "aegis_active_v2",
                SummonerSkillId.ArcaneBurst => "arcane_burst",
                SummonerSkillId.LightningStrike => "lightning_strike",
                SummonerSkillId.WaterBurst => "water_burst",
                SummonerSkillId.Gale => "gale",
                _ => "arcane_burst",
            };
            return Load($"Skills/icon_skill_{name}");
        }

        public static Sprite Buff(SummonerBuffId id) =>
            Load($"Skills/icon_buff_{Snake(id.ToString())}");

        public static Sprite Reward(RunRewardDefinition reward)
        {
            if (reward == null)
                return null;
            if (reward.Icon != null)
                return reward.Icon;
            return Reward(reward.RewardId);
        }

        public static Sprite Reward(string rewardId) =>
            string.IsNullOrWhiteSpace(rewardId)
                ? null
                : Load($"TraitRewards/icon_reward_{NormalizeId(rewardId)}");

        public static Sprite Loot(string lootId) =>
            string.IsNullOrWhiteSpace(lootId)
                ? null
                : Load($"Loot/icon_loot_{NormalizeId(lootId)}");

        public static Sprite MerchantOffer(MerchantOffer offer)
        {
            if (offer == null)
                return null;
            if (offer.Equipment != null)
                return Equipment(offer.Equipment);
            if (offer.Relic != null)
                return Relic(offer.RelicFamily, offer.RelicTargetRank);
            if (offer.Trophy != null)
                return Loot(offer.Trophy.Id);
            if (offer.Consumable == null)
                return null;
            return offer.Consumable.Effect switch
            {
                ConsumableEffect.HealCorePercent => Trait(PermanentTraitType.CoreVitality),
                ConsumableEffect.SummonContracts => Reward("summon-triple"),
                ConsumableEffect.RandomSlime => Reward("summon-jackpot-egg"),
                _ => null,
            };
        }

        static Sprite Load(string relativePath)
        {
            string path = Root + relativePath;
            if (Cache.TryGetValue(path, out Sprite cached))
                return cached;
            Sprite sprite = Resources.Load<Sprite>(path);
            Cache[path] = sprite;
            return sprite;
        }

        static string NormalizeId(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant().Replace('-', '_').Replace(' ', '_');

        static string Snake(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var result = new System.Text.StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsUpper(c) && i > 0)
                    result.Append('_');
                result.Append(char.ToLowerInvariant(c));
            }
            return result.ToString();
        }
    }
}
