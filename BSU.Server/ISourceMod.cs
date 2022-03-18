using System;
using System.Collections.Generic;
using System.IO;

namespace BSU.Server;

public interface ISourceMod
{
    List<NormalizedPath> GetFileList();
    Stream OpenRead(NormalizedPath path);
    DateTime GetLastChangeDateTime(NormalizedPath path);
}
