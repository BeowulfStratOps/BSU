using System;
using BSU.Core.Sync;
using System.Threading;

namespace BSU.Core.View
{
    internal class SimpleWorkUnit : WorkUnit
    {
        private readonly Action _action;

        public SimpleWorkUnit(Action action, CancellationToken token) : base(token)
        {
            _action = action;
        }

        protected override void DoWork(CancellationToken token)
        {
            _action();
        }
    }
}