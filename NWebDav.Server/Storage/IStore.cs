using System;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Storage
{
    public interface IStore // TODO(wd): Replace with IStorageService
    {
        Task<IDavStorable?> GetItemAsync(Uri uri, CancellationToken cancellationToken);

        Task<IDavFolder?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken);
    }
}
