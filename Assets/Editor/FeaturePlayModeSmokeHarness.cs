using System;
using CrossDefense.Core;
using CrossDefense.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace CrossDefense.Editor
{
    [InitializeOnLoad]
    public static class FeaturePlayModeSmokeHarness
    {
        const string ActiveKey = "CrossDefense.FeatureSmoke.Active";
        static int _playFrames;

        static FeaturePlayModeSmokeHarness()
        {
            if (SessionState.GetBool(ActiveKey, false)) Attach();
        }

        public static void RunFromCommandLine()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            SessionState.SetBool(ActiveKey, true);
            Attach();
            EditorApplication.EnterPlaymode();
        }

        static void Attach()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                _playFrames = 0;
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                EditorApplication.playModeStateChanged -= OnPlayModeChanged;
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.Exit(0);
            }
        }

        static void Tick()
        {
            if (!EditorApplication.isPlaying || ++_playFrames < 12) return;
            EditorApplication.update -= Tick;
            try
            {
                RunAssertions();
                Debug.Log("[CrossDefense] Feature PlayMode smoke passed: codex, pause flags, merchant UI.");
                EditorApplication.ExitPlaymode();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.Exit(1);
            }
        }

        static void RunAssertions()
        {
            GameManager game = UnityEngine.Object.FindFirstObjectByType<GameManager>();
            RootLayoutController root = UnityEngine.Object.FindFirstObjectByType<RootLayoutController>();
            if (game == null || root == null) throw new InvalidOperationException("Gameplay services or root UI are missing.");
            if (game.MonsterCatalog?.Monsters?.Count != 16) throw new InvalidOperationException("Monster catalog is not 16 entries.");

            VisualElement codexGrid = root.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("codex-grid");
            if (codexGrid == null || codexGrid.childCount != 16) throw new InvalidOperationException("Codex grid is not 4x4/16 slots.");
            VisualElement uiRoot = root.GetComponent<UIDocument>().rootVisualElement;
            VisualElement overdriveGauge = uiRoot.Q<VisualElement>("overdrive-gauge");
            VisualElement skillButton = uiRoot.Q<VisualElement>("skill-button");
            if (overdriveGauge == null ||
                Mathf.Abs(overdriveGauge.resolvedStyle.width - 48f) > 1f ||
                Mathf.Abs(overdriveGauge.resolvedStyle.height - 128f) > 1f)
                throw new InvalidOperationException("Overdrive gauge is not the approved 48x128 vertical tank.");
            if (skillButton == null ||
                Mathf.Abs(overdriveGauge.worldBound.yMax - skillButton.worldBound.yMax) > 1f ||
                Mathf.Abs(skillButton.worldBound.xMin - overdriveGauge.worldBound.xMax - 16f) > 1f)
                throw new InvalidOperationException("Overdrive gauge is not bottom-aligned 16px left of the skill button.");
            if (overdriveGauge.Query<VisualElement>(className: "overdrive-gauge__tick").ToList().Count != 3)
                throw new InvalidOperationException("Overdrive gauge does not have three quarter ticks.");
            VisualElement slimeCodexGrid = uiRoot.Q<VisualElement>("slime-codex-grid");
            if (slimeCodexGrid == null || slimeCodexGrid.childCount != 8)
                throw new InvalidOperationException("Slime codex grid is not 8 slots.");
            VisualElement slimeCodexHost = uiRoot.Q<VisualElement>("slime-codex-modal");
            VisualElement codexHost = uiRoot.Q<VisualElement>("monster-codex-modal");
            VisualElement merchantHost = uiRoot.Q<VisualElement>("merchant-modal");
            VisualElement runResultHost = uiRoot.Q<VisualElement>("run-result-modal");
            VisualElement slimeCodexOverlay = uiRoot.Q<VisualElement>("slime-codex-overlay");
            VisualElement codexOverlay = uiRoot.Q<VisualElement>("codex-overlay");
            VisualElement merchantOverlay = uiRoot.Q<VisualElement>("merchant-overlay");
            float rootHeight = uiRoot.resolvedStyle.height;
            if (slimeCodexHost == null || slimeCodexHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Slime codex modal host does not cover the screen.");
            if (codexHost == null || codexHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Codex modal host does not cover the screen.");
            if (merchantHost == null || merchantHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Merchant modal host does not cover the screen.");
            if (runResultHost == null || runResultHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Run result modal host does not cover the screen.");
            if (slimeCodexHost.pickingMode != PickingMode.Ignore ||
                codexHost.pickingMode != PickingMode.Ignore ||
                merchantHost.pickingMode != PickingMode.Ignore ||
                runResultHost.pickingMode != PickingMode.Ignore)
                throw new InvalidOperationException("A closed modal host is blocking screen input.");
            if (slimeCodexOverlay == null || slimeCodexOverlay.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Closed slime codex overlay lost its screen layout.");
            if (codexOverlay == null || codexOverlay.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Closed codex overlay lost its screen layout.");
            if (merchantOverlay == null || merchantOverlay.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Closed merchant overlay lost its screen layout.");

            game.SetGameplayPause(GameplayPauseReason.SlimeCodex, true);
            game.SetGameplayPause(GameplayPauseReason.MonsterCodex, true);
            game.SetGameplayPause(GameplayPauseReason.SlimeCodex, false);
            if (!game.IsGameplayPaused || Time.timeScale != 0f) throw new InvalidOperationException("Overlapping pause reasons resumed too early.");
            game.SetGameplayPause(GameplayPauseReason.MonsterCodex, false);
            if (game.IsGameplayPaused || Time.timeScale == 0f) throw new InvalidOperationException("Gameplay did not resume after the last pause reason.");

            if (!game.BeginMerchant(8)) throw new InvalidOperationException("Merchant did not open.");
            if (game.Phase != RunPhase.Merchant || !game.IsGameplayPaused || game.Merchant.Offers.Count != 3)
                throw new InvalidOperationException("Merchant phase, pause, or inventory is invalid.");
            if (root.MerchantModal?.IsVisible != true) throw new InvalidOperationException("Merchant UI is not visible.");
            game.CloseMerchant();
            if (game.IsMerchantOpen || game.IsGameplayPaused) throw new InvalidOperationException("Merchant did not close cleanly.");
        }
    }
}
