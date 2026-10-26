#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Builds only from the isolated review checkout, leaving Miguel's open editor
// and existing executable alone. The caller supplies the deliverable path.
public static class BuildCombatRework
{
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/Temp/CombatReworkReview/"))
            throw new Exception("Use the isolated CombatReworkReview project");
        string output = VerifyCombatRework.Argument("--rework-output", "");
        if (!Path.IsPathRooted(output) || Path.GetExtension(output) != ".exe")
            throw new Exception("Supply an absolute .exe path with --rework-output");
        string review = VerifyCombatRework.Folder + "/Verification.txt";
        if (!File.Exists(review) || !File.ReadAllText(review).StartsWith("PASS"))
            throw new Exception("Complete the graphical review before building");
        VerifyCombatRework.ValidateAuthoring();
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            target = BuildTarget.StandaloneWindows64,
            locationPathName = output,
            options = BuildOptions.None
        });
        bool success = report.summary.result == BuildResult.Succeeded && File.Exists(output);
        File.WriteAllText(VerifyCombatRework.Folder + "/Build.txt",
            (success ? "PASS" : "FAIL") + "\n" + report.summary.result + "\n" +
            "Windows x64 player: " + output + "\n" +
            "Errors=" + report.summary.totalErrors + "; warnings=" + report.summary.totalWarnings +
            "; bytes=" + report.summary.totalSize + "; duration=" + report.summary.totalTime + "\n" +
            string.Join("\n", report.steps.SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Warning || message.type == LogType.Error)
                .Select(message => message.type + ": " + message.content).Distinct()) + "\n");
        EditorApplication.Exit(success ? 0 : 1);
    }
}
#endif
