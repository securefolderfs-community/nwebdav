using NWebDav.Server.Stores;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Extensions
{
    public static class DavStorageExtensions
    {
        public static async Task<HttpStatusCode> DavDeleteAsync(this IStoreCollection collection, IStoreItem storeItem, CancellationToken cancellationToken = default)
        {
            try
            {
                await collection.DeleteAsync(storeItem, cancellationToken);
                return HttpStatusCode.OK;
            }
            catch (UnauthorizedAccessException)
            {
                return HttpStatusCode.Forbidden;
            }
            catch (HttpListenerException ex)
            {
                return (HttpStatusCode)ex.ErrorCode;
            }
        }
    }
}
