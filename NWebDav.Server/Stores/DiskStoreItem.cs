using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NWebDav.Server.Helpers;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

[DebuggerDisplay("{FileInfo.FullPath}")]
public sealed class DiskStoreItem : IStoreItem
{
    private readonly DiskStoreBase _store;
    private readonly ILogger<DiskStoreItem> _logger;

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public string UniqueKey { get; }

    /// <inheritdoc/>
    public IPropertyManager PropertyManager { get; }

    public DiskStoreItem(DiskStoreBase store, DiskStoreItemPropertyManager propertyManager, FileInfo fileInfo, ILogger<DiskStoreItem> logger)
    {
        _store = store;
        _logger = logger;
        Name = fileInfo.Name;
        UniqueKey = fileInfo.FullName;
        PropertyManager = propertyManager;
        FileInfo = fileInfo;
    }

    public FileInfo FileInfo { get; }
    public bool IsWritable => _store.IsWritable;

    /// <inheritdoc/>
    public async Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return FileInfo.OpenRead();
    }

    /// <inheritdoc/>
    public async Task<DavStatusCode> UploadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
    {
        // Check if the item is writable
        if (!IsWritable)
            return DavStatusCode.Conflict;

        try
        {
            // Copy the information to the destination stream
            var outputStream = FileInfo.OpenWrite();
            await using (outputStream.ConfigureAwait(false))
            {
                // Copy the stream
                await inputStream.CopyToAsync(outputStream, cancellationToken).ConfigureAwait(false);
            }

            return DavStatusCode.Ok;
        }
        catch (IOException ioException) when (ioException.IsDiskFull())
        {
            return DavStatusCode.InsufficientStorage;
        }
    }

    /// <inheritdoc/>
    public async Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken)
    {
        try
        {
            // If the destination is also a disk-store, then we can use the FileCopy API
            // (it's probably a bit more efficient than copying in C#)
            if (destination is DiskStoreCollection diskCollection)
            {
                // Check if the collection is writable
                if (!diskCollection.IsWritable)
                    return new StoreItemResult(DavStatusCode.PreconditionFailed);

                var destinationPath = Path.Combine(diskCollection.FullPath, name);

                // Check if the file already exists
                var fileExists = File.Exists(destinationPath);
                if (fileExists && !overwrite)
                    return new StoreItemResult(DavStatusCode.PreconditionFailed);

                // Copy the file
                File.Copy(FileInfo.FullName, destinationPath, true);

                // Return the appropriate status
                return new StoreItemResult(fileExists ? DavStatusCode.NoContent : DavStatusCode.Created);
            }
            else
            {
                // Create the item in the destination collection
                var sourceStream = await GetReadableStreamAsync(cancellationToken).ConfigureAwait(false);
                await using (sourceStream.ConfigureAwait(false))
                {
                    return await destination.CreateItemAsync(name, sourceStream, overwrite, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exc)
        {
            _logger.LogError(exc, "Unexpected exception while copying data.");
            return new StoreItemResult(DavStatusCode.InternalServerError);
        }
    }
}