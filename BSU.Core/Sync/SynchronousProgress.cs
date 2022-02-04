using System;

namespace BSU.Core.Sync;

public class SynchronousProgress<T> : IProgress<T>
{
    public void Report(T value)
    {
        ProgressChanged?.Invoke(this, value);
    }

    public event EventHandler<T>? ProgressChanged;
}
