using System;
using System.Collections.Generic;
using System.IO;

namespace BSU.Core
{
    public class Core
    {
        private readonly Settings _settings;

        public Core(FileInfo settingsPath)
        {
            _settings = new Settings(settingsPath);
        }

        public void AddRepo(Uri uri)
        {
            throw new NotImplementedException();
        }

        public void AddStorage(DirectoryInfo directory)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Does all the hard work. Don't spam it.
        /// </summary>
        /// <returns></returns>
        public GlobalState GetState()
        {
            throw new NotImplementedException();
        }
    }
}