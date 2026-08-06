using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class GitHubPagesBuild
    {
        const string OutputDirectoryName = "PagesBuild";

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        [MenuItem("Isekai Slime Summoner/Build/GitHub Pages WebGL")]
        public static void Build()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                throw new BuildFailedException(
                    "Run this build with the WebGL target. The command-line invocation passes -buildTarget WebGL automatically.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, OutputDirectoryName));
            string expectedPrefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!outputDirectory.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                throw new BuildFailedException("The Pages output directory resolved outside the project root.");

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new BuildFailedException("No enabled scenes are configured in EditorBuildSettings.");

            WebGLCompressionFormat previousCompression = PlayerSettings.WebGL.compressionFormat;
            bool previousFallback = PlayerSettings.WebGL.decompressionFallback;
            bool previousCaching = PlayerSettings.WebGL.dataCaching;

            try
            {
                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, true);
                Directory.CreateDirectory(outputDirectory);

                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
                PlayerSettings.WebGL.decompressionFallback = true;
                PlayerSettings.WebGL.dataCaching = true;

                var options = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outputDirectory,
                    target = BuildTarget.WebGL,
                    options = BuildOptions.None,
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException(
                        $"GitHub Pages WebGL build failed: {report.summary.result}, " +
                        $"errors={report.summary.totalErrors}");

                File.WriteAllText(Path.Combine(outputDirectory, ".nojekyll"), string.Empty);
                Debug.Log(
                    $"[CrossDefense] GitHub Pages build succeeded: " +
                    $"{report.summary.totalSize / (1024f * 1024f):0.0} MiB at {outputDirectory}");
            }
            finally
            {
                PlayerSettings.WebGL.compressionFormat = previousCompression;
                PlayerSettings.WebGL.decompressionFallback = previousFallback;
                PlayerSettings.WebGL.dataCaching = previousCaching;
                AssetDatabase.SaveAssets();
            }
        }
    }
}
