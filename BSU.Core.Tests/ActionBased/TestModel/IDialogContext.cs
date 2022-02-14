using BSU.Core.ViewModel.Util;

namespace BSU.Core.Tests.ActionBased.TestModel;

internal interface IDialogContext
{
    void SetResult(object? result);
    bool TryGetResult(out object? result);
}

internal class DialogContext : IDialogContext
{
    private object? _result;
    private bool _isSet;

    public void SetResult(object? result)
    {
        _result = result;
        _isSet = true;
    }

    public bool TryGetResult(out object? result)
    {
        result = _result;
        return _isSet;
    }
}

internal class TestClosable : DialogContext, ICloseable
{
    public void Close(bool result)
    {
        SetResult(result);
    }
}
