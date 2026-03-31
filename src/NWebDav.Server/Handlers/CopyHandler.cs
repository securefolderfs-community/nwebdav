using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Storage;
using OwlCore.Storage;
using SecureFolderFS.Storage.Extensions;

namespace NWebDav.Server.Handlers
{
    /// <summary>
    /// Implementation of the COPY method.
    /// </summary>
    /// <remarks>
    /// The specification of the WebDAV COPY method can be found in the
    /// <see href="http://www.webdav.org/specs/rfc2518.html#METHOD_COPY">
    /// WebDAV specification
    /// </see>.
    /// </remarks>
    public sealed class CopyHandler : IRequestHandler
    {
        /// <summary>
        /// Handle a COPY request.
        /// </summary>
        /// <inheritdoc/>
        public async Task HandleRequestAsync(HttpListenerContext context, IStore store, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            // Obtain request and response
            var request = context.Request;
            var response = context.Response;

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

            // Check if the Overwrite header is set
            var overwrite = request.GetOverwrite();

            // Split the destination Uri
            var destination = RequestHelpers.SplitUri(destinationUri);

            // Obtain the destination collection
            var destinationCollection = await store.GetCollectionAsync(destination.CollectionUri, cancellationToken).ConfigureAwait(false);
            if (destinationCollection == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.Conflict, "Destination cannot be found or is not a collection.");
                return;
            }

            // Obtain the source item
            var sourceItem = await store.GetItemAsync(request.Url, cancellationToken).ConfigureAwait(false);
            if (sourceItem == null)
            {
                // Source not found
                response.SetStatus(HttpStatusCode.NotFound, "Source cannot be found.");
                return;
            }

            // Determine depth
            var depth = request.GetDepth();

            // Keep track of all errors
            var errors = new UriResultCollection();

            // Copy collection
            await CopyAsync(sourceItem, destinationCollection, destination.Name, overwrite, depth, cancellationToken, destination.CollectionUri, errors).ConfigureAwait(false);

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

        private async Task CopyAsync(IDavStorable source, IDavFolder destinationCollection, string newName, bool overwrite, int depth, CancellationToken cancellationToken, Uri baseUri, UriResultCollection errors)
        {
            try
            {
                _ = source switch
                {
                    IDavFile file => (IStorableChild)await destinationCollection.CreateCopyOfAsync(file, overwrite, newName, cancellationToken).ConfigureAwait(false),
                    IDavFolder folder when depth > 0 => await destinationCollection.CreateCopyOfAsync(folder, overwrite, newName, cancellationToken).ConfigureAwait(false),
                    _ => throw new NotSupportedException($"The source item type '{source.GetType().Name}' is not supported.")
                };
            }
            catch (FileAlreadyExistsException)
            {
                errors.AddResult(baseUri, HttpStatusCode.Conflict);
            }
            catch (UnauthorizedAccessException)
            {
                errors.AddResult(baseUri, HttpStatusCode.Forbidden);
            }
        }
    }
}
