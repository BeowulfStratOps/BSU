using System.Collections.Generic;
using BSU.CoreCommon.Hashes;

namespace BSU.Core
{
    /// <summary>
    /// Represents a mod version an update is aiming for.
    /// </summary>
    public record UpdateTarget(List<IModHash> Hashes, string Title);
}
