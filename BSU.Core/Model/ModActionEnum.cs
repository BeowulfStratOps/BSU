namespace BSU.Core.Model
{
    public enum ModActionEnum
    {
        Update,
        ContinueUpdate,
        Await,
        Use,
        AbortAndUpdate,
        Unusable, // should never reach the user
        AbortActiveAndUpdate,
        Loading
    }
}
