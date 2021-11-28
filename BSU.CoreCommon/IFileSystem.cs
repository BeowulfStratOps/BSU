using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BSU.CoreCommon
{
    public interface IFileSystem
    {
        Task<Stream> OpenWrite(string path, CancellationToken cancellationToken);
        Task<Stream> OpenRead(string path, CancellationToken cancellationToken);
        Task Move(string from, string to, CancellationToken cancellationToken);
        Task<bool> HasFile(string path, CancellationToken cancellationToken);
        Task Delete(string path, CancellationToken cancellationToken);
    }
}
