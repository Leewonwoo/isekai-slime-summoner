using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CrossDefense.Editor
{
    public static class GitHubPagesBuild
    {
        const string OutputDirectoryName = "PagesBuild";
        const int WebWidth = 1080;
        const int WebHeight = 1920;
        const string LayoutVersion = "9x16-1080x1920-korean-v1";

        const string ResponsiveStyle = @"

/* GitHub Pages: keep the game canvas centered at a responsive 9:16 ratio. */
html, body { width: 100%; height: 100%; overflow: hidden; background: #000; }
#unity-container.unity-desktop,
#unity-container.unity-mobile {
  position: fixed;
  left: 50%;
  top: 50%;
  width: min(100vw, calc(100vh * 9 / 16));
  height: auto;
  aspect-ratio: 9 / 16;
  transform: translate(-50%, -50%);
}
#unity-canvas,
#unity-canvas.unity-mobile {
  display: block;
  width: 100% !important;
  height: 100% !important;
}
#unity-footer { display: none; }
";

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

                ApplyResponsiveLayout(outputDirectory);
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

        static void ApplyResponsiveLayout(string outputDirectory)
        {
            string indexPath = Path.Combine(outputDirectory, "index.html");
            string index = File.ReadAllText(indexPath);
            index = Regex.Replace(
                index,
                @"<canvas id=""unity-canvas"" width=\d+ height=\d+",
                $"<canvas id=\"unity-canvas\" width={WebWidth} height={WebHeight}");
            index = Regex.Replace(
                index,
                @"canvas\.style\.width = ""\d+px"";\s*canvas\.style\.height = ""\d+px"";",
                "canvas.style.width = \"100%\";\n        canvas.style.height = \"100%\";");
            index = index.Replace(
                "// config.matchWebGLToCanvasSize = false;",
                "config.matchWebGLToCanvasSize = false;");
            index = index.Replace(
                "href=\"TemplateData/style.css\"",
                $"href=\"TemplateData/style.css?v={LayoutVersion}\"");
            index = index.Replace(
                "dataUrl: buildUrl + \"/PagesBuild.data.unityweb\"",
                $"dataUrl: buildUrl + \"/PagesBuild.data.unityweb?v={LayoutVersion}\"");
            File.WriteAllText(indexPath, index);

            string stylePath = Path.Combine(outputDirectory, "TemplateData", "style.css");
            File.AppendAllText(stylePath, ResponsiveStyle);
        }
    }
}
