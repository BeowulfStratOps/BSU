using BSU.Core.Hashes;
using BSU.CoreCommon.Hashes;

namespace BSU.Core
{
    /// <summary>
    /// Represents a mod version an update is aiming for.
    /// </summary>
    public record UpdateTarget(HashCollection Hashes, string Title);
}
