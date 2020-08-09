namespace BSU.Core.Model
{
    internal enum CalculatedRepositoryState
    {
        Loading, // At least one loading
        NeedsUpdate, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts. 
        NeedsDownload, // All mods are usable or need a download
        Ready, // All use
        RequiresUserIntervention, // Else
        InProgress // All are ready or being worked on
    }
}