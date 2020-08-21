using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core.Model.Utility;

namespace BSU.Core.Model
{
    public class RepositoryUpdate
    {
        public List<Promise<IUpdateState>> Promises { get; private set; }
        public List<IUpdateState> Updates { get; private set; }

        public void Set(List<Promise<IUpdateState>> promises)
        {
            if (Promises != null) throw new InvalidOperationException();
            Promises = promises;
            foreach (var promise in Promises)
            {
                promise.OnValue += CheckAllSetUp;
                promise.OnError += CheckAllSetUp;
            }
            CheckAllSetUp();
        }

        private void CheckAllSetUp()
        {
            if (Promises.All(p => p.HasError || p.HasValue))
                AllDone?.Invoke();
        }

        public event Action AllDone;

        public void Abort()
        {
            // TODO: implement for setUp
            // TODO: implement for prepared
            throw new NotImplementedException();
        }

        public void Prepare()
        {
            // TODO: ensure all done
            // TODO: roll back errored ones
            Updates = Promises.Where(p => p.HasValue).Select(p => p.Value).ToList();
            foreach (var update in Updates)
            {
                update.OnPrepared += CheckAllPrepared;
                update.Prepare();
            }
        }

        private void CheckAllPrepared()
        {
            if (Updates.All(u => u.IsPrepared))
                AllPrepared?.Invoke();
        }

        public void Commit()
        {
            // TODO: rollback failed ones
            foreach (var update in Updates)
            {
                update.Commit();
            }
        }

        public event Action AllPrepared;

        public long GetTotalBytesToDownload()
        {
            return Updates.Sum(u => u.GetPrepStats());
        }
    }
}
