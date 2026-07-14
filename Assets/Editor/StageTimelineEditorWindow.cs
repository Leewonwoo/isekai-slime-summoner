using System;
using UnityEditor;
using UnityEngine;
using CrossDefense.Data;

namespace CrossDefense.Editor
{
    /// <summary>IMGUI editor for arranging waves and tuning their spawn/balance values.</summary>
    public sealed class StageTimelineEditorWindow : EditorWindow
    {
        const string DefaultTimelineFolder = "Assets/Data/StageTimelines";
        const string DefaultMonsterFolder = "Assets/Data/Monsters";

        StageTimeline _timeline;
        SerializedObject _serializedTimeline;
        SerializedProperty _stageId;
        SerializedProperty _displayName;
        SerializedProperty _balanceProfile;
        SerializedProperty _randomSeed;
        SerializedProperty _randomizeDirectionWeights;
        SerializedProperty _directionWeightJitter;
        SerializedProperty _waves;
        int _selectedWave = -1;
        Vector2 _waveScroll;
        Vector2 _detailScroll;

        [MenuItem("Cross Defense/Stage Timeline Editor", priority = 0)]
        public static void OpenWindow()
        {
            var window = GetWindow<StageTimelineEditorWindow>();
            window.titleContent = new GUIContent("Stage Timeline");
            window.minSize = new Vector2(960f, 560f);
            if (Selection.activeObject is StageTimeline selected)
                window.SetTimeline(selected);
            window.Show();
        }

        public static void OpenWindow(StageTimeline timeline)
        {
            var window = GetWindow<StageTimelineEditorWindow>();
            window.titleContent = new GUIContent("Stage Timeline");
            window.minSize = new Vector2(960f, 560f);
            window.SetTimeline(timeline);
            window.Show();
        }

        [MenuItem("Assets/Create/Cross Defense/Stage Timeline", false, 20)]
        static void CreateTimelineAsset()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "StageTimelines");
            var path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultTimelineFolder}/NewStageTimeline.asset");
            var timeline = CreateInstance<StageTimeline>();
            AssetDatabase.CreateAsset(timeline, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = timeline;
            OpenWindow(timeline);
        }

        [MenuItem("Cross Defense/Create Starter Stage Timeline", priority = 10)]
        static void CreateStarterTimeline()
        {
            EnsureFolder("Assets", "Data");
            EnsureFolder("Assets/Data", "StageTimelines");
            EnsureFolder("Assets/Data", "Monsters");

            var balance = GetOrCreateAsset<StageBalanceProfile>("Assets/Data/StageBalance_Default.asset");
            var basic = CreateMonster("Monster_BasicSlime", "Basic Slime", MonsterShape.BasicSlime, MonsterAttribute.None, 100, 1.0f, 1, 2);
            var fast = CreateMonster("Monster_FastSlime", "Fast Slime", MonsterShape.SpitterSlime, MonsterAttribute.Fire, 65, 1.6f, 1, 3);
            var tank = CreateMonster("Monster_TankSlime", "Tank Slime", MonsterShape.TankSlime, MonsterAttribute.Ice, 280, 0.65f, 3, 8);
            var splitter = CreateMonster("Monster_SplitSlime", "Split Slime", MonsterShape.SplitSlime, MonsterAttribute.Nature, 180, 0.9f, 2, 6);
            var king = CreateMonster("Monster_KingSlime", "King Slime", MonsterShape.Boss, MonsterAttribute.None, 1800, 0.45f, 10, 40);
            var mother = CreateMonster("Monster_MotherSlime", "Mother Slime", MonsterShape.Boss, MonsterAttribute.Fire, 3200, 0.4f, 14, 70);

            var path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultTimelineFolder}/Stage_01.asset");
            var timeline = CreateInstance<StageTimeline>();
            AssetDatabase.CreateAsset(timeline, path);
            ConfigureStarterTimeline(timeline, balance, basic, fast, tank, splitter, king, mother);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = timeline;
            OpenWindow(timeline);
        }

        void OnEnable()
        {
            if (_timeline == null && Selection.activeObject is StageTimeline selected)
                SetTimeline(selected);
        }

        void OnSelectionChange()
        {
            if (Selection.activeObject is StageTimeline selected)
            {
                SetTimeline(selected);
                Repaint();
            }
        }

        void OnGUI()
        {
            DrawToolbar();
            if (_timeline == null)
            {
                EditorGUILayout.HelpBox("Select a Stage Timeline asset or create one to start editing waves.", MessageType.Info);
                if (GUILayout.Button("Create New Stage Timeline", GUILayout.Height(32f)))
                    CreateTimelineAsset();
                if (GUILayout.Button("Create Starter Timeline (20 Waves)", GUILayout.Height(32f)))
                    CreateStarterTimeline();
                return;
            }

            EnsureSerializedObject();
            _serializedTimeline.Update();

            var selectedTimeline = (StageTimeline)EditorGUILayout.ObjectField("Timeline Asset", _timeline, typeof(StageTimeline), false);
            if (selectedTimeline != _timeline)
            {
                SetTimeline(selectedTimeline);
                GUIUtility.ExitGUI();
                return;
            }

            DrawStageSettings();
            EditorGUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            DrawWaveList();
            DrawWaveDetails();
            EditorGUILayout.EndHorizontal();

            if (_serializedTimeline.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_timeline);
                Repaint();
            }
        }

        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Stage Timeline Editor", EditorStyles.toolbarButton, GUILayout.Width(145f));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(55f)))
            {
                if (_serializedTimeline != null) _serializedTimeline.ApplyModifiedProperties();
                if (_timeline != null) EditorUtility.SetDirty(_timeline);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawStageSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Stage Settings", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_stageId, new GUIContent("Stage ID"));
            EditorGUILayout.PropertyField(_displayName, new GUIContent("Display Name"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_balanceProfile, new GUIContent("Shared Balance Profile"));
            EditorGUILayout.PropertyField(_randomSeed, new GUIContent("Run Seed"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_randomizeDirectionWeights, new GUIContent("Randomize Spawn Zones"));
            using (new EditorGUI.DisabledScope(!_randomizeDirectionWeights.boolValue))
                EditorGUILayout.Slider(_directionWeightJitter, 0f, 1f, new GUIContent("Spawn Zone Jitter"));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.LabelField($"Wave Count: {_waves.arraySize}", EditorStyles.miniBoldLabel);
            EditorGUILayout.EndVertical();
        }

        void DrawWaveList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(250f));
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(24f)))
            {
                AddWave();
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            _waveScroll = EditorGUILayout.BeginScrollView(_waveScroll);
            for (int i = 0; i < _waves.arraySize; i++)
            {
                var wave = _waves.GetArrayElementAtIndex(i);
                var label = wave.FindPropertyRelative("label").stringValue;
                var isBoss = wave.FindPropertyRelative("isBoss").boolValue;
                var title = $"{i + 1:00}  {label}";
                if (isBoss) title += "  [BOSS]";
                var oldColor = GUI.backgroundColor;
                if (i == _selectedWave) GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
                if (GUILayout.Button(title, GUILayout.Height(28f))) _selectedWave = i;
                GUI.backgroundColor = oldColor;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_selectedWave <= 0))
            {
                if (GUILayout.Button("Move Up")) MoveWave(-1);
            }
            using (new EditorGUI.DisabledScope(_selectedWave < 0 || _selectedWave >= _waves.arraySize - 1))
            {
                if (GUILayout.Button("Move Down")) MoveWave(1);
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(_selectedWave < 0))
            {
                if (GUILayout.Button("Duplicate")) DuplicateWave();
                if (GUILayout.Button("Remove")) RemoveWave();
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        void DrawWaveDetails()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            if (_selectedWave < 0 || _selectedWave >= _waves.arraySize)
            {
                EditorGUILayout.HelpBox("Select a wave from the left panel.", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
            var wave = _waves.GetArrayElementAtIndex(_selectedWave);
            EditorGUILayout.LabelField($"Wave {_selectedWave + 1}", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("label"), new GUIContent("Label"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("isBoss"), new GUIContent("Boss Wave"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("preparationTime"), new GUIContent("Preparation Time (sec)"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("hpMultiplier"), new GUIContent("Wave HP Multiplier"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("speedMultiplier"), new GUIContent("Wave Speed Multiplier"));
            EditorGUILayout.PropertyField(wave.FindPropertyRelative("summonContractReward"), new GUIContent("Summon Contract Reward"));

            DrawSpawnZoneWeights(wave.FindPropertyRelative("spawnZoneWeights"));
            DrawMonsterSpawns(wave.FindPropertyRelative("monsterSpawns"));
            DrawWaveValidation(wave);
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        static void DrawSpawnZoneWeights(SerializedProperty weights)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Viewport Edge Spawn Weights", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Weights are relative spawn chances for the top / right / bottom / left edge. Zero disables an edge.", MessageType.None);
            EditorGUILayout.BeginHorizontal();
            DrawWeight(weights.FindPropertyRelative("top"), "Top");
            DrawWeight(weights.FindPropertyRelative("right"), "Right");
            DrawWeight(weights.FindPropertyRelative("bottom"), "Bottom");
            DrawWeight(weights.FindPropertyRelative("left"), "Left");
            EditorGUILayout.EndHorizontal();
        }

        static void DrawWeight(SerializedProperty property, string label)
        {
            EditorGUILayout.BeginVertical(GUILayout.MinWidth(70f));
            EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);
            property.floatValue = EditorGUILayout.Slider(property.floatValue, 0f, 10f);
            EditorGUILayout.EndVertical();
        }

        void DrawMonsterSpawns(SerializedProperty spawns)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Monster Composition", EditorStyles.boldLabel);
            if (GUILayout.Button("Add Monster", GUILayout.Width(95f)))
            {
                AddMonster(spawns);
                GUIUtility.ExitGUI();
            }
            EditorGUILayout.EndHorizontal();

            int removeIndex = -1;
            for (int i = 0; i < spawns.arraySize; i++)
            {
                var spawn = spawns.GetArrayElementAtIndex(i);
                var monster = spawn.FindPropertyRelative("monster");
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Entry {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(65f))) removeIndex = i;
                EditorGUILayout.EndHorizontal();
                monster.objectReferenceValue = EditorGUILayout.ObjectField("Monster Profile", monster.objectReferenceValue, typeof(MonsterData), false);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(spawn.FindPropertyRelative("count"), new GUIContent("Count"));
                EditorGUILayout.PropertyField(spawn.FindPropertyRelative("spawnInterval"), new GUIContent("Interval"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(spawn.FindPropertyRelative("hpMultiplier"), new GUIContent("HP x"));
                EditorGUILayout.PropertyField(spawn.FindPropertyRelative("speedMultiplier"), new GUIContent("Speed x"));
                EditorGUILayout.PropertyField(spawn.FindPropertyRelative("rewardMultiplier"), new GUIContent("Reward x"));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
                spawns.DeleteArrayElementAtIndex(removeIndex);

            EditorGUILayout.LabelField($"Total monsters in wave: {GetTotalCount(spawns)}", EditorStyles.miniBoldLabel);
        }

        static int GetTotalCount(SerializedProperty spawns)
        {
            int total = 0;
            for (int i = 0; i < spawns.arraySize; i++)
                total += Mathf.Max(0, spawns.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue);
            return total;
        }

        static void DrawWaveValidation(SerializedProperty wave)
        {
            var weights = wave.FindPropertyRelative("spawnZoneWeights");
            float totalWeight = 0f;
            foreach (var zone in new[] { "top", "right", "bottom", "left" })
                totalWeight += weights.FindPropertyRelative(zone).floatValue;

            if (wave.FindPropertyRelative("monsterSpawns").arraySize == 0)
                EditorGUILayout.HelpBox("Add at least one Monster Profile to this wave.", MessageType.Warning);
            if (totalWeight <= 0f)
                EditorGUILayout.HelpBox("At least one direction weight must be greater than zero.", MessageType.Warning);
        }

        void AddWave()
        {
            int index = _waves.arraySize;
            _waves.InsertArrayElementAtIndex(index);
            ResetWave(_waves.GetArrayElementAtIndex(index), index + 1);
            _selectedWave = index;
            ApplyChanges();
        }

        void DuplicateWave()
        {
            if (_selectedWave < 0 || _selectedWave >= _waves.arraySize) return;
            int newIndex = _selectedWave + 1;
            _waves.InsertArrayElementAtIndex(newIndex);
            var copy = _waves.GetArrayElementAtIndex(newIndex);
            copy.FindPropertyRelative("label").stringValue += " Copy";
            _selectedWave = newIndex;
            ApplyChanges();
        }

        void RemoveWave()
        {
            if (_selectedWave < 0 || _selectedWave >= _waves.arraySize) return;
            if (!EditorUtility.DisplayDialog("Remove Wave", $"Remove Wave {_selectedWave + 1}?", "Remove", "Cancel")) return;
            _waves.DeleteArrayElementAtIndex(_selectedWave);
            _selectedWave = Mathf.Clamp(_selectedWave, 0, _waves.arraySize - 1);
            ApplyChanges();
        }

        void MoveWave(int offset)
        {
            int target = _selectedWave + offset;
            if (_selectedWave < 0 || target < 0 || target >= _waves.arraySize) return;
            _waves.MoveArrayElement(_selectedWave, target);
            _selectedWave = target;
            ApplyChanges();
        }

        static void AddMonster(SerializedProperty spawns)
        {
            int index = spawns.arraySize;
            spawns.InsertArrayElementAtIndex(index);
            var spawn = spawns.GetArrayElementAtIndex(index);
            spawn.FindPropertyRelative("monster").objectReferenceValue = null;
            spawn.FindPropertyRelative("count").intValue = 1;
            spawn.FindPropertyRelative("spawnInterval").floatValue = 0.5f;
            spawn.FindPropertyRelative("hpMultiplier").floatValue = 1f;
            spawn.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            spawn.FindPropertyRelative("rewardMultiplier").floatValue = 1f;
        }

        static void ResetWave(SerializedProperty wave, int number)
        {
            wave.FindPropertyRelative("label").stringValue = $"Wave {number}";
            wave.FindPropertyRelative("isBoss").boolValue = false;
            wave.FindPropertyRelative("preparationTime").floatValue = 5f;
            wave.FindPropertyRelative("hpMultiplier").floatValue = 1f;
            wave.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            wave.FindPropertyRelative("summonContractReward").intValue = 6;
            var weights = wave.FindPropertyRelative("spawnZoneWeights");
            weights.FindPropertyRelative("top").floatValue = 1f;
            weights.FindPropertyRelative("right").floatValue = 1f;
            weights.FindPropertyRelative("bottom").floatValue = 1f;
            weights.FindPropertyRelative("left").floatValue = 1f;
            wave.FindPropertyRelative("monsterSpawns").arraySize = 0;
        }

        void SetTimeline(StageTimeline timeline)
        {
            _timeline = timeline;
            _serializedTimeline = null;
            _selectedWave = timeline == null ? -1 : 0;
            EnsureSerializedObject();
        }

        void EnsureSerializedObject()
        {
            if (_timeline == null) return;
            if (_serializedTimeline != null && _serializedTimeline.targetObject == _timeline) return;
            _serializedTimeline = new SerializedObject(_timeline);
            _stageId = _serializedTimeline.FindProperty("stageId");
            _displayName = _serializedTimeline.FindProperty("displayName");
            _balanceProfile = _serializedTimeline.FindProperty("balanceProfile");
            _randomSeed = _serializedTimeline.FindProperty("randomSeed");
            _randomizeDirectionWeights = _serializedTimeline.FindProperty("randomizeDirectionWeights");
            _directionWeightJitter = _serializedTimeline.FindProperty("directionWeightJitter");
            _waves = _serializedTimeline.FindProperty("waves");
            _selectedWave = _waves.arraySize == 0 ? -1 : Mathf.Clamp(_selectedWave, 0, _waves.arraySize - 1);
        }

        void ApplyChanges()
        {
            _serializedTimeline.ApplyModifiedProperties();
            EditorUtility.SetDirty(_timeline);
            Repaint();
        }

        static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, folder);
        }

        static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static MonsterData CreateMonster(string fileName, string displayName, MonsterShape shape, MonsterAttribute attribute, int hp, float speed, int damage, int reward)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{DefaultMonsterFolder}/{fileName}.asset");
            var monster = CreateInstance<MonsterData>();
            AssetDatabase.CreateAsset(monster, path);
            var serialized = new SerializedObject(monster);
            serialized.FindProperty("monsterId").stringValue = fileName.ToLowerInvariant();
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("shape").enumValueIndex = (int)shape;
            serialized.FindProperty("attribute").enumValueIndex = (int)attribute;
            serialized.FindProperty("baseHp").intValue = hp;
            serialized.FindProperty("moveSpeed").floatValue = speed;
            serialized.FindProperty("contactDamage").intValue = damage;
            serialized.FindProperty("rewardGold").intValue = reward;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return monster;
        }

        static void ConfigureStarterTimeline(StageTimeline timeline, StageBalanceProfile balance, MonsterData basic, MonsterData fast, MonsterData tank, MonsterData splitter, MonsterData king, MonsterData mother)
        {
            var serialized = new SerializedObject(timeline);
            serialized.FindProperty("stageId").stringValue = "stage-01";
            serialized.FindProperty("displayName").stringValue = "Crossroads Outpost";
            serialized.FindProperty("balanceProfile").objectReferenceValue = balance;
            serialized.FindProperty("randomSeed").intValue = 2026;
            serialized.FindProperty("randomizeDirectionWeights").boolValue = true;
            serialized.FindProperty("directionWeightJitter").floatValue = 0.15f;

            var waves = serialized.FindProperty("waves");
            waves.arraySize = 20;
            for (int i = 0; i < waves.arraySize; i++)
            {
                var wave = waves.GetArrayElementAtIndex(i);
                ResetWave(wave, i + 1);
                wave.FindPropertyRelative("preparationTime").floatValue = Mathf.Max(2f, 5f - i * 0.1f);
                wave.FindPropertyRelative("hpMultiplier").floatValue = 1f + i * 0.1f;
                wave.FindPropertyRelative("speedMultiplier").floatValue = 1f + i * 0.015f;
                int waveNumber = i + 1;
                int contractReward = waveNumber == 20 ? 0
                    : waveNumber == 10 ? 12
                    : waveNumber == 5 || waveNumber == 15 ? 10
                    : Mathf.Clamp(5 + i / 5, 5, 8);
                wave.FindPropertyRelative("summonContractReward").intValue = contractReward;
                if (i == 9 || i == 19)
                    wave.FindPropertyRelative("isBoss").boolValue = true;

                var weights = wave.FindPropertyRelative("spawnZoneWeights");
                var focus = i % 4;
                foreach (var zone in new[] { "top", "right", "bottom", "left" })
                    weights.FindPropertyRelative(zone).floatValue = 1f;
                weights.FindPropertyRelative(new[] { "top", "right", "bottom", "left" }[focus]).floatValue = 3f;

                var spawns = wave.FindPropertyRelative("monsterSpawns");
                spawns.arraySize = 1;
                SetSpawn(spawns.GetArrayElementAtIndex(0), i < 3 ? basic : fast, 6 + i * 2, 0.5f);
                if (i >= 4 && i != 9 && i != 19)
                {
                    spawns.arraySize = 2;
                    SetSpawn(spawns.GetArrayElementAtIndex(1), i < 10 ? tank : splitter, 2 + i / 2, 0.7f);
                }
                if (i == 9)
                {
                    spawns.arraySize = 2;
                    SetSpawn(spawns.GetArrayElementAtIndex(0), tank, 8, 0.65f);
                    SetSpawn(spawns.GetArrayElementAtIndex(1), king, 1, 1.5f);
                }
                if (i == 19)
                {
                    spawns.arraySize = 3;
                    SetSpawn(spawns.GetArrayElementAtIndex(0), splitter, 18, 0.5f);
                    SetSpawn(spawns.GetArrayElementAtIndex(1), tank, 10, 0.7f);
                    SetSpawn(spawns.GetArrayElementAtIndex(2), mother, 1, 1.5f);
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(timeline);
        }

        static void SetSpawn(SerializedProperty spawn, MonsterData monster, int count, float interval)
        {
            spawn.FindPropertyRelative("monster").objectReferenceValue = monster;
            spawn.FindPropertyRelative("count").intValue = count;
            spawn.FindPropertyRelative("spawnInterval").floatValue = interval;
            spawn.FindPropertyRelative("hpMultiplier").floatValue = 1f;
            spawn.FindPropertyRelative("speedMultiplier").floatValue = 1f;
            spawn.FindPropertyRelative("rewardMultiplier").floatValue = 1f;
        }
    }

    [CustomEditor(typeof(StageTimeline))]
    public sealed class StageTimelineInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Stage Timeline Editor", GUILayout.Height(28f)))
                StageTimelineEditorWindow.OpenWindow((StageTimeline)target);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}
