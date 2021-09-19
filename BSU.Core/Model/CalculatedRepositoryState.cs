using System;

namespace BSU.Core.Model
{
    public enum CalculatedRepositoryStateEnum
    {
        NeedsUpdate, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts.
        NeedsDownload, // All mods are usable or need a download. TODO: combine download and update state?
        Ready, // All use
        RequiresUserIntervention, // Else
        InProgress, // All are ready or being worked on
        Loading
    }

    public record CalculatedRepositoryState(CalculatedRepositoryStateEnum State, bool IsPartial);
}
