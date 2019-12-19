using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreCommon;

namespace BSU.Core.Sync
{
    internal abstract class WorkUnit
    {
        protected ILocalMod _local;
        protected string _path;
        protected bool _done;

        public WorkUnit(ILocalMod local, string path)
        {
            _local = local;
            _path = path;
        }

        private Exception _error;
        public abstract void DoWork();
        public bool IsDone() => _done;
        internal void SetError(Exception e) => _error = e;
        public bool HasError() => _error != null;
        public Exception GetError() => _error;
    }
}
