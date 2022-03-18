using System.Collections.Generic;
using System.IO;

namespace BSU.Server;

public interface IDestinationMod
{
    List<NormalizedPath> GetFileList();
    Stream OpenRead(NormalizedPath path);
    void Write(NormalizedPath path, Stream data);
    void Remove(NormalizedPath path);
}
