using System;
using System.Collections.Generic;

namespace BSU.Core.Tests.ActionBased;

internal static class Helper
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
}
