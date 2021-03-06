﻿using System.Collections.Generic;
using System.IO;
using BSU.Hashes;

namespace BSU.CoreCommon
{
    public interface IStorageMod
    {
        /// <summary>
        /// Attempts to retrieve a display name for this mod folder.
        /// </summary>
        /// <returns></returns>
        string GetDisplayName();
        List<string> GetFileList();

        /// <summary>
        /// Returns a read-only file stream. Must be disposed.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        Stream OpenFile(string path, FileAccess access);

        /// <summary>
        /// Get hash of a local file. Null if it doesn't exist.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        /// <returns></returns>
        FileHash GetFileHash(string path);

        /// <summary>
        /// Deletes a file. Exception if it doesn't exists.
        /// </summary>
        /// <param name="path">Relative path. Using forward slashes, starting with a forward slash, and in lower case.</param>
        void DeleteFile(string path);
        public void Load();
        public Uid GetUid();
    }
}
