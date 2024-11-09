using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Stores;
using OwlCore.Storage;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using NWebDav.Server.Extensions;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the MOVE method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV MOVE method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_MOVE">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class MoveHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a MOVE request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(HttpListenerContext context, IStore store, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

            // We should always move the item from a parent container
            var splitSourceUri = RequestHelpers.SplitUri(request.Url);

            // Obtain source collection
            var sourceCollection = await store.GetCollectionAsync(splitSourceUri.CollectionUri, cancellationToken).ConfigureAwait(false);
            if (sourceCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // Obtain the item to move
            var moveItem = await sourceCollection.TryGetFirstByNameAsync(splitSourceUri.Name, cancellationToken).ConfigureAwait(false);
            if (moveItem is null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // Obtain the destination
            var destinationUri = request.GetDestinationUri();
            if (destinationUri == null)
            {
                // Bad request
                response.SetStatus(HttpStatusCode.BadRequest, "Destination header is missing.");
                return;
            }

            // Make sure the source and destination are different
            if (request.Url.AbsoluteUri.Equals(destinationUri.AbsoluteUri, StringComparison.CurrentCultureIgnoreCase))
            {
                // Forbidden
                response.SetStatus(HttpStatusCode.Forbidden, "Source and destination cannot be the same.");
                return;
            }

            // We should always move the item to a parent
            var splitDestinationUri = RequestHelpers.SplitUri(destinationUri);

            // Obtain destination collection
            var destinationCollection = await store.GetCollectionAsync(splitDestinationUri.CollectionUri, cancellationToken).ConfigureAwait(false);
            if (destinationCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // Check if the Overwrite header is set
            var overwrite = request.GetOverwrite();
            if (!overwrite)
            {
                // If overwrite is false and destination exist ==> Precondition Failed
                var destItem = await destinationCollection.TryGetFirstByNameAsync(splitDestinationUri.Name, cancellationToken).ConfigureAwait(false);
                if (destItem is not null)
                {
                    // Cannot overwrite destination item
                    response.SetStatus(HttpStatusCode.PreconditionFailed, "Cannot overwrite destination item.");
                    return;
                }
            }

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Move collection
            await MoveAsync(sourceCollection, moveItem, destinationCollection, splitDestinationUri.Name, overwrite, splitDestinationUri.CollectionUri, errors, cancellationToken).ConfigureAwait(false);

            // Check if there are any errors
            if (errors.HasItems)
            {
                // Obtain the status document
                var xDocument = new XDocument(errors.GetXmlMultiStatus());

                // Stream the document
                await response.SendResponseAsync(HttpStatusCode.MultiStatus, xDocument, logger, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Set the response
                response.SetStatus(HttpStatusCode.Created);
            }
        }

        private async Task MoveAsync(IStoreCollection sourceCollection, IStoreItem moveItem, IStoreCollection destinationCollection, string destinationName, bool overwrite, Uri baseUri, UriResultCollection errors, CancellationToken cancellationToken)
        {
            // Determine the new base URI
            var subBaseUri = UriHelper.Combine(baseUri, destinationName);

            // Obtain the actual item
            if (moveItem is IStoreCollection moveCollection && !moveCollection.SupportsFastMove(destinationCollection, destinationName, overwrite))
            {
                // Create a new collection
                var newCollectionResult = await destinationCollection.CreateCollectionAsync(destinationName, overwrite, cancellationToken).ConfigureAwait(false);
                if (newCollectionResult.Result != HttpStatusCode.Created && newCollectionResult.Result != HttpStatusCode.NoContent)
                {
                    errors.AddResult(subBaseUri, newCollectionResult.Result);
                    return;
                }

                // Move all sub items
                await foreach (var entry in moveCollection.GetItemsAsync(StorableType.All, cancellationToken).ConfigureAwait(false))
                    await MoveAsync(moveCollection, entry, newCollectionResult.Collection, entry.Name, overwrite, subBaseUri, errors, cancellationToken).ConfigureAwait(false);

                // Delete the source collection
                var deleteResult = await sourceCollection.DeleteItemAsync(moveItem.Name, cancellationToken).ConfigureAwait(false);
                if (deleteResult != HttpStatusCode.OK)
                    errors.AddResult(subBaseUri, newCollectionResult.Result);
            }
            else
            {
                // Items should be moved directly
                var result = await sourceCollection.MoveItemAsync(moveItem.Name, destinationCollection, destinationName, overwrite, cancellationToken).ConfigureAwait(false);
                if (result.Result != HttpStatusCode.Created && result.Result != HttpStatusCode.NoContent)
                    errors.AddResult(subBaseUri, result.Result);
            }
        }
    }
}
