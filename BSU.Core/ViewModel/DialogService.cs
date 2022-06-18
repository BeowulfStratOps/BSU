using System;
using System.Threading.Tasks;
using BSU.Core.Ioc;

namespace BSU.Core.ViewModel;

internal class DialogService : IDialogService
{
    private readonly IServiceProvider _services;
    private readonly IInteractionService _interactionService;

    public DialogService(IServiceProvider services)
    {
        _interactionService = services.Get<IInteractionService>();
        _services = services;
    }

    public async Task<AddStorageDialogResult?> AddStorage()
    {
        var vm = new AddStorage(_services);
        if (!await _interactionService.AddStorage(vm)) return null;
        var type = vm.GetStorageType();
        var name = vm.GetName();
        var path = vm.GetPath();
        return new AddStorageDialogResult(type, name, path);
    }
}

internal interface IDialogService
{
    Task<AddStorageDialogResult?> AddStorage();
}

internal record AddStorageDialogResult(string Type, string Name, string Path);
