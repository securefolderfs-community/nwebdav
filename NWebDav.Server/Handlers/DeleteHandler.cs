using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using OwlCore.Storage;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Locking;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the DELETE method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV DELETE method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_DELETE">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class DeleteHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a DELETE request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(HttpListenerContext context, IStore store, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // Keep track of all errors
            var errors = new UriResultCollection();

            // We should always remove the item from a parent container
            var splitUri = RequestHelpers.SplitUri(request.Url);
            if (splitUri is null)
            {
                response.SetStatus(HttpStatusCode.InternalServerError);
                return;
            }

            // Obtain parent collection
            var parentCollection = await store.GetCollectionAsync(splitUri.CollectionUri, context).ConfigureAwait(false);
            if (parentCollection is null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // Obtain the item that actually is deleted
            var deleteItem = await parentCollection.GetItemAsync(splitUri.Name, context).ConfigureAwait(false);
            if (deleteItem is null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // Check if the item is locked
            if (deleteItem.LockingManager is null || await deleteItem.LockingManager.IsLockedAsync(deleteItem, cancellationToken))
            {
                // Obtain the lock token
                var ifToken = request.GetIfLockToken();
                if (ifToken is not null && deleteItem.LockingManager is not null && !await deleteItem.LockingManager.HasLockAsync(deleteItem, ifToken, cancellationToken))
                {
                    response.SetStatus(HttpStatusCode.Locked);
                    return;
                }

                // Remove the token
                if (deleteItem.LockingManager is not null && ifToken is not null)
                    await deleteItem.LockingManager.UnlockAsync(deleteItem, ifToken, cancellationToken);
            }

            // Delete item
            var status = await DeleteItemAsync(parentCollection, splitUri.Name, context, splitUri.CollectionUri).ConfigureAwait(false);
            if (status == HttpStatusCode.OK && errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument, logger, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Return the proper status
                response.SetStatus(status);
            }
        }

        private async Task<HttpStatusCode> DeleteItemAsync(IStoreCollection collection, string name, HttpListenerContext context, Uri baseUri)
        {
            // Obtain the actual item
            var deleteItem = await collection.GetItemAsync(name, context).ConfigureAwait(false);
            if (deleteItem is IStoreCollection deleteCollection)
            {
                // Determine the new base URI
                var subBaseUri = UriHelper.Combine(baseUri, name);

                // Delete all entries first
                foreach (var entry in await deleteCollection.GetItemsAsync(context).ConfigureAwait(false))
                    await DeleteItemAsync(deleteCollection, entry.Name, context, subBaseUri).ConfigureAwait(false);
            }

            // Attempt to delete the item
            return await collection.DeleteItemAsync(name, context).ConfigureAwait(false);
        }
    }
}
