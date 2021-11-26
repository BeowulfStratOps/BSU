using System.Threading.Tasks;

namespace BSU.Core.Model.Updating
{
    internal record ModUpdateInfo(Task<UpdateResult> Update, IModelStorageMod Mod);

    internal enum UpdateResult
    {
        Success,
        Failed,
        FailedSharingViolation
    }
}
