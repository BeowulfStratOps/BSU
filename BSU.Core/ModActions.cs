using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core
{
    internal class ModActions
    {
        public readonly List<ILocalMod> Update = new List<ILocalMod>();
        public readonly List<ILocalMod> Use = new List<ILocalMod>();

        public ModActions()
        {
            Update = new List<ILocalMod>();
            Use = new List<ILocalMod>();
        }
    }
}
