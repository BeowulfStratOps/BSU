using System;

namespace BSU.Server;

public class NormalizedPath : IEquatable<NormalizedPath>, IComparable<NormalizedPath>, IComparable
{
    private readonly string _path;

    public NormalizedPath(string path)
    {
        _path = Normalized(path);
    }

    private static string Normalized(string path)
    {
        if (!path.StartsWith("/"))
            throw new ArgumentException($"'{path}' does not have a leading /");
        if (path.Contains('\\'))
            throw new ArgumentException($"'{path}' uses windows style separators");
        return path.ToLowerInvariant();
    }

    public static implicit operator string(NormalizedPath normalizedPath) => normalizedPath._path;

    public static implicit operator NormalizedPath(string path) => new (path);

    public string GetExtension() => _path.Split(".")[^1]; // TODO: handle file without extension

    public bool Equals(NormalizedPath? other) => other != null && _path == other._path;

    public override bool Equals(object? obj) => obj is NormalizedPath np && Equals(np);

    public override int GetHashCode() => _path.GetHashCode();

    public string GetFileName() => _path.Split("/")[^1];

    public override string ToString() => _path;

    public int CompareTo(NormalizedPath? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return string.Compare(_path, other._path, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is NormalizedPath other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(NormalizedPath)}");
    }
}
