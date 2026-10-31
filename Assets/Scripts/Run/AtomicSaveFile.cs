using System;
using System.IO;
using System.Text;

// Keep the last committed save intact until its replacement has reached disk.
public static class AtomicSaveFile
{
    public static void Write(string path, string contents)
    {
        string temporary = path + ".tmp";
        byte[] bytes = Encoding.UTF8.GetBytes(contents);
        using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush(true);
        }
        if (File.Exists(path))
            File.Replace(temporary, path, path + ".bak");
        else
            File.Move(temporary, path);
    }

    public static string Read(string path, Func<string, bool> valid)
    {
        // Prefer committed progress. A temporary file can recover an interrupted first save.
        foreach (string candidate in new[] { path, path + ".tmp", path + ".bak" })
        {
            try
            {
                if (!File.Exists(candidate))
                    continue;
                string contents = File.ReadAllText(candidate);
                if (valid(contents))
                    return contents;
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            catch (ArgumentException) { } // Invalid serialized data; try the next copy.
        }
        return null;
    }
}
