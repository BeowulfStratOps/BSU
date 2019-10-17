using System;
using System.Collections.Generic;
using System.Text;
using BSU.Core.Hashes;
using BSU.CoreInterface;

namespace BSU.Core
{
    class HashCache
    {
        private readonly Dictionary<IRemoteMod, MatchHash> _remoteMatchHashes = new Dictionary<IRemoteMod, MatchHash>();
        private readonly Dictionary<IRemoteMod, VersionHash> _remoteVersionHashes = new Dictionary<IRemoteMod, VersionHash>();
        private readonly Dictionary<ILocalMod, MatchHash> _localMatchHashes = new Dictionary<ILocalMod, MatchHash>();
        private readonly Dictionary<ILocalMod, VersionHash> _localVersionHashes = new Dictionary<ILocalMod, VersionHash>();

        public MatchHash GetMatchHash(IRemoteMod mod)
        {
            if (_remoteMatchHashes.TryGetValue(mod, out var hash)) return hash;
            var newHash = MatchHash.FromRemoteMod(mod);
            _remoteMatchHashes.Add(mod, newHash);
            return newHash;
        }

        public MatchHash GetMatchHash(ILocalMod mod)
        {
            if (_localMatchHashes.TryGetValue(mod, out var hash)) return hash;
            var newHash = MatchHash.FromLocalMod(mod);
            _localMatchHashes.Add(mod, newHash);
            return newHash;
        }

        public VersionHash GetVersionHash(IRemoteMod mod)
        {
            if (_remoteVersionHashes.TryGetValue(mod, out var hash)) return hash;
            var newHash = new VersionHash(mod);
            _remoteVersionHashes.Add(mod, newHash);
            return newHash;
        }

        public VersionHash GetVersionHash(ILocalMod mod)
        {
            if (_localVersionHashes.TryGetValue(mod, out var hash)) return hash;
            var newHash = new VersionHash(mod);
            _localVersionHashes.Add(mod, newHash);
            return newHash;
        }
    }
}
