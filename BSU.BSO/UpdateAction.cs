using System.Threading;
using BSU.CoreInterface;
using Microsoft.VisualBasic.CompilerServices;

namespace BSU.BSO
{
    class UpdateAction : IWorkUnit
    {
        private readonly long _sizeTotal;
        private long _sizeTodo;
        private bool _done;

        public UpdateAction(string path, long sizeTotal)
        {
            _sizeTotal = sizeTotal;
            _sizeTodo = sizeTotal;
        }

        public long GetBytesTotal() => _sizeTotal;
        public long GetBytesRemaining() => _sizeTodo;

        public void DoWork()
        {
            // TODO: implement
            Thread.Sleep(500);
            _sizeTodo = 0;
            _done = true;
        }

        public bool IsDone() => _done;
    }
}