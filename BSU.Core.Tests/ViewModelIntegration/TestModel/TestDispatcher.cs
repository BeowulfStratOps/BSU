using System;
using BSU.Core.Concurrency;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel
{
    internal class TestDispatcher : IDispatcher
    {
        public void ExecuteSynchronized(Action action) => action();
    }
}
