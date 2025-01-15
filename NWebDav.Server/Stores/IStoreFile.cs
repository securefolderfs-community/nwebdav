using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public interface IStoreFile : IStoreItem
    {
        // Read/Write access to the data
        Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken);

        Task<HttpStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);
    }
}
