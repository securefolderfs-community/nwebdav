using NWebDav.Server.Stores;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Extensions
{
    public static class StoreExtensions
    {
        public static async Task<IStoreItem?> TryGetFirstByNameAsync(this IStoreCollection collection, string name, CancellationToken cancellationToken = default)
        {
            IStoreItem item;
            try
            {
                return await collection.GetFirstByNameAsync(name, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                return null;
            }
        }
    }
}
