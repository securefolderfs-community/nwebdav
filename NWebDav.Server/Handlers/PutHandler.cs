using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using OwlCore.Storage;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the PUT method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV PUT method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_PUT">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class PutHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a PUT request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(HttpListenerContext context, IStore store, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelpers.SplitUri(request.Url);

            // Obtain collection
            var collection = await store.GetCollectionAsync(splitUri.CollectionUri, cancellationToken).ConfigureAwait(false);
            if (collection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.Conflict);
                return;
            }

            // Obtain the item
            var result = await collection.CreateItemAsync(splitUri.Name, true, cancellationToken).ConfigureAwait(false);
            var status = result.Result;
            if (status == HttpStatusCode.Created || status == HttpStatusCode.NoContent)
            {
                if (result.Item is IStoreFile storeFile)
                {
                    if (context.Request.Headers["Transfer-Encoding"]?.ToLowerInvariant() != "chunked")
                    {
                        if (request.InputStream != Stream.Null)
                        {
                            // Upload the information to the item
                            status = await storeFile.UploadFromStreamAsync(request.InputStream ?? Stream.Null, cancellationToken).ConfigureAwait(false);
                        }
                        else
                        {
                            status = HttpStatusCode.OK;
                        }
                    }
                    else
                        status = HttpStatusCode.LengthRequired;
                }
                else
                {
                    status = HttpStatusCode.Conflict;
                }
            }

            // Finished writing
            response.SetStatus(status);
        }
    }
}
