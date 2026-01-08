using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Storage;
using NWebDav.Server.Stores;

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

            // Obtain the item - decode URL-encoded characters
            var decodedName = Uri.UnescapeDataString(splitUri.Name);
            var result = await collection.CreateItemAsync(decodedName, true, cancellationToken).ConfigureAwait(false);
            var status = result.Result;
            if (status is HttpStatusCode.Created or HttpStatusCode.NoContent)
            {
                if (result.Item is IStoreFile storeFile)
                {
                    // Check if there's content to upload
                    // macOS Finder uses chunked transfer encoding with X-Expected-Entity-Length header
                    // Content-Length will be -1 for chunked, but we can still read the stream
                    var isChunked = request.Headers["Transfer-Encoding"]?.ToLowerInvariant() == "chunked";
                    var hasExpectedLength = long.TryParse(request.Headers["X-Expected-Entity-Length"], out var expectedLength);
                    var hasContent = request.ContentLength64 > 0 || isChunked || hasExpectedLength;

                    if (hasContent && request.InputStream != Stream.Null)
                    {
                        // Get the appropriate stream based on Content-Encoding header
                        var inputStream = GetDecodedStream(request);

                        // For chunked transfers on macOS, wrap with a length-limited stream
                        // This works around an issue where HttpListener's ChunkedInputStream doesn't properly signal EOF
                        if (isChunked && hasExpectedLength && expectedLength > 0)
                            inputStream = new LengthLimitedStream(inputStream, expectedLength);

                        // Upload the information to the item
                        try
                        {
                            await using var outputStream = await storeFile.OpenStreamAsync(FileAccess.Write, cancellationToken).ConfigureAwait(false);
                            await inputStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
                            await outputStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                            status = HttpStatusCode.OK;
                        }
                        catch (IOException ioException) when (ioException.IsDiskFull())
                        {
                            status = HttpStatusCode.InsufficientStorage;
                        }
                        catch (UnauthorizedAccessException)
                        {
                            status = HttpStatusCode.Forbidden;
                        }
                    }
                    else
                    {
                        // No content - empty file or just creating the file
                        status = HttpStatusCode.OK;
                    }
                }
                else
                {
                    status = HttpStatusCode.Conflict;
                }
            }

            // Finished writing
            response.SetStatus(status);
        }

        /// <summary>
        /// Gets the input stream, decompressing if necessary based on Content-Encoding header.
        /// </summary>
        private static Stream GetDecodedStream(HttpListenerRequest request)
        {
            var contentEncoding = request.Headers["Content-Encoding"]?.ToLowerInvariant();
            return contentEncoding switch
            {
                "gzip" => new GZipStream(request.InputStream, CompressionMode.Decompress, leaveOpen: false),
                "deflate" => new DeflateStream(request.InputStream, CompressionMode.Decompress, leaveOpen: false),
                "br" => new BrotliStream(request.InputStream, CompressionMode.Decompress, leaveOpen: false),
                _ => request.InputStream
            };
        }
    }
}
