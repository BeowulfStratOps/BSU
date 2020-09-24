﻿using System.ComponentModel;
using System.Runtime.CompilerServices;
using BSU.Core.Annotations;
using BSU.Core.Model;
using BSU.Core.ViewModel.Util;

namespace BSU.Core.ViewModel
{
    public class StorageMod : ViewModelClass
    {
        internal IModelStorageMod ModelStorageMod { get; }
        
        public string Identifier { get; set; }
        
        internal StorageMod(IModelStorageMod mod)
        {
            var state = mod.GetState();
            mod.StateChanged += StateChanged;
            ModelStorageMod = mod;
            Identifier = mod.ToString();
        }

        private void StateChanged()
        {
            var state = ModelStorageMod.GetState();
        }
    }
}