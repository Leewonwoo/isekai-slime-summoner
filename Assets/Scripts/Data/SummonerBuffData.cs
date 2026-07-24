using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    public enum SummonerBuffId
    {
        Aegis,
        LegionCommand,
        LifeBlessing,
        ElementalResonance,
        TimeAcceleration,
    }

    public readonly struct SummonerBuffDefinition
    {
        public SummonerBuffId Id { get; }
        public string DisplayName { get; }
        public int UnlockLevel { get; }
        public float Cooldown { get; }
        public float Duration { get; }
        public string Description { get; }

        public SummonerBuffDefinition(
            SummonerBuffId id,
            string displayName,
            int unlockLevel,
            float cooldown,
            float duration,
            string description)
        {
            Id = id;
            DisplayName = displayName ?? string.Empty;
            UnlockLevel = Mathf.Max(1, unlockLevel);
            Cooldown = Mathf.Max(0.1f, cooldown);
            Duration = Mathf.Max(0f, duration);
            Description = description ?? string.Empty;
        }
    }

    public static class SummonerBuffCatalog
    {
        public const int MaxEquipped = 3;

        static readonly SummonerBuffDefinition[] Definitions =
        {
            new(
                SummonerBuffId.Aegis,
                "소환사 보호막",
                1,
                32f,
                6f,
                "최대 HP 35% 보호막 · 6초"),
            new(
                SummonerBuffId.LegionCommand,
                "군단 지휘",
                4,
                36f,
                8f,
                "모든 슬라임 공격력 +20% · 공격속도 +25% · 8초"),
            new(
                SummonerBuffId.LifeBlessing,
                "생명의 가호",
                8,
                48f,
                8f,
                "소환사 HP 2%/초 · 슬라임 HP 4%/초 회복 · 8초"),
            new(
                SummonerBuffId.ElementalResonance,
                "정령 공명",
                12,
                42f,
                8f,
                "다음 신물 스킬 피해 +30% · 범위 +15% · 상태이상 +25%"),
            new(
                SummonerBuffId.TimeAcceleration,
                "시간 가속",
                16,
                55f,
                8f,
                "다른 장착 스킬 쿨다운 회복속도 ×2 · 8초"),
        };

        public static IReadOnlyList<SummonerBuffDefinition> All => Definitions;

        public static SummonerBuffDefinition Get(SummonerBuffId id)
        {
            for (int i = 0; i < Definitions.Length; i++)
                if (Definitions[i].Id == id)
                    return Definitions[i];
            return Definitions[0];
        }

        public static bool IsUnlocked(SummonerBuffId id, int summonerLevel) =>
            Enum.IsDefined(typeof(SummonerBuffId), id) &&
            summonerLevel >= Get(id).UnlockLevel;
    }
}
