using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public interface IStore // TODO(wd): Replace with IStorageService
    {
        Task<IStoreItem?> GetItemAsync(Uri uri, CancellationToken cancellationToken);

        Task<IStoreCollection?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken);
    }
}
