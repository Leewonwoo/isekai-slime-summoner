using System.Collections.Generic;
using UnityEngine;

namespace CrossDefense.Data
{
    [CreateAssetMenu(
        fileName = "SkillCatalog",
        menuName = "Isekai Slime Summoner/Data/Skill Catalog",
        order = 23)]
    public sealed class SkillCatalog : ScriptableObject
    {
        [SerializeField] List<SkillData> skills = new();
        [SerializeField] SkillData defaultBasicAttack;

        public IReadOnlyList<SkillData> Skills => skills;
        public SkillData DefaultBasicAttack => defaultBasicAttack;

        public SkillData FindAttack(SummonerAttackArchetype archetype)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                if (skill != null &&
                    skill.Category == SkillCategory.BasicAttack &&
                    skill.AttackArchetype == archetype)
                    return skill;
            }
            return null;
        }

        public SkillData FindActive(SummonerSkillId id)
        {
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                if (skill != null &&
                    skill.Category == SkillCategory.Active &&
                    skill.ActiveSkillId == id)
                    return skill;
            }
            return null;
        }

        public bool IsUnlocked(SummonerSkillId id, int summonerLevel)
        {
            SkillData skill = FindActive(id);
            return skill != null && summonerLevel >= skill.UnlockLevel;
        }

        public bool IsRelicSkill(SummonerSkillId id) =>
            id != SummonerSkillId.Aegis && FindActive(id) != null;

        public bool Validate(out string error)
        {
            if (defaultBasicAttack == null ||
                defaultBasicAttack.Category != SkillCategory.BasicAttack)
            {
                error = "The default basic attack SkillData is missing or invalid.";
                return false;
            }

            var ids = new HashSet<string>();
            var attacks = new HashSet<SummonerAttackArchetype>();
            var active = new HashSet<SummonerSkillId>();
            for (int i = 0; i < skills.Count; i++)
            {
                SkillData skill = skills[i];
                if (skill == null)
                {
                    error = $"SkillCatalog entry {i + 1} is empty.";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(skill.SkillId) || !ids.Add(skill.SkillId))
                {
                    error = $"A skillId is empty or duplicated: {skill.SkillId}";
                    return false;
                }
                if (skill.Category == SkillCategory.BasicAttack)
                {
                    if (!attacks.Add(skill.AttackArchetype))
                    {
                        error = $"A basic attack archetype is duplicated: {skill.AttackArchetype}";
                        return false;
                    }
                    if (skill.ProjectileSprite == null)
                    {
                        error = $"{skill.SkillId} has no projectile sprite.";
                        return false;
                    }
                }
                else if (!active.Add(skill.ActiveSkillId))
                {
                    error = $"An active skill id is duplicated: {skill.ActiveSkillId}";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }
    }
}
