using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum SummonerSkillId
    {
        Meteor,
        IceWall,
        Aegis,
        ArcaneBurst,
        LightningStrike,
        WaterBurst,
        Gale,
    }

    public enum SummonerSkillTargeting
    {
        Point,
        Directional,
        Instant,
    }

    public readonly struct SummonerSkillDefinition
    {
        public SummonerSkillId Id { get; }
        public string DisplayName { get; }
        public int UnlockLevel { get; }
        public SummonerSkillTargeting Targeting { get; }
        public float Cooldown { get; }
        public float DamageMultiplier { get; }
        public float Radius { get; }
        public float Duration { get; }
        public string Description { get; }

        public SummonerSkillDefinition(
            SummonerSkillId id,
            string displayName,
            int unlockLevel,
            SummonerSkillTargeting targeting,
            float cooldown,
            float damageMultiplier,
            float radius,
            float duration,
            string description)
        {
            Id = id;
            DisplayName = displayName;
            UnlockLevel = Mathf.Max(1, unlockLevel);
            Targeting = targeting;
            Cooldown = Mathf.Max(0.1f, cooldown);
            DamageMultiplier = Mathf.Max(0f, damageMultiplier);
            Radius = Mathf.Max(0f, radius);
            Duration = Mathf.Max(0f, duration);
            Description = description ?? string.Empty;
        }
    }

    public static class SummonerSkillCatalog
    {
        static readonly SummonerSkillDefinition[] Definitions =
        {
            new(
                SummonerSkillId.Meteor, "메테오", 1, SummonerSkillTargeting.Point,
                22f, 2.6f, 1.8f, 0f,
                "반경 1.8 · 화염 260% · 연소 30%/초 3초"),
            new(
                SummonerSkillId.IceWall, "얼음벽", 8, SummonerSkillTargeting.Directional,
                26f, 1.2f, 0.55f, 4f,
                "4초 벽 지대 · 빙결 120% · 둔화 60% 2.5초"),
            new(
                SummonerSkillId.Aegis, "소환사 보호막", 1, SummonerSkillTargeting.Instant,
                32f, 0f, 0f, 6f,
                "최대 HP 35% 보호막 · 6초"),
            new(
                SummonerSkillId.ArcaneBurst, "마력 폭발", 1, SummonerSkillTargeting.Point,
                16f, 1.8f, 1.45f, 0f,
                "지정 지점에 마력 180% 범위 피해"),
            new(
                SummonerSkillId.LightningStrike, "낙뢰", 1, SummonerSkillTargeting.Point,
                19f, 2.2f, 1.35f, 0f,
                "지정 지점에 뇌격 220% 범위 피해"),
            new(
                SummonerSkillId.WaterBurst, "수압탄", 1, SummonerSkillTargeting.Point,
                18f, 2f, 1.55f, 0f,
                "지정 지점에 수압 200% 범위 피해"),
            new(
                SummonerSkillId.Gale, "돌풍", 1, SummonerSkillTargeting.Point,
                17f, 1.7f, 1.9f, 0f,
                "지정 지점에 돌풍 170% 범위 피해"),
        };

        public static IReadOnlyList<SummonerSkillDefinition> All => Definitions;

        public static SummonerSkillDefinition Get(SummonerSkillId id)
        {
            for (int i = 0; i < Definitions.Length; i++)
                if (Definitions[i].Id == id)
                    return Definitions[i];
            return Definitions[0];
        }

        public static bool IsUnlocked(SummonerSkillId id, int summonerLevel) =>
            summonerLevel >= Get(id).UnlockLevel;

        public static bool IsRelicSkill(SummonerSkillId id) =>
            id == SummonerSkillId.Meteor || id == SummonerSkillId.IceWall ||
            id == SummonerSkillId.ArcaneBurst || id == SummonerSkillId.LightningStrike ||
            id == SummonerSkillId.WaterBurst || id == SummonerSkillId.Gale;
    }
}
