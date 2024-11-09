using NWebDav.Server.Enums;
using NWebDav.Server.Extensions;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using OwlCore.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_directoryInfo.FullPath}\\")]
    public class DiskStoreCollection : IStoreCollection
    {
        private static readonly XElement s_xDavCollection = new XElement(WebDavNamespaces.DavNs + "collection");
        private readonly DirectoryInfo _directoryInfo;

        /// <inheritdoc/>
        public virtual string Id { get; }

        /// <inheritdoc/>
        public virtual string Name { get; }

        public DiskStoreCollection(ILockingManager lockingManager, DirectoryInfo directoryInfo, bool isWritable)
        {
            _directoryInfo = directoryInfo;
            Id = _directoryInfo.FullName;
            Name = _directoryInfo.Name;
            LockingManager = lockingManager;
            IsWritable = isWritable;
        }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<IStoreItem> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            switch (type)
            {
                case StorableType.File:
                {
                    foreach (var filePath in Directory.EnumerateFiles(_directoryInfo.FullName))
                        yield return NewFile(filePath);

                    break;
                }

                case StorableType.Folder:
                {
                    foreach (var folderPath in Directory.EnumerateDirectories(_directoryInfo.FullName))
                        yield return NewCollection(folderPath);

                    break;
                }

                case StorableType.All:
                {
                    foreach (var item in _directoryInfo.EnumerateFileSystemInfos())
                    {
                        yield return item switch
                        {
                            FileInfo => NewFile(item.FullName),
                            DirectoryInfo => NewCollection(item.FullName)
                        };
                    }

                    break;
                }
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IStoreItem> GetFirstByNameAsync(string name, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            // Determine the path
            var id = Path.Combine(_directoryInfo.FullName, name);

            // Check if the item is a file
            if (File.Exists(id))
                return NewFile(id);

            // Check if the item is a directory
            if (Directory.Exists(id))
                return NewCollection(id);

            // Item not found
            throw new FileNotFoundException($"An item was not found. Name: '{name}'.");
        }

        protected virtual IStoreFile NewFile(string id)
        {
            return new DiskStoreFile(LockingManager, new(id), IsWritable);
        }

        protected virtual IStoreCollection NewCollection(string id)
        {
            return new DiskStoreCollection(LockingManager, new(id), IsWritable);
        }




        public static PropertyManager<DiskStoreCollection> DefaultPropertyManager { get; } = new PropertyManager<DiskStoreCollection>(new DavProperty<DiskStoreCollection>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.CreationTimeUtc,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.CreationTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new DavDisplayName<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.Name
            },
            new DavGetLastModified<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.LastWriteTimeUtc,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.LastWriteTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new DavGetResourceType<DiskStoreCollection>
            {
                Getter = (context, collection) => new []{s_xDavCollection}
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreCollection>(),
            new DavSupportedLockDefault<DiskStoreCollection>(),

            // Hopmann/Lippert collection properties
            new DavExtCollectionChildCount<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.EnumerateFiles().Count() + collection._directoryInfo.EnumerateDirectories().Count()
            },
            new DavExtCollectionIsFolder<DiskStoreCollection>
            {
                Getter = (context, collection) => true
            },
            new DavExtCollectionIsHidden<DiskStoreCollection>
            {
                Getter = (context, collection) => (collection._directoryInfo.Attributes & FileAttributes.Hidden) != 0
            },
            new DavExtCollectionIsStructuredDocument<DiskStoreCollection>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionHasSubs<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.EnumerateDirectories().Any()
            },
            new DavExtCollectionNoSubs<DiskStoreCollection>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionObjectCount<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.EnumerateFiles().Count()
            },
            new DavExtCollectionReserved<DiskStoreCollection>
            {
                Getter = (context, collection) => !collection.IsWritable
            },
            new DavExtCollectionVisibleCount<DiskStoreCollection>
            {
                Getter = (context, collection) =>
                    collection._directoryInfo.EnumerateDirectories().Count(di => (di.Attributes & FileAttributes.Hidden) == 0) +
                    collection._directoryInfo.EnumerateFiles().Count(fi => (fi.Attributes & FileAttributes.Hidden) == 0)
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.CreationTimeUtc,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.CreationTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastAccessTime<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.LastAccessTimeUtc,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.LastAccessTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastModifiedTime<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.LastWriteTimeUtc,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.LastWriteTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32FileAttributes<DiskStoreCollection>
            {
                Getter = (context, collection) => collection._directoryInfo.Attributes,
                Setter = (context, collection, value) =>
                {
                    collection._directoryInfo.Attributes = value;
                    return HttpStatusCode.OK;
                }
            }
        });

        public bool IsWritable { get; }
        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, CancellationToken cancellationToken)
        {
            // Return error
            if (!IsWritable)
                return Task.FromResult(new StoreItemResult(HttpStatusCode.PreconditionFailed));

            // Determine the destination path
            var destinationPath = Path.Combine(Id, name);

            // Determine result
            HttpStatusCode result;

            // Check if the file can be overwritten
            if (File.Exists(name))
            {
                if (!overwrite)
                    return Task.FromResult(new StoreItemResult(HttpStatusCode.PreconditionFailed));

                result = HttpStatusCode.NoContent;
            }
            else
            {
                result = HttpStatusCode.Created;
            }

            try
            {
                // Create a new file
                File.Create(destinationPath).Dispose();
            }
            catch (Exception exc)
            {
                // Log exception
                // TODO(wd): Add logging
                //s_log.Log(LogLevel.Error, () => $"Unable to create '{destinationPath}' file.", exc);
                return Task.FromResult(new StoreItemResult(HttpStatusCode.InternalServerError));
            }

            // Return result
            return Task.FromResult(new StoreItemResult(result, new DiskStoreFile(LockingManager, new FileInfo(destinationPath), IsWritable)));
        }

        public Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken)
        {
            // Return error
            if (!IsWritable)
                return Task.FromResult(new StoreCollectionResult(HttpStatusCode.PreconditionFailed));

            // Determine the destination path
            var destinationPath = Path.Combine(Id, name);

            // Check if the directory can be overwritten
            HttpStatusCode result;
            if (Directory.Exists(destinationPath))
            {
                // Check if overwrite is allowed
                if (!overwrite)
                    return Task.FromResult(new StoreCollectionResult(HttpStatusCode.PreconditionFailed));

                // Overwrite existing
                result = HttpStatusCode.NoContent;
            }
            else
            {
                // Created new directory
                result = HttpStatusCode.Created;
            }

            try
            {
                // Attempt to create the directory
                Directory.CreateDirectory(destinationPath);
            }
            catch (Exception exc)
            {
                // Log exception
                // TODO(wd): Add logging
                //s_log.Log(LogLevel.Error, () => $"Unable to create '{destinationPath}' directory.", exc);
                return null;
            }

            // Return the collection
            return Task.FromResult(new StoreCollectionResult(result, new DiskStoreCollection(LockingManager, new DirectoryInfo(destinationPath), IsWritable)));
        }

        public async Task<StoreItemResult> CopyAsync(IStoreCollection destinationCollection, string name, bool overwrite, CancellationToken cancellationToken)
        {
            // Just create the folder itself
            var result = await destinationCollection.CreateCollectionAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
            return new StoreItemResult(result.Result, result.Collection);
        }

        public bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite)
        {
            // We can only move disk-store collections
            return destination is DiskStoreCollection;
        }

        public async Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destinationCollection, string destinationName, bool overwrite, CancellationToken cancellationToken)
        {
            // Return error
            if (!IsWritable)
                return new StoreItemResult(HttpStatusCode.PreconditionFailed);

            // Determine the object that is being moved
            var item = await this.TryGetFirstByNameAsync(sourceName, cancellationToken).ConfigureAwait(false);
            if (item is null)
                return new StoreItemResult(HttpStatusCode.NotFound);

            try
            {
                // If the destination collection is a directory too, then we can simply move the file
                if (destinationCollection is DiskStoreCollection destinationDiskStoreCollection)
                {
                    // Return error
                    if (!destinationDiskStoreCollection.IsWritable)
                        return new StoreItemResult(HttpStatusCode.PreconditionFailed);

                    // Determine source and destination paths
                    var sourcePath = Path.Combine(_directoryInfo.FullName, sourceName);
                    var destinationPath = Path.Combine(destinationDiskStoreCollection._directoryInfo.FullName, destinationName);

                    // Check if the file already exists
                    HttpStatusCode result;
                    if (File.Exists(destinationPath))
                    {
                        // Remove the file if it already exists (if allowed)
                        if (!overwrite)
                            return new StoreItemResult(HttpStatusCode.Forbidden);

                        // The file will be overwritten
                        File.Delete(destinationPath);
                        result = HttpStatusCode.NoContent;
                    }
                    else if (Directory.Exists(destinationPath))
                    {
                        // Remove the directory if it already exists (if allowed)
                        if (!overwrite)
                            return new StoreItemResult(HttpStatusCode.Forbidden);

                        // The file will be overwritten
                        Directory.Delete(destinationPath, true);
                        result = HttpStatusCode.NoContent;
                    }
                    else
                    {
                        // The file will be "created"
                        result = HttpStatusCode.Created;
                    }

                    switch (item)
                    {
                        case DiskStoreFile _:
                            // Move the file
                            File.Move(sourcePath, destinationPath);
                            return new StoreItemResult(result, new DiskStoreFile(LockingManager, new FileInfo(destinationPath), IsWritable));

                        case DiskStoreCollection _:
                            // Move the directory
                            Directory.Move(sourcePath, destinationPath);
                            return new StoreItemResult(result, new DiskStoreCollection(LockingManager, new DirectoryInfo(destinationPath), IsWritable));

                        default:
                            // Invalid item
                            Debug.Fail($"Invalid item {item.GetType()} inside the {nameof(DiskStoreCollection)}.");
                            return new StoreItemResult(HttpStatusCode.InternalServerError);
                    }
                }
                else
                {
                    // Attempt to copy the item to the destination collection
                    var result = await item.CopyAsync(destinationCollection, destinationName, overwrite, cancellationToken).ConfigureAwait(false);
                    if (result.Result == HttpStatusCode.Created || result.Result == HttpStatusCode.NoContent)
                        await DeleteItemAsync(sourceName, cancellationToken).ConfigureAwait(false);

                    // Return the result
                    return result;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return new StoreItemResult(HttpStatusCode.Forbidden);
            }
        }

        public Task<HttpStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken)
        {
            // Return error
            if (!IsWritable)
                return Task.FromResult(HttpStatusCode.PreconditionFailed);

            // Determine the full path
            var fullPath = Path.Combine(_directoryInfo.FullName, name);
            try
            {
                // Check if the file exists
                if (File.Exists(fullPath))
                {
                    // Delete the file
                    File.Delete(fullPath);
                    return Task.FromResult(HttpStatusCode.OK);
                }

                // Check if the directory exists
                if (Directory.Exists(fullPath))
                {
                    // Delete the directory
                    Directory.Delete(fullPath);
                    return Task.FromResult(HttpStatusCode.OK);
                }

                // Item not found
                return Task.FromResult(HttpStatusCode.NotFound);
            }
            catch (UnauthorizedAccessException)
            {
                return Task.FromResult(HttpStatusCode.Forbidden);
            }
            catch (Exception exc)
            {
                // Log exception
                // TODO(wd): Add logging
                //s_log.Log(LogLevel.Error, () => $"Unable to delete '{fullPath}' directory.", exc);
                return Task.FromResult(HttpStatusCode.InternalServerError);
            }
        }

        public EnumerationDepthMode InfiniteDepthMode => EnumerationDepthMode.Rejected;
    }
}