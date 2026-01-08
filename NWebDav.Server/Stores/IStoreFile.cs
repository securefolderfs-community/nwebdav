using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Stores
{
    public interface IStoreFile : IStoreItem, IDavFile
    {

        Task<HttpStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);
    }
}
