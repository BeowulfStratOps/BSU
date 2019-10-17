using System;
using System.Collections.Generic;
using System.Text;
using BSU.CoreInterface;

namespace BSU.Core
{
    class State
    {
        public Dictionary<IRemoteMod, ModActions> Actions = new Dictionary<IRemoteMod, ModActions>();
        public HashCache Hashes = new HashCache();
    }
}
