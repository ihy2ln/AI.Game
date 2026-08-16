#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.EditorTools
{
    /// <summary>
    /// Windows standalone build of just the battle scene -- for verifying the auto-battle
    /// slice actually plays (there's no Android adb on this dev machine, and headless
    /// batchmode can compile/test logic but can't show what it looks like on screen).
    /// Not a shipping build config, just a fast local verification build.
    /// </summary>
    public static class BuildBattleStandalone
    {
        [MenuItem("AI.Game/Battle/Build Windows Standalone (dev)")]
        public static void Build()
        {
            BattleSceneBuilder.CreateBattleScene();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Battle.unity" },
                locationPathName = "Builds/BattleStandalone/AI.Game-Battle.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[AI.Game] Build result: {summary.result}, "
                + $"{summary.totalErrors} errors, {summary.totalWarnings} warnings, "
                + $"output: {summary.outputPath}");

            if (summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
    }
}
#endif
