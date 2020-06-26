namespace BSU.Core.Model
{
    internal enum ModAction
    {
        Update,
        ContinueUpdate,
        Await,
        Use,
        AbortAndUpdate,
        Unusable,
        Loading,
        Error
    }
}