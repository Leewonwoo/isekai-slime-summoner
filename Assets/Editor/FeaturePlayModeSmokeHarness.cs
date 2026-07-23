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
            VisualElement codexHost = uiRoot.Q<VisualElement>("monster-codex-modal");
            VisualElement merchantHost = uiRoot.Q<VisualElement>("merchant-modal");
            VisualElement codexOverlay = uiRoot.Q<VisualElement>("codex-overlay");
            VisualElement merchantOverlay = uiRoot.Q<VisualElement>("merchant-overlay");
            float rootHeight = uiRoot.resolvedStyle.height;
            if (codexHost == null || codexHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Codex modal host does not cover the screen.");
            if (merchantHost == null || merchantHost.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Merchant modal host does not cover the screen.");
            if (codexOverlay == null || codexOverlay.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Closed codex overlay lost its screen layout.");
            if (merchantOverlay == null || merchantOverlay.resolvedStyle.height < rootHeight * 0.9f)
                throw new InvalidOperationException("Closed merchant overlay lost its screen layout.");

            game.SetGameplayPause(GameplayPauseReason.MonsterCodex, true);
            game.SetGameplayPause(GameplayPauseReason.Merchant, true);
            game.SetGameplayPause(GameplayPauseReason.MonsterCodex, false);
            if (!game.IsGameplayPaused || Time.timeScale != 0f) throw new InvalidOperationException("Overlapping pause reasons resumed too early.");
            game.SetGameplayPause(GameplayPauseReason.Merchant, false);
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
