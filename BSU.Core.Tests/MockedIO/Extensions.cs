using System.Linq;
using BSU.Core.Model;

namespace BSU.Core.Tests.MockedIO;

public static class Extensions
{
    internal static IModelStorageMod GetStorageMod(this IModel model, string name)
    {
        return model.GetStorageMods().Single(r => r.Identifier == "@" + name);
    }

    internal static IModelRepositoryMod GetRepoMod(this IModel model, string name)
    {
        return model.GetRepositoryMods().Single(r => r.Identifier == "@" + name);
    }
}
