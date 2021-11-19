using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BSU.Core.Model;
using NLog;

namespace BSU.Core.Services
{
    internal class Helper
    {
        private readonly IModel _model;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        internal Helper(IModel model, StructureEventCombiner eventCombiner)
        {
            _model = model;
            eventCombiner.AnyChange += () => AnyChange?.Invoke();
        }

        public event Action AnyChange;

        public CalculatedRepositoryStateEnum GetRepositoryState(IModelRepository repo)
        {
            if (repo.State == LoadingState.Loading) return CalculatedRepositoryStateEnum.Loading;
            if (repo.State == LoadingState.Error) return CalculatedRepositoryStateEnum.Error;

            var mods = repo.GetMods();

            (RepositoryModActionSelection selection, ModActionEnum? action, bool hasError) GetModSelection(IModelRepositoryMod mod)
            {
                var selection = mod.GetCurrentSelection();
                var action = selection is not RepositoryModActionStorageMod actionStorageMod
                    ? null
                    : (ModActionEnum?)CoreCalculation.GetModAction(mod, actionStorageMod.StorageMod);
                var hasError = GetErrorForSelection(mod) != null;
                return (selection, action, hasError);
            }

            var infos = mods.Select(GetModSelection).ToList();

            var calculatedState = CoreCalculation.CalculateRepositoryState(infos);
            _logger.Trace("Repo {0} calculated state: {1}", repo.Identifier, calculatedState);
            return calculatedState;
        }

        public string GetErrorForSelection(IModelRepositoryMod mod)
        {
            var selection = mod.GetCurrentSelection();

            switch (selection)
            {
                case null:
                    return "Select an action";
                case RepositoryModActionDoNothing:
                    return null;
                case RepositoryModActionDownload when string.IsNullOrWhiteSpace(mod.DownloadIdentifier):
                    return "Name must be a valid folder name";
                case RepositoryModActionDownload when mod.DownloadIdentifier.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || mod.DownloadIdentifier.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0:
                    return "Invalid characters in name";
                case RepositoryModActionDownload selectStorage:
                {
                    var folderExists = selectStorage.DownloadStorage.HasMod(mod.DownloadIdentifier);
                    return folderExists ? "Name in use" : null;
                }
                case RepositoryModActionStorageMod selectMod when CoreCalculation.GetModAction(mod ,selectMod.StorageMod) == ModActionEnum.AbortActiveAndUpdate:
                    return "This mod is currently being updated";
                case RepositoryModActionStorageMod selectMod:
                {
                    var conflicts = GetConflictsUsingMod(mod, selectMod.StorageMod, _model.GetRepositoryMods());
                    if (!conflicts.Any())
                        return null;

                    var conflictNames = conflicts.Select(c => $"{c}");
                    return "In conflict with: " + string.Join(", ", conflictNames);
                }
                default:
                    return null;
            }
        }

        public static List<IModelRepositoryMod> GetConflictsUsingMod(IModelRepositoryMod repoMod, IModelStorageMod storageMod, List<IModelRepositoryMod> allRepoMods)
        {
            // TODO: test case
            var result = new List<IModelRepositoryMod>();

            // TODO: do in parallel?
            foreach (var mod in allRepoMods)
            {
                if (mod == repoMod) continue;
                if (mod.GetCurrentSelection() is not RepositoryModActionStorageMod otherMod || otherMod.StorageMod != storageMod) continue;
                if (CoreCalculation.IsConflicting(repoMod, mod, storageMod))
                    result.Add(mod);
            }

            return result;
        }

        public static string GetAvailableDownloadIdentifier(IModelStorage storage, string baseIdentifier)
        {
            bool Exists(string name)
            {
                return storage.GetMods().Any(
                    m => string.Equals(m.Identifier, name, StringComparison.InvariantCultureIgnoreCase));
            }

            if (!Exists(baseIdentifier))
                return baseIdentifier;
            var i = 1;
            while (true)
            {
                var name = $"{baseIdentifier}_{i}";
                if (!Exists(name))
                    return name;
                i++;
            }
        }

        public IEnumerable<IModelRepositoryMod> GetUsedBy(IModelStorageMod storageMod)
        {
            var repositoryMods = _model.GetRepositoryMods();
            var result = new List<IModelRepositoryMod>();
            foreach (var repositoryMod in repositoryMods)
            {
                var selection = repositoryMod.GetCurrentSelection();
                if (selection is RepositoryModActionStorageMod mod && mod.StorageMod == storageMod)
                    result.Add(repositoryMod);
            }

            return result;
        }
    }
}
