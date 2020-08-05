namespace BSU.Core.Model
{
    internal enum CalculatedRepositoryState
    {
        Loading, // At least one loading
        NeedsUpdate, // auto selected previously used, other auto selection worked without any conflicts, no internal conflicts. 
        NeedsDowload, // All mods are useable or need a download
        Ready, // All use
        RequiresUserIntervention // Else
    }
}