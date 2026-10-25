#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEditor;

public static class TunnelProfileExport
{
    const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
    public static void Run()
    {
        Export("Baseline");
        EditorApplication.Exit(0);
    }

    public static void Export(string label)
    {
        string folder = ProfileTunnelBoss.Folder;
        var output = new StringBuilder();
        try
        {
            var driver = typeof(Editor).Assembly.GetType("UnityEditorInternal.ProfilerDriver") ?? Type.GetType("UnityEditorInternal.ProfilerDriver, UnityEditor");
            File.WriteAllLines(folder + "/ProfilerAPI.txt", driver.GetMethods(Flags).Where(m => m.Name.Contains("FrameData") || m.Name.Contains("Profile")).Select(m => m.ToString()));
            var load = driver.GetMethods(Flags).First(m => m.Name == "LoadProfile" && m.GetParameters().Length == 2);
            load.Invoke(null, new object[] { Path.GetFullPath("Temp/Tunnel-" + label + ".raw"), false });
            int first = Convert.ToInt32(driver.GetProperty("firstFrameIndex", Flags).GetValue(null)), last = Convert.ToInt32(driver.GetProperty("lastFrameIndex", Flags).GetValue(null));
            output.AppendLine("Frames " + first + ".." + last);
            var raw = driver.GetMethods(Flags).First(m => m.Name == "GetRawFrameDataView" && m.GetParameters().Length == 2);
            var frames = new List<(int index, double ms)>();
            for (int i = first; i <= last; i++)
            {
                var data = raw.Invoke(null, new object[] { i, 0 });
                var type = data.GetType();
                if ((bool)type.GetProperty("valid").GetValue(data))
                    frames.Add((i, Convert.ToDouble(type.GetProperty("frameTimeMs").GetValue(data))));
                ((IDisposable)data).Dispose();
            }
            foreach (var frame in frames.OrderByDescending(f => f.ms).Take(12))
            {
                output.AppendLine("\nFRAME " + frame.index + " " + frame.ms.ToString("F3") + " ms");
                var data = raw.Invoke(null, new object[] { frame.index, 0 });
                var type = data.GetType();
                int count = (int)type.GetProperty("sampleCount").GetValue(data);
                MethodInfo name = type.GetMethod("GetSampleName"), time = type.GetMethod("GetSampleTimeMs");
                var samples = new List<(string name, double ms)>();
                for (int i = 0; i < count; i++)
                {
                    double ms = Convert.ToDouble(time.Invoke(data, new object[] { i }));
                    if (ms < .25)
                        continue;
                    samples.Add(((string)name.Invoke(data, new object[] { i }), ms));
                }
                foreach (var sample in samples.OrderByDescending(s => s.ms).Take(35))
                    output.AppendLine(sample.ms.ToString("F3") + " ms " + sample.name);
                ((IDisposable)data).Dispose();
            }
        }
        catch (Exception e) { output.AppendLine(e.ToString()); }
        File.WriteAllText(folder + "/" + label + "-Stacks.txt", output.ToString());
    }
}
#endif
