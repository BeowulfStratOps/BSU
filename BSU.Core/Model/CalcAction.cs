using System;
using BSU.Core.Hashes;

namespace BSU.Core.Model
{
    public partial class CalcAction
    {
        internal static ModAction CalculateAction(VersionHash repoHash, VersionHash storageHash, UpdateTarget job, UpdateTarget updating, bool canWrite)
        {
            if (job == null)
            {
                if (repoHash.IsMatch(storageHash) && updating == null)
                    return ModAction.Use;
                if (!canWrite) return ModAction.Unusable;
                var continuation = updating != null;
                return continuation ? ModAction.ContinueUpdate : ModAction.Update;
            }
            if (job.Hash == repoHash.GetHashString())
                return ModAction.Await;
            
            throw new InvalidOperationException("Wtf...");
        }
    }
}