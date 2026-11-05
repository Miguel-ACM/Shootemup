using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class BuildHeavenPlayerProfile
{
    public static void Run()
    {
        if(!Application.isBatchMode||!Application.dataPath.Replace('\\','/').EndsWith("/Temp/UnlockReview/Assets"))throw new Exception("Isolated checkout only");
        PlayerSettings.companyName="ShootemupReview";PlayerSettings.productName="HeavenCpuPlayer";
        string path=Path.GetFullPath("Build/HeavenCPU/Shootemup.exe");Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),target=BuildTarget.StandaloneWindows64,locationPathName=path,options=BuildOptions.Development});
        File.WriteAllText("ReviewOutput/HeavenProfile/Build.txt",report.summary.result+"\nDevelopment Windows x64 player\nErrors="+report.summary.totalErrors+"; warnings="+report.summary.totalWarnings);
        EditorApplication.Exit(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded?0:1);
    }
}
