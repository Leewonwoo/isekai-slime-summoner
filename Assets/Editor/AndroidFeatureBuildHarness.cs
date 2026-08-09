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
            Build("Builds/IsekaiSlimeSummoner-feature-smoke.apk", false);
        }

        public static void BuildReleaseFromCommandLine()
        {
            Build("Builds/IsekaiSlimeSummoner-release.apk", true);
        }

        private static void Build(string outputPath, bool requireReleaseCredentials)
        {
            try
            {
                if (requireReleaseCredentials)
                {
                    string keystorePassword = Environment.GetEnvironmentVariable("ISEKAI_KEYSTORE_PASSWORD");
                    string aliasPassword = Environment.GetEnvironmentVariable("ISEKAI_KEYALIAS_PASSWORD");
                    if (string.IsNullOrEmpty(keystorePassword) || string.IsNullOrEmpty(aliasPassword))
                        throw new InvalidOperationException(
                            "Release signing credentials are missing. Set ISEKAI_KEYSTORE_PASSWORD and ISEKAI_KEYALIAS_PASSWORD.");

                    PlayerSettings.Android.keystorePass = keystorePassword;
                    PlayerSettings.Android.keyaliasPass = aliasPassword;
                }

                string[] scenes = EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(scene => scene.path)
                    .ToArray();
                if (scenes.Length == 0) scenes = new[] { "Assets/Scenes/SampleScene.unity" };

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputPath,
                    target = BuildTarget.Android,
                    options = BuildOptions.None,
                };
                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException($"Android build failed: {report.summary.result}");
                Debug.Log($"[CrossDefense] Android build passed: {outputPath}, {report.summary.totalSize} bytes.");
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
