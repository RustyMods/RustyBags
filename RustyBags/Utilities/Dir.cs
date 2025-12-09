using System;
using System.Collections.Generic;
using System.IO;

namespace RustyBags.Utilities;

public class Dir 
{
    public readonly string Path;
    public Dir(string dir, string name)
    {
        Path = System.IO.Path.Combine(dir, name);
        EnsureDirectoryExists();
    }

    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(Path))
        {
            Directory.CreateDirectory(Path);
        }
    }

    public void WriteAllLines(string fileName, List<string> lines)
    {
        string fullPath = System.IO.Path.Combine(Path, fileName);
        EnsureDirectoryExists();
        File.WriteAllLines(fullPath, lines);
    }
}