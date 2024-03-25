using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Http;
using NWebDav.Server.Stores;
using OwlCore.Storage;
using SecureFolderFS.Shared.Extensions;
using SecureFolderFS.Storage.Extensions;
using System;
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
        public async Task HandleRequestAsync(IHttpContext context, IStore store, IFolder storageRoot, ILogger? logger = null, CancellationToken cancellationToken = default)
        {
            if (context.Request.Url is null)
            {
                context.Response.SetStatus(HttpStatusCode.NotFound);
                return;
            }

            // It's not a collection, so we'll try again by fetching the item in the parent collection
            var splitUri = RequestHelper.SplitUri(context.Request.Url);

            // Obtain collection
            IModifiableFolder folder;
            try
            {
                if (await storageRoot.GetItemRecursiveAsync(splitUri.CollectionUri.GetUriPath(), cancellationToken)
                        .ConfigureAwait(false) is not IModifiableFolder modifiableFolder)
                {
                    context.Response.SetStatus(HttpStatusCode.Forbidden);
                    return;
                }

                folder = modifiableFolder;
            }
            catch (Exception)
            {
                context.Response.SetStatus(HttpStatusCode.Conflict);
                return;
            }

            var createdFileResult = await folder.CreateFileWithResultAsync(splitUri.Name, true, cancellationToken).ConfigureAwait(false);
            if (createdFileResult.Successful)
            {
                var fileStreamResult = await createdFileResult.Value!.OpenStreamWithResultAsync(FileAccess.ReadWrite, cancellationToken).ConfigureAwait(false);
                if (!fileStreamResult.Successful)
                {
                    context.Response.SetStatus(fileStreamResult);
                    return;
                }

                var fileStream = fileStreamResult.Value!;
                await using (fileStream)
                {
                    if (context.Request.InputStream is null)
                    {
                        // TODO: Is that error appropriate?
                        context.Response.SetStatus(HttpStatusCode.NoContent);
                        return;
                    }

                    // Make sure we can write to the file
                    if (!fileStream.CanWrite)
                    {
                        context.Response.SetStatus(HttpStatusCode.Forbidden);
                        return;
                    }

                    try
                    {
                        // Copy contents
                        await context.Request.InputStream.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);

                        // Set status to OK
                        context.Response.SetStatus(HttpStatusCode.OK);
                    }
                    catch (IOException ioEx) when (ioEx.IsDiskFull())
                    {
                        context.Response.SetStatus(HttpStatusCode.InsufficientStorage);
                    }
                }
            }
            else
                context.Response.SetStatus(createdFileResult);
        }
    }
}
