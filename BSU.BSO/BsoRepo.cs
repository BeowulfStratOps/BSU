﻿using System;
using System.Collections.Generic;
using BSU.CoreInterface;

namespace BSU.BSO
{
    public class BsoRepo : IRepository
    {
        public BsoRepo(string url, string name)
        {

        }

        public List<IRemoteMod> GetMods()
        {
            throw new NotImplementedException();
        }

        public string GetName()
        {
            throw new NotImplementedException();
        }

        public string GetLocation()
        {
            throw new NotImplementedException();
        }
    }
}
