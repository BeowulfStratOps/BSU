namespace BSU.Core.Persistence
{
    internal interface IStorageState
    {
        IStorageModState GetMod(string identifier);
    }
}