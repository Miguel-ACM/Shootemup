using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;

// Lets the Play mode checks run either on their own from a terminal
// (Unity -batchmode -executeMethod VerifyX.Run, which quits with the result) or as tests in
// Unity's Test Runner, where the test enters and leaves Play mode itself and nothing quits.
// State lives in SessionState because entering Play mode reloads every script.
public static class CheckHarness
{
    const string UnderTestKey = "CheckHarness.UnderTest", TestSessionKey = "CheckHarness.TestSession", ReportKey = "CheckHarness.Report",
        DeadlineKey = "CheckHarness.Deadline", StartErrorKey = "CheckHarness.StartError";

    public static bool UnderTest => SessionState.GetBool(UnderTestKey, false);

    // Checks call these instead of setting EditorApplication.isPlaying or calling Exit directly.
    public static void EnterPlayMode()
    {
        if (!UnderTest)
            EditorApplication.isPlaying = true;
    }

    public static void ExitPlayMode()
    {
        if (!UnderTest)
            EditorApplication.isPlaying = false;
    }

    // A finished check's update hook can still fire after its test ends, so once tests have run
    // in this editor session no check may quit it.
    public static void Exit(int code)
    {
        if (!UnderTest && !SessionState.GetBool(TestSessionKey, false))
            EditorApplication.Exit(code);
    }

    // Prepares a check for a test: the test then yields EnterPlayMode, waits for Finished,
    // yields ExitPlayMode and calls AssertPassed.
    public static void Begin(Action start, string report, double timeoutSeconds)
    {
        SessionState.SetBool(UnderTestKey, true);
        SessionState.SetBool(TestSessionKey, true);
        SessionState.SetString(ReportKey, report);
        SessionState.SetFloat(DeadlineKey, (float)(EditorApplication.timeSinceStartup + timeoutSeconds));
        SessionState.EraseString(StartErrorKey);
        if (File.Exists(report))
            File.Delete(report);
        try
        {
            start();
        }
        catch (Exception e)
        {
            SessionState.SetString(StartErrorKey, e.ToString());
        }
    }

    public static bool Finished =>
        SessionState.GetString(StartErrorKey, "") != "" ||
        File.Exists(SessionState.GetString(ReportKey, "")) ||
        EditorApplication.timeSinceStartup > SessionState.GetFloat(DeadlineKey, 0);

    public static void AssertPassed()
    {
        string report = SessionState.GetString(ReportKey, ""), startError = SessionState.GetString(StartErrorKey, "");
        SessionState.EraseBool(UnderTestKey);
        SessionState.EraseString(ReportKey);
        SessionState.EraseFloat(DeadlineKey);
        SessionState.EraseString(StartErrorKey);
        if (startError != "")
            Assert.Fail("The check failed before Play mode:\n" + startError);
        if (!File.Exists(report))
            Assert.Fail("Timed out without writing " + report);
        string text = File.ReadAllText(report);
        if (!text.StartsWith("PASS"))
            Assert.Fail(text);
    }
}
