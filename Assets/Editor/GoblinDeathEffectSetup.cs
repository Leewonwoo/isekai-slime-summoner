using CrossDefense.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class GoblinDeathEffectSetup
    {
        const string SheetPath = "Assets/Art/Enemies/effect_goblin_death_sheet.png";

        [MenuItem("Isekai Slime Summoner/Setup/Goblin Death Effect")]
        public static void Apply()
        {
            SummonerSkillEffectSetup.ConfigureNineFrameSheet(
                SheetPath,
                new Vector2(0.5f, 0.5f));
            Sprite[] frames = SummonerSkillEffectSetup.LoadFrames(SheetPath);
            GameManager[] managers = Object.FindObjectsByType<GameManager>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            for (int i = 0; i < managers.Length; i++)
            {
                var serialized = new SerializedObject(managers[i]);
                SummonerSkillEffectSetup.SetSpriteArray(
                    serialized.FindProperty("runtimeGoblinDeathEffectFrames"),
                    frames);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(managers[i]);
            }

            if (managers.Length > 0)
                EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log(
                $"[CrossDefense] Goblin death effect configured: " +
                $"frames={frames.Length}, managers={managers.Length}.");
        }
    }
}
