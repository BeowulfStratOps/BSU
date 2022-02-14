using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal static class FileHelper
{
    public static Dictionary<string, byte[]> CreateFiles(int match, int version)
    {
        var result = new Dictionary<string, byte[]>();

        for (int i = 0; i < 5; i++)
        {
            var data = new byte[20 * 2048];
            Array.Fill(data, (byte)(version % 256));
            result.Add($"addons/m{match}_p{i}.pbo", data);
        }

        return result;
    }

    public static void AssertFileEquality(Dictionary<string,byte[]> files1, Dictionary<string,byte[]> files2)
    {
        Assert.Equal(files1.Keys.ToHashSet(), files2.Keys.ToHashSet());
        foreach (var path in files1.Keys)
        {
            Assert.Equal(files1[path], files2[path]);
        }
    }
}
