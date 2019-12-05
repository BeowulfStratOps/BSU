using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core.Sync
{


    public abstract class WorkUnit
    {
        protected ILocalMod _local;
        protected string _path;
        protected bool _done;

        public WorkUnit(ILocalMod local, string path)
        {
            _local = local;
            _path = path;
        }

        public Exception Error { get; private set; }
        public abstract void DoWork();
        public bool IsDone() => _done;
        internal void SetError(Exception e) => Error = e;
        public bool HasError() => Error != null;
    }
}
