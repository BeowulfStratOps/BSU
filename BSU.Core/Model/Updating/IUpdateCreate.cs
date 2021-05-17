using System;
using System.Threading.Tasks;
using BSU.Core.Hashes;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model.Updating
{
    public interface IUpdateState
    {
        IProgressProvider ProgressProvider { get; }
        MatchHash GetTargetMatch();
        VersionHash GetTargetVersion();
        void Abort();
        public event Action OnEnded;
    }

    public interface IUpdateCreated : IUpdateState
    {
        Task<IUpdatePrepared> Prepare();
    }

    public interface IUpdatePrepared : IUpdateState
    {
        Task<IUpdateDone> Update();
        int GetStats();
    }

    public interface IUpdateDone
    {

    }

    public interface IUpdateCreate : IUpdateState
    {
        Task<IUpdateCreated> Create();
    }

    public enum UpdateState
    {
        NotCreated,
        Creating,
        Created,
        Preparing,
        Prepared,
        Updating,
        Updated,
        Aborted,
        Errored
    }
}
