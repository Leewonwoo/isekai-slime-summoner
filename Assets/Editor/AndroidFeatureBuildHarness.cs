using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class AndroidFeatureBuildHarness
    {
        public static void BuildFromCommandLine()
        {
            try
            {
                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = "Builds/CrossDefense-feature-smoke.apk",
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Android build failed: {report.summary.result}");
                Debug.Log($"[CrossDefense] Android feature build passed: {report.summary.totalSize} bytes.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }
    }
}
