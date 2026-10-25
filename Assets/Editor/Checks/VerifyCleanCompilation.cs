#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Compilation;

// Run in an isolated project: rebuild every script assembly without starting gameplay.
[InitializeOnLoad]
public static class VerifyCleanCompilation
{
    const string Key = "CleanCompilationReview", Folder = "ArtSource/CodeCleanupReview";
    static VerifyCleanCompilation()
    {
        CompilationPipeline.compilationFinished += _ =>
          {
              if (SessionState.GetBool(Key, false))
                  SessionState.SetBool(Key + "Finished", true);
          };
        EditorApplication.update += Finish;
    }

    public static void Run()
    {
        Directory.CreateDirectory(Folder);
        File.WriteAllText(Folder + "/Assemblies.txt", "");
        File.WriteAllText(Folder + "/Diagnostics.txt", "");
        SessionState.SetBool(Key, true);
        SessionState.SetBool(Key + "Finished", false);
        CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
    }

    static void Finish()
    {
        if (!SessionState.GetBool(Key, false) || !SessionState.GetBool(Key + "Finished", false) || EditorApplication.isCompiling || EditorApplication.isUpdating)
            return;
        SessionState.SetBool(Key, false);
        // Unity 6's clean-cache path does not emit assemblyCompilationFinished for every
        // rebuilt assembly. The fresh compiler log records those Csc jobs and diagnostics.
        string log;
        using (var stream = new FileStream(UnityEngine.Application.consoleLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(stream))
            log = reader.ReadToEnd();
        var assemblies = Regex.Matches(log, @"\bCsc\s+.*?[/\\]([^/\\\s]+)\.dll\b").Cast<Match>().Select(match => match.Groups[1].Value).Distinct().OrderBy(name => name).ToArray();
        var diagnostics = Regex.Matches(log, @"(?m)^.*\b(?:warning|error)\s+[A-Z]+\d+\s*:.*$").Cast<Match>().Select(match => match.Value.Trim()).Distinct().ToArray();
        File.WriteAllLines(Folder + "/Assemblies.txt", assemblies);
        File.WriteAllLines(Folder + "/Diagnostics.txt", diagnostics);
        bool success = diagnostics.Length == 0 && new[] { "Assembly-CSharp", "Assembly-CSharp-Editor", "PathCreator", "PathCreatorEditor" }.All(assemblies.Contains);
        File.WriteAllText(Folder + "/Verification.txt", (success ? "PASS" : "FAIL") + "\nFull Unity script rebuild with CleanBuildCache; no Play mode.\n" +
            assemblies.Length + " assemblies compiled; " + diagnostics.Count(line => line.Contains("warning ")) + " warnings; " + diagnostics.Count(line => line.Contains("error ")) + " errors.\n" +
            "Required game, editor and PathCreator assemblies: " + string.Join(", ", assemblies.Where(name => name.StartsWith("Assembly-CSharp") || name.StartsWith("PathCreator"))) + "\n" + string.Join("\n", diagnostics));
        CheckHarness.Exit(success ? 0 : 1);
    }
}
#endif
