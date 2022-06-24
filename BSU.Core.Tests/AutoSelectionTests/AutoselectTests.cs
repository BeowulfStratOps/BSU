using BSU.Core.Ioc;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Services;
using BSU.Core.Tests.Util;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests.AutoSelectionTests
{
    // TODO: re-use those model mocks.

    public class AutoselectTests : LoggedTest
    {
        private readonly AutoSelectionService _autoSelector;

        public AutoselectTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
            var services = new ServiceProvider();
            services.Add<IModActionService>(new ModActionService());
            services.Add<IStorageService>(new StorageService());
            services.Add<IConflictService>(new ConflictService(services));
            _autoSelector = new AutoSelectionService(services);
        }

        private static (MockModel model, MockRepo repo, MockStorage storage) GetModel()
        {
            var model = new MockModel();
            return (model, model.CreateRepo(), model.CreateStorage());
        }

        [Fact]
        private void SingleUse_AllLoaded_NoConflict()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod();

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod, selected.StorageMod);
        }

        [Fact]
        private void SingleUse_AllLoaded_Conflict_Use()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod();

            repo.AddMod(version: 3); // conflict

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod, selected.StorageMod);
        }

        [Fact]
        private void SingleUse_AllLoaded_Conflict_Update()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod(version: 2);

            var repo2 = model.CreateRepo();
            var mod2 = repo2.AddMod(version: 3); // conflict
            mod2.SetSelection(new ModSelectionStorageMod(storageMod));

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            Assert.IsType<ModSelectionNone>(selection);
        }

        [Fact]
        private void Precedence()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod();
            storage.AddMod(version: 2);

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod, selected.StorageMod);
        }

        [Fact]
        private void PreferNonSteam()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod();
            storage.AddMod(canWrite: false);

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod, selected.StorageMod);
        }

        [Fact]
        private void SwitchSteamToNonSteam()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var nonSteamMod = storage.AddMod();
            var steamMod = storage.AddMod(canWrite: false);

            repoMod.SetSelection(new ModSelectionStorageMod(steamMod));

            var selection = _autoSelector.GetAutoSelection(model, repoMod, IAutoSelectionService.SteamUsage.DontUseSteam, true);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(nonSteamMod, selected.StorageMod);
        }

        [Fact]
        private void SwitchNonSteamToSteam()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var nonSteamMod = storage.AddMod(identifier: "nonSteamMod");
            var steamMod = storage.AddMod(canWrite: false, identifier: "steamMod");

            repoMod.SetSelection(new ModSelectionStorageMod(nonSteamMod));

            var selection = _autoSelector.GetAutoSelection(model, repoMod, IAutoSelectionService.SteamUsage.UseSteamAndPreferIt, true);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(steamMod, selected.StorageMod);
        }

        [Fact]
        private void KeepSelectionFromPreviousRun()
        {
            var (model, repo, storage) = GetModel();
            storage.AddMod();
            var storageMod2 = storage.AddMod(state: StorageModStateEnum.Loading);
            var repoMod = repo.AddMod(previousSelection: PersistedSelection.FromSelection(new ModSelectionStorageMod(storageMod2)));

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod2, selected.StorageMod);
        }

        [Fact]
        private void MoveOnFromLoading()
        {
            var (model, repo, _) = GetModel();

            var repoMod = repo.AddMod();
            repoMod.SetSelection(new ModSelectionLoading());

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            Assert.IsType<ModSelectionNone>(selection);
        }
        
        [Fact]
        private void RepoModError()
        {
            var (model, repo, storage) = GetModel();

            storage.AddMod();
            var repoMod = repo.AddMod(state: LoadingState.Error);

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            Assert.IsType<ModSelectionDisabled>(selection);
        }
        
        [Fact]
        private void RepoModErrorPlusSteam()
        {
            var (model, repo, storage) = GetModel();

            storage.AddMod();
            storage.AddMod(0, 0, canWrite: false);
            var repoMod = repo.AddMod(state: LoadingState.Error);

            var selection = _autoSelector.GetAutoSelection(model, repoMod);

            Assert.IsType<ModSelectionDisabled>(selection);
        }
    }
}
