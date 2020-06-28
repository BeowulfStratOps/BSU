using System;
using System.Collections.Generic;
using System.Linq;
using BSU.Core;
using BSU.Core.Model;
using BSU.Core.Sync;

namespace RealTest
{
    internal class ModelExecuter
    {
        private readonly Model _model;

        public ModelExecuter(Core core)
        {
            _model = core.Model;
        }

        public IUpdateState PrepareUpdate(string repoModPath, string storagModPath)
        {
            var repoName = repoModPath.Split("/")[0];
            var repoModName = repoModPath.Split("/")[1];
            
            var storageName = storagModPath.Split("/")[0];
            var storageModName = storagModPath.Split("/")[1];

            var repository = _model.Repositories.Single(r => r.Identifier == repoName);
            
            var repoMod = repository.Mods.Single(m => m.Identifier == repoModName);
            
            var storage = _model.Storages.Single(s => s.Identifier == storageName);
            var storageMod = storage.Mods.Single(m => m.Identifier == storageModName);

            var action = repoMod.Actions.GetValueOrDefault(storageMod);
            
            if (action != ModAction.Update) throw new InvalidOperationException();

            return storageMod.PrepareUpdate(repoMod);
        }

        public IUpdateState PrepareDownload(string repoModPath, string storageName, string identifier)
        {
            var repoName = repoModPath.Split("/")[0];
            var repoModName = repoModPath.Split("/")[1];

            var repository = _model.Repositories.Single(r => r.Identifier == repoName);
            var repoMod = repository.Mods.Single(m => m.Identifier == repoModName);
            
            var storage = _model.Storages.Single(s => s.Identifier == storageName);
            
            return storage.PrepareDownload(repoMod, identifier);
        }
    }
}