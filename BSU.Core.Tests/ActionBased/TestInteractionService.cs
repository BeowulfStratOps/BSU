using System;
using BSU.Core.ViewModel;

namespace BSU.Core.Tests.ActionBased;

internal class TestInteractionService : IInteractionService
{
    public record MessagePopupDto(string Message, string Title);

    private readonly Func<ModelActionContext, object?> _handleInteraction;

    public TestInteractionService(Func<ModelActionContext, object?> handleInteraction)
    {
        _handleInteraction = handleInteraction;
    }

    private object? Handle(object viewModel, IDialogContext? dialogContext = null)
    {
        dialogContext ??= new DialogContext();
        return _handleInteraction(new ModelActionContext(viewModel, dialogContext));
    }

    public bool AddRepository(AddRepository viewModel) => (bool)Handle(viewModel, new TestClosable())!;
    public bool AddStorage(AddStorage viewModel)
    {
        throw new NotImplementedException();
    }

    public void MessagePopup(string message, string title)
    {
        throw new NotImplementedException();
    }

    public bool? YesNoCancelPopup(string message, string title)
    {
        throw new NotImplementedException();
    }

    public bool YesNoPopup(string message, string title)
    {
        throw new NotImplementedException();
    }

    public bool SelectRepositoryStorage(SelectRepositoryStorage viewModel) => (bool)Handle(viewModel)!;

    public bool PresetSettings(PresetSettings vm) => (bool)Handle(vm)!;
}
