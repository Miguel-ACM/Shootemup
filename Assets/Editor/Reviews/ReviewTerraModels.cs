#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;

/// <summary>Captures every Enemy Editor model, for comparing procedural hulls with Terra models.</summary>
public static class ReviewTerraModels
{
    public static void CaptureAll()
    {
        string folder = "ArtSource/Terra/Captures/" + (Environment.GetEnvironmentVariable("TERRA_CAPTURE") ?? "After");
        Directory.CreateDirectory(folder);
        try
        {
            var window = EnemyEditorWindow.Open();
            foreach (var info in EnemyEditorWindow.Catalog)
            {
                window.Select(info.type.Name);
                window.CaptureModel(folder + "/" + info.level + "-" + info.type.Name + ".png");
            }
            window.Close();
            File.WriteAllText(folder + "/Done.txt", "PASS " + EnemyEditorWindow.Catalog.Count);
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            File.WriteAllText(folder + "/Done.txt", "FAIL\n" + e);
            EditorApplication.Exit(1);
        }
    }
}
#endif
