#if UNITY_EDITOR
using System.Collections.Generic;
using CrossDefense.Core;
using CrossDefense.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class SkillDataMigration
    {
        const string Root = "Assets/Data/Skills";
        const string CatalogPath = Root + "/SkillCatalog_Default.asset";

        [MenuItem("CrossDefense/Data/Rebuild Skill Data")]
        public static void Rebuild()
        {
            EnsureFolder("Assets/Data", "Skills");

            var skills = new List<SkillData>
            {
                CreateBasic(
                    "Basic_EnergyBolt", "basic-energy-bolt",
                    SummonerAttackArchetype.EnergyBolt, MonsterAttribute.None,
                    "8ac37c39a193ddf5aa93239e788a0b80", 0.65f,
                    0f, 1, 1f, 0f, 0f, 0f, 0f),
                CreateBasic(
                    "Basic_Fireball", "basic-fireball",
                    SummonerAttackArchetype.Fireball, MonsterAttribute.Fire,
                    "05fe82a7604717789a0fc6b6cba6d228", 0.78f,
                    0.95f, 1, 1f, 0f, 0f, 2f, 3f),
                CreateBasic(
                    "Basic_IceLance", "basic-ice-lance",
                    SummonerAttackArchetype.IceLance, MonsterAttribute.Ice,
                    "3bf397a35a02c10c805a2aa1143a2745", 0.6175f,
                    0f, 3, 1f, 0.3f, 2f, 0f, 0f),
                CreateBasic(
                    "Basic_ThunderSlash", "basic-thunder-slash",
                    SummonerAttackArchetype.ThunderSlash, MonsterAttribute.Lightning,
                    "0fd81a6db1870bba0a73c068aa6d0857", 0.702f,
                    0f, 3, 0.65f, 0f, 0f, 0f, 0f),
                CreateActive(
                    "Active_Meteor", "active-meteor", "Meteor",
                    SummonerSkillId.Meteor, SkillExecutionMode.Meteor,
                    SummonerSkillTargeting.Point, MonsterAttribute.Fire,
                    1, 22f, 2.6f, 1.8f, 0f,
                    StatusProfile(dot: 0.3f, dotDuration: 3f, visualScale: 1.15f),
                    BarrageProfile(2, 4, 0.24f, 0.48f, 0.72f,
                        dot: 0.12f, dotDuration: 1.5f),
                    BarrageProfile(3, 12, 0.18f, 0.32f, 0.82f,
                        battlefieldWide: true, dot: 0.08f,
                        dotDuration: 1.5f, visualScale: 0.86f)),
                CreateActive(
                    "Active_IceWall", "active-ice-wall", "Ice Wall",
                    SummonerSkillId.IceWall, SkillExecutionMode.IceWall,
                    SummonerSkillTargeting.Directional, MonsterAttribute.Ice,
                    8, 26f, 1.2f, 0.55f, 4f,
                    StatusProfile(slow: 0.6f, slowDuration: 2.5f),
                    BarrageProfile(2, 4, 0.24f, 0.48f, 0.72f,
                        slow: 0.65f, slowDuration: 1.35f),
                    BarrageProfile(3, 12, 0.18f, 0.32f, 0.82f,
                        battlefieldWide: true, slow: 0.75f,
                        slowDuration: 1.8f, visualScale: 1.15f)),
                CreateActive(
                    "Active_Aegis", "active-aegis", "Aegis",
                    SummonerSkillId.Aegis, SkillExecutionMode.Shield,
                    SummonerSkillTargeting.Instant, MonsterAttribute.None,
                    1, 32f, 0f, 0f, 6f,
                    StatusProfile(strength: 0.35f),
                    RankProfile(2),
                    RankProfile(3)),
                CreateElement(
                    "Active_ArcaneBurst", "active-arcane-burst", "Arcane Burst",
                    SummonerSkillId.ArcaneBurst, MonsterAttribute.None,
                    16f, 1.8f, 1.45f),
                CreateElement(
                    "Active_LightningStrike", "active-lightning-strike", "Lightning Strike",
                    SummonerSkillId.LightningStrike, MonsterAttribute.Lightning,
                    19f, 2.2f, 1.35f),
                CreateElement(
                    "Active_WaterBurst", "active-water-burst", "Water Burst",
                    SummonerSkillId.WaterBurst, MonsterAttribute.Water,
                    18f, 2f, 1.55f),
                CreateElement(
                    "Active_Gale", "active-gale", "Gale",
                    SummonerSkillId.Gale, MonsterAttribute.Wind,
                    17f, 1.7f, 1.9f),
            };

            SkillCatalog catalog = LoadOrCreate<SkillCatalog>(CatalogPath);
            var serialized = new SerializedObject(catalog);
            SerializedProperty list = serialized.FindProperty("skills");
            list.arraySize = skills.Count;
            for (int i = 0; i < skills.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = skills[i];
            serialized.FindProperty("defaultBasicAttack").objectReferenceValue = skills[0];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            AssignCatalogToOpenScenes(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CrossDefense] Rebuilt {skills.Count} SkillData assets and SkillCatalog.");
        }

        static SkillData CreateElement(
            string fileName,
            string skillId,
            string displayName,
            SummonerSkillId activeId,
            MonsterAttribute attribute,
            float cooldown,
            float damage,
            float radius) =>
            CreateActive(
                fileName, skillId, displayName, activeId,
                SkillExecutionMode.ElementBurst, SummonerSkillTargeting.Point,
                attribute, 1, cooldown, damage, radius, 0f,
                RankProfile(1),
                BarrageProfile(2, 4, 0.24f, 0.48f, 0.72f),
                BarrageProfile(
                    3, 12, 0.18f, 0.32f, 0.82f,
                    battlefieldWide: true));

        static SkillData CreateBasic(
            string fileName,
            string skillId,
            SummonerAttackArchetype archetype,
            MonsterAttribute attribute,
            string spriteGuid,
            float scale,
            float areaRadius,
            int pierce,
            float chainDamage,
            float slow,
            float slowDuration,
            float dot,
            float dotDuration)
        {
            SkillData skill = LoadOrCreate<SkillData>($"{Root}/{fileName}.asset");
            var so = new SerializedObject(skill);
            Set(so, "skillId", skillId);
            Set(so, "displayName", archetype.ToString());
            Set(so, "description", string.Empty);
            Set(so, "category", (int)SkillCategory.BasicAttack);
            Set(so, "executionMode", (int)SkillExecutionMode.BasicProjectile);
            Set(so, "attackArchetype", (int)archetype);
            Set(so, "projectileSprite", LoadSprite(spriteGuid));
            Set(so, "baseDamage", 12f);
            Set(so, "attacksPerSecond", 1.25f);
            Set(so, "attackRange", 4.5f);
            Set(so, "projectileSpeed", 10f);
            Set(so, "projectileScale", scale);
            Set(so, "clickDamage", 18f);
            Set(so, "clickAttacksPerSecond", 2f);
            Set(so, "clickHitRadius", 0.65f);
            Set(so, "volleyShotDelay", 0.09f);
            Set(so, "projectileCount", 1);
            Set(so, "additionalProjectileDamageMultiplier", 0.65f);
            Set(so, "areaRadius", areaRadius);
            Set(so, "pierceCount", pierce);
            Set(so, "chainDamageMultiplier", chainDamage);
            Set(so, "slowPercent", slow);
            Set(so, "slowDuration", slowDuration);
            Set(so, "damageOverTime", dot);
            Set(so, "damageOverTimeDuration", dotDuration);
            Set(so, "attribute", (int)attribute);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        static SkillData CreateActive(
            string fileName,
            string skillId,
            string displayName,
            SummonerSkillId activeId,
            SkillExecutionMode mode,
            SummonerSkillTargeting targeting,
            MonsterAttribute attribute,
            int unlockLevel,
            float cooldown,
            float damage,
            float radius,
            float duration,
            params RankValues[] ranks)
        {
            SkillData skill = LoadOrCreate<SkillData>($"{Root}/{fileName}.asset");
            var so = new SerializedObject(skill);
            Set(so, "skillId", skillId);
            Set(so, "displayName", displayName);
            Set(so, "description", string.Empty);
            Set(so, "category", (int)SkillCategory.Active);
            Set(so, "executionMode", (int)mode);
            Set(so, "activeSkillId", (int)activeId);
            Set(so, "targeting", (int)targeting);
            Set(so, "unlockLevel", unlockLevel);
            Set(so, "cooldown", cooldown);
            Set(so, "activeDamageMultiplier", damage);
            Set(so, "activeRadius", radius);
            Set(so, "activeDuration", duration);
            Set(so, "attribute", (int)attribute);
            SerializedProperty profiles = so.FindProperty("rankProfiles");
            profiles.arraySize = ranks.Length;
            for (int i = 0; i < ranks.Length; i++)
                ApplyRank(profiles.GetArrayElementAtIndex(i), ranks[i]);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(skill);
            return skill;
        }

        static RankValues RankProfile(int rank) => new()
        {
            Damage = 1f + (rank - 1) * 0.35f,
            Radius = 1f + (rank - 1) * 0.15f,
            Duration = 1f + (rank - 1) * 0.15f,
            StrikeCount = 1,
            StrikeInterval = 0.24f,
            PerStrikeDamage = 1f,
            PerStrikeRadius = 1f,
            VisualScale = 1f,
        };

        static RankValues StatusProfile(
            float slow = 0f,
            float slowDuration = 0f,
            float dot = 0f,
            float dotDuration = 0f,
            float strength = 0f,
            float visualScale = 1f)
        {
            RankValues values = RankProfile(1);
            values.Slow = slow;
            values.SlowDuration = slowDuration;
            values.Dot = dot;
            values.DotDuration = dotDuration;
            values.Strength = strength;
            values.VisualScale = visualScale;
            return values;
        }

        static RankValues BarrageProfile(
            int rank,
            int strikes,
            float interval,
            float perDamage,
            float perRadius,
            bool battlefieldWide = false,
            float slow = 0f,
            float slowDuration = 0f,
            float dot = 0f,
            float dotDuration = 0f,
            float visualScale = 1f)
        {
            RankValues values = RankProfile(rank);
            values.StrikeCount = strikes;
            values.StrikeInterval = interval;
            values.PerStrikeDamage = perDamage;
            values.PerStrikeRadius = perRadius;
            values.BattlefieldWide = battlefieldWide;
            values.Slow = slow;
            values.SlowDuration = slowDuration;
            values.Dot = dot;
            values.DotDuration = dotDuration;
            values.VisualScale = visualScale;
            return values;
        }

        static void ApplyRank(SerializedProperty property, RankValues values)
        {
            property.FindPropertyRelative("damageMultiplier").floatValue = values.Damage;
            property.FindPropertyRelative("radiusMultiplier").floatValue = values.Radius;
            property.FindPropertyRelative("durationMultiplier").floatValue = values.Duration;
            property.FindPropertyRelative("strikeCount").intValue = values.StrikeCount;
            property.FindPropertyRelative("strikeInterval").floatValue = values.StrikeInterval;
            property.FindPropertyRelative("perStrikeDamageMultiplier").floatValue =
                values.PerStrikeDamage;
            property.FindPropertyRelative("perStrikeRadiusMultiplier").floatValue =
                values.PerStrikeRadius;
            property.FindPropertyRelative("battlefieldWide").boolValue = values.BattlefieldWide;
            property.FindPropertyRelative("slowPercent").floatValue = values.Slow;
            property.FindPropertyRelative("slowDuration").floatValue = values.SlowDuration;
            property.FindPropertyRelative("damageOverTimeMultiplier").floatValue = values.Dot;
            property.FindPropertyRelative("damageOverTimeDuration").floatValue = values.DotDuration;
            property.FindPropertyRelative("strength").floatValue = values.Strength;
            property.FindPropertyRelative("visualScale").floatValue = values.VisualScale;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static Sprite LoadSprite(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite direct = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (direct != null)
                return direct;
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite)
                    return sprite;
            Debug.LogError($"[CrossDefense] Projectile sprite not found for GUID {guid}.");
            return null;
        }

        static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        static void AssignCatalogToOpenScenes(SkillCatalog catalog)
        {
            GameManager[] managers = Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int i = 0; i < managers.Length; i++)
            {
                var manager = new SerializedObject(managers[i]);
                manager.FindProperty("skillCatalog").objectReferenceValue = catalog;
                manager.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(managers[i]);
                EditorSceneManager.MarkSceneDirty(managers[i].gameObject.scene);
            }
            if (managers.Length > 0)
                EditorSceneManager.SaveOpenScenes();
        }

        static void Set(SerializedObject so, string property, int value) =>
            so.FindProperty(property).intValue = value;
        static void Set(SerializedObject so, string property, float value) =>
            so.FindProperty(property).floatValue = value;
        static void Set(SerializedObject so, string property, string value) =>
            so.FindProperty(property).stringValue = value;
        static void Set(SerializedObject so, string property, Object value) =>
            so.FindProperty(property).objectReferenceValue = value;

        struct RankValues
        {
            public float Damage;
            public float Radius;
            public float Duration;
            public int StrikeCount;
            public float StrikeInterval;
            public float PerStrikeDamage;
            public float PerStrikeRadius;
            public bool BattlefieldWide;
            public float Slow;
            public float SlowDuration;
            public float Dot;
            public float DotDuration;
            public float Strength;
            public float VisualScale;
        }
    }
}
#endif
