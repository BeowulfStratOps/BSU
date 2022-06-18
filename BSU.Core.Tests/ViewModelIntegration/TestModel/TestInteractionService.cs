using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BSU.Core.ViewModel;

namespace BSU.Core.Tests.ViewModelIntegration.TestModel;

internal class TestInteractionService : IInteractionService
{
    private readonly Stack<IDialog> _dialogStack = new(); 

    public record MessagePopupDto(string Message, string Title);
    
    private async Task<TResult> Handle<TViewModel, TResult>(TViewModel viewModel)
    {
        var tcs = new TaskCompletionSource<object?>();
        var dialog = new Dialog<TViewModel>(viewModel, tcs);
        _dialogStack.Push(dialog);
        var result = await tcs.Task;
        var check = _dialogStack.Pop();
        if (check != dialog)
            throw new InvalidOperationException();
        return (TResult)result!;
    }

    public Task<bool> AddRepository(AddRepository viewModel) => Handle<AddRepository, bool>(viewModel);
    public Task<bool> AddStorage(AddStorage viewModel) => Handle<AddStorage, bool>(viewModel);

    public Task MessagePopup(string message, string title, MessageImageEnum image) => Handle<MessagePopupDto, object>(new MessagePopupDto(message, title));
    public Task<T> OptionsPopup<T>(string message, string title, Dictionary<T, string> options, MessageImageEnum image) where T : notnull
    {
        throw new NotImplementedException();
    }

    public Task<bool> SelectRepositoryStorage(SelectRepositoryStorage viewModel) => Handle<SelectRepositoryStorage, bool>(viewModel);
    public Task<bool> GlobalSettings(GlobalSettings vm)
    {
        throw new NotImplementedException();
    }

    public void CloseBsu()
    {
        throw new NotImplementedException();
    }

    public void SetViewModel(ViewModel.ViewModel vm)
    {
        if (_dialogStack.Any()) throw new InvalidOperationException();
        _dialogStack.Push(new Dialog<ViewModel.ViewModel>(vm, null!));
    }

    public object GetCurrentDialog() => _dialogStack.Peek();
}
