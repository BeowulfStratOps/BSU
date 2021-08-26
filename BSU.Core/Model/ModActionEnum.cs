namespace BSU.Core.Model
{
    internal enum ModActionEnum
    {
        Update,
        ContinueUpdate,
        Await,
        Use,
        AbortAndUpdate,
        Unusable, // should never reach the user
    }
}
