using System;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal class TestModelInterface
{
    private readonly Action<Action, bool> _doInModelThread;

    public TestModelInterface(Action<Action, bool> doInModelThread)
    {
        _doInModelThread = doInModelThread;
    }

    public void DoInModelThread(Action action, bool waitForWork)
    {
        _doInModelThread(action, waitForWork);
    }
}
