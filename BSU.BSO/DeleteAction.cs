using System.Threading;
using BSU.CoreInterface;

namespace BSU.BSO
{
    class DeleteAction : IWorkUnit
    {
        private bool _done;

        public DeleteAction(string path)
        {

        }

        public void DoWork()
        {
            // TODO: implement
            Thread.Sleep(500);
            _done = true;
        }

        public bool IsDone() => _done;
    }
}