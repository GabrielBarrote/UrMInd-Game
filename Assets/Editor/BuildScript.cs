using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Headless build for CI/CLI: `Unity.exe -batchmode -quit -executeMethod BuildScript.BuildWin64`.
// Outputs to Build/UrMIndGame/UrMIndGame.exe. Fails the process with exit 1 on error so
// the shell caller sees the failure and does not treat a broken build as success.
public static class BuildScript
{
    public static void BuildWin64()
    {
        const string outDir = "Build/UrMIndGame";
        const string exe = outDir + "/UrMIndGame.exe";
        Directory.CreateDirectory(outDir);

        var scenes = new[] { "Assets/City - 02 - Day.unity" };

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exe,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary s = report.summary;

        Debug.Log("[BuildScript] result=" + s.result
            + " size=" + s.totalSize
            + " time=" + s.totalTime
            + " errors=" + s.totalErrors
            + " warnings=" + s.totalWarnings
            + " output=" + s.outputPath);

        if (s.result != BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
