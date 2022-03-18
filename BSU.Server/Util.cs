using System.IO;

namespace BSU.Server;

public static class Util
{
    public static Stream StringToStream(string text)
    {
        var memStream = new MemoryStream();
        var writer = new StreamWriter(memStream);
        writer.Write(text);
        writer.Flush();
        memStream.Position = 0;
        return memStream;
    }

    public static NormalizedPath GetRelativePath(DirectoryInfo basePath, FileInfo file)
    {
        return "/" + Path.GetRelativePath(basePath.FullName, file.FullName).Replace("\\", "/");
    }

    public static string GetAbsolutePath(DirectoryInfo basePath, NormalizedPath path)
    {
        // TODO: this won't work if we have files with uppercase letters on a case-sensitive filesystem
        return Path.Combine(basePath.FullName, ((string)path)[1..]);
    }
}
