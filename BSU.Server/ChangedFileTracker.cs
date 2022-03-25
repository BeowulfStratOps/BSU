using System;
using System.Collections.Generic;
using System.IO;

namespace BSU.Server;

public class ChangedFileTracker
{
    private readonly StreamWriter _writer;
    private readonly object _fileLock = new();
    private readonly HashSet<string> _existingEntries = new();

    public ChangedFileTracker(string path)
    {
        if (File.Exists(path))
        {
            var fileContents = File.ReadAllText(path);
            _existingEntries = new HashSet<string>(fileContents.Split("\n",
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
        }

        _writer = new StreamWriter(path, true);
    }

    public void AddChangedFilePath(NormalizedPath path)
    {
        if (_existingEntries.Contains(path))
            return;
        lock (_fileLock)
        {
            _writer.WriteLine(path);
            _writer.Flush();
        }
    }
}
