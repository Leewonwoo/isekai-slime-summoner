using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum RelicFamily
    {
        None,
        Fire,
        Ice,
        Lightning,
        Water,
        Wind,
    }

    [Serializable]
    public sealed class RelicRankDefinition
    {
        [SerializeField] string displayName;
        [SerializeField] string skillName;
        [TextArea] [SerializeField] string description;
        [Min(1)] [SerializeField] int price;

        public string DisplayName => displayName;
        public string SkillName => skillName;
        public string Description => description;
        public int Price => Mathf.Max(1, price);

        public RelicRankDefinition(string name, string activeSkill, string detail, int goldPrice)
        {
            displayName = name;
            skillName = activeSkill;
            description = detail;
            price = goldPrice;
        }
    }

    [Serializable]
    public sealed class RelicDefinition
    {
        [SerializeField] string id;
        [SerializeField] RelicFamily family;
        [SerializeField] SummonerSkillId skillId;
        [SerializeField] bool merchantAvailable = true;
        [SerializeField] List<RelicRankDefinition> ranks = new();

        public string Id => id;
        public RelicFamily Family => family;
        public SummonerSkillId SkillId => skillId;
        public bool MerchantAvailable => merchantAvailable;
        public int MaxRank => ranks?.Count ?? 0;
        public IReadOnlyList<RelicRankDefinition> Ranks => ranks;

        public RelicDefinition(
            string relicId,
            RelicFamily relicFamily,
            SummonerSkillId activeSkill,
            bool soldByMerchant,
            params RelicRankDefinition[] rankDefinitions)
        {
            id = relicId;
            family = relicFamily;
            skillId = activeSkill;
            merchantAvailable = soldByMerchant;
            ranks = new List<RelicRankDefinition>(rankDefinitions ?? Array.Empty<RelicRankDefinition>());
        }

        public RelicRankDefinition Rank(int rank)
        {
            if (ranks == null || ranks.Count == 0)
                return null;
            return ranks[Mathf.Clamp(rank, 1, ranks.Count) - 1];
        }
    }

    [CreateAssetMenu(
        fileName = "RelicCatalog",
        menuName = "Isekai Slime Summoner/Data/Relic Catalog",
        order = 44)]
    public sealed class RelicCatalog : ScriptableObject
    {
        [SerializeField] List<RelicDefinition> relics = CreateDefaultDefinitions();

        public IReadOnlyList<RelicDefinition> Relics => relics;

        public RelicDefinition Find(RelicFamily family)
        {
            if (relics == null)
                return null;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null && relics[i].Family == family)
                    return relics[i];
            return null;
        }

        public static RelicCatalog CreateRuntimeDefault()
        {
            var catalog = CreateInstance<RelicCatalog>();
            catalog.hideFlags = HideFlags.HideAndDontSave;
            catalog.relics = CreateDefaultDefinitions();
            return catalog;
        }

        static List<RelicDefinition> CreateDefaultDefinitions() =>
            new()
            {
                new(
                    "fire-relic", RelicFamily.Fire, SummonerSkillId.Meteor, true,
                    new RelicRankDefinition("붉은 지팡이", "메테오", "화염 운석을 떨어뜨립니다.", 100),
                    new RelicRankDefinition("멸화의 지팡이", "중력유성", "더 넓고 강한 화염 운석을 떨어뜨립니다.", 180),
                    new RelicRankDefinition("아마겟돈", "폭류유성", "거대한 화염 운석으로 전장을 휩씁니다.", 320)),
                new(
                    "ice-relic", RelicFamily.Ice, SummonerSkillId.IceWall, true,
                    new RelicRankDefinition("아이스 너클", "얼음벽", "적을 둔화시키는 얼음벽을 만듭니다.", 100),
                    new RelicRankDefinition("콜드 스냅", "빙하낙하", "더 오래 유지되는 빙하를 소환합니다.", 180),
                    new RelicRankDefinition("블리자드혼", "블리자드", "넓은 범위를 얼어붙게 만듭니다.", 320)),
                new(
                    "lightning-relic", RelicFamily.Lightning, SummonerSkillId.LightningStrike, true,
                    new RelicRankDefinition("뇌전 수리검", "낙뢰", "지정한 지점에 낙뢰를 내립니다.", 100),
                    new RelicRankDefinition("천뢰검", "뇌운폭격", "강한 뇌격으로 넓은 범위를 공격합니다.", 180),
                    new RelicRankDefinition("쿠사나기 검", "류진낙하", "폭발적인 천둥으로 전장을 강타합니다.", 320)),
                new(
                    "water-relic", RelicFamily.Water, SummonerSkillId.WaterBurst, true,
                    new RelicRankDefinition("피즈의 창", "수압탄", "압축한 물을 폭발시킵니다.", 100),
                    new RelicRankDefinition("크라켄", "폭포낙하", "거대한 물줄기를 내리꽂습니다.", 180),
                    new RelicRankDefinition("포세이돈의 창", "창해폭류", "거센 파도로 넓은 범위를 휩씁니다.", 320)),
                new(
                    "wind-relic", RelicFamily.Wind, SummonerSkillId.Gale, true,
                    new RelicRankDefinition("질풍선", "돌풍", "응축된 바람을 폭발시킵니다.", 100),
                    new RelicRankDefinition("파천선", "용오름", "강한 회오리로 넓은 범위를 공격합니다.", 180),
                    new RelicRankDefinition("파초선", "천공폭풍", "거대한 폭풍으로 전장을 휩씁니다.", 320)),
            };
    }
}
