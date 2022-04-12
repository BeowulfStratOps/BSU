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
        public AutoselectTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

            Assert.Null(selection);
        }

        [Fact]
        private void Precedence()
        {
            var (model, repo, storage) = GetModel();
            var repoMod = repo.AddMod();
            var storageMod = storage.AddMod();
            storage.AddMod(version: 2);

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod, AutoSelectorCalculation.SteamUsage.DontUseSteam, true);

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

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod, AutoSelectorCalculation.SteamUsage.UseSteamAndPreferIt, true);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(steamMod, selected.StorageMod);
        }

        [Fact]
        private void KeepSelectionFromPreviousRun()
        {
            var (model, repo, storage) = GetModel();
            var storageMod1 = storage.AddMod();
            var storageMod2 = storage.AddMod(state: StorageModStateEnum.Loading);
            var repoMod = repo.AddMod(previousSelection: PersistedSelection.FromSelection(new ModSelectionStorageMod(storageMod2)));

            var selection = AutoSelectorCalculation.GetAutoSelection(model, repoMod);

            var selected = Assert.IsType<ModSelectionStorageMod>(selection);
            Assert.Equal(storageMod2, selected.StorageMod);
        }
    }
}
