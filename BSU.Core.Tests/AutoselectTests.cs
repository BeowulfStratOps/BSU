using System;
using System.Collections.Generic;
using BSU.Core.Hashes;
using BSU.Core.Model;
using BSU.Core.Persistence;
using BSU.Core.Tests.Mocks;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace BSU.Core.Tests
{
    public class AutoselectTests : LoggedTest
    {
        public AutoselectTests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        private IModelStorageMod AddAction(Dictionary<IModelStorageMod, ModAction> mods, ModActionEnum actionType,
            bool hasConflict)
        {
            var modAction = new ModAction(actionType, null, VersionHash.CreateEmpty(), new HashSet<ModAction>());

            if (hasConflict) modAction.Conflicts.Add(new ModAction(ModActionEnum.Use, null, null, new HashSet<ModAction>()));
            
            var storageMod = new Mock<IModelStorageMod>(MockBehavior.Strict);
            var storageModIdentifier = new StorageModIdentifiers(Guid.NewGuid().ToString(), Guid.NewGuid().ToString());
            storageMod.Setup(s => s.GetStorageModIdentifiers()).Returns(storageModIdentifier);
            
            mods.Add(storageMod.Object, modAction);

            return storageMod.Object;
        }
        
        [Fact]
        private void SingleUse_AllLoaded_NoConflict()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void SingleUse_AllLoaded_Conflict()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Use, true);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Null(selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void SingleUse_NotLoaded_NoUsed()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Use, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(false, mods, structure,
                null);
            
            Assert.Null(selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void SingleUse_NotLoaded_Used()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                storageMod.GetStorageModIdentifiers());
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void SingleUse_NotLoaded_UsedConflict()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, true);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                storageMod.GetStorageModIdentifiers());
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void Precedence()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, false);
            AddAction(mods, ModActionEnum.Update, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void Loading()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Use, false);
            AddAction(mods, ModActionEnum.Loading, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Null(selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void LoadingUsed()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, false);
            AddAction(mods, ModActionEnum.Loading, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                storageMod.GetStorageModIdentifiers());
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void UsedNotPresent_BeforeAllLoaded()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Use, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(false, mods, structure,
                new StorageModIdentifiers("doesn't", "exist"));
            
            Assert.Null(selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void UsedNotPresent_AllLoaded()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            var storageMod = AddAction(mods, ModActionEnum.Use, false);
            
            var structure = new MockModelStructure();
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                new StorageModIdentifiers("doesn't", "exist"));
            
            Assert.Equal(storageMod, selectedMod);
            Assert.Null(selectedDownload);
        }
        
        [Fact]
        private void Download_Writable()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Unusable, false);
            
            var structure = new MockModelStructure();
            var storage = new Mock<IModelStorage>(MockBehavior.Strict);
            storage.Setup(s => s.CanWrite).Returns(true);
            structure.Storages.Add(storage.Object);
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Null(selectedMod);
            Assert.Equal(storage.Object, selectedDownload);
        }
        
        [Fact]
        private void Download_NotWritable()
        {
            var mods = new Dictionary<IModelStorageMod, ModAction>();

            AddAction(mods, ModActionEnum.Unusable, false);
            
            var structure = new MockModelStructure();
            var storage = new Mock<IModelStorage>(MockBehavior.Strict);
            storage.Setup(s => s.CanWrite).Returns(false);
            structure.Storages.Add(storage.Object);
            
            var (selectedMod, selectedDownload) = CoreCalculation.AutoSelect(true, mods, structure,
                null);
            
            Assert.Null(selectedMod);
            Assert.Null(selectedDownload);
        }
    }
}