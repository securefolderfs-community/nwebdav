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
using NWebDav.Server.Enums;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;
using SecureFolderFS.Shared.Extensions;
using SecureFolderFS.Storage.Extensions;
using SecureFolderFS.Storage.Recyclable;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// A WebDAV folder backed by an <see cref="OwlCore.Storage.IFolder"/> instance.
    /// </summary>
    [DebuggerDisplay("{Inner.Name}\\")]
    public class BackedDavFolder : IDavFolder
    {
        private static readonly XElement s_xDavCollection = new XElement(WebDavNamespaces.DavNs + "collection");

        /// <summary>
        /// Gets the inner backing storage folder.
        /// </summary>
        public IFolder Inner { get; }

        /// <inheritdoc/>
        public virtual string Id => Inner.Id;

        /// <inheritdoc/>
        public virtual string Name => Inner.Name;

        /// <inheritdoc/>
        public EnumerationDepthMode DepthMode => EnumerationDepthMode.Rejected;

        /// <summary>
        /// Gets a value indicating whether this folder is writable.
        /// </summary>
        public bool IsWritable { get; }

        /// <inheritdoc/>
        public IPropertyManager PropertyManager => DefaultPropertyManager;

        /// <inheritdoc/>
        public ILockingManager? LockingManager { get; }

        /// <summary>
        /// Gets the default property manager for <see cref="BackedDavFolder"/>.
        /// </summary>
        public static PropertyManager<BackedDavFolder> DefaultPropertyManager { get; } = new(new DavProperty<BackedDavFolder>[]
        {
            // RFC-2518 properties
            new DavDisplayName<BackedDavFolder>
            {
                Getter = (context, collection) => collection.Name
            },
            new DavGetResourceType<BackedDavFolder>
            {
                Getter = (context, collection) => new[] { s_xDavCollection }
            },
            new DavCreationDate<BackedDavFolder>
            {
                Getter = (context, collection) => Directory.GetCreationTimeUtc(collection.GetDeepestWrapper().Inner.Id),
                Setter = (context, collection, value) =>
                {
                    Directory.SetCreationTimeUtc(collection.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new DavGetLastModified<BackedDavFolder>
            {
                Getter = (context, collection) => Directory.GetLastWriteTimeUtc(collection.GetDeepestWrapper().Inner.Id),
                Setter = (context, collection, value) =>
                {
                    Directory.SetLastWriteTimeUtc(collection.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<BackedDavFolder>(),
            new DavSupportedLockDefault<BackedDavFolder>(),

            // Hopmann/Lippert collection properties
            new DavExtCollectionChildCount<BackedDavFolder>()
            {
                Getter = (context, collection) => collection.GetItemsAsync().ToArrayAsyncImpl().GetAwaiter().GetResult().Length
            },
            new DavExtCollectionIsFolder<BackedDavFolder>
            {
                Getter = (context, collection) => true
            },
            new DavExtCollectionIsHidden<BackedDavFolder>
            {
                Getter = (context, collection) => (new DirectoryInfo(collection.GetDeepestWrapper().Inner.Id).Attributes & FileAttributes.Hidden) != 0
            },
            new DavExtCollectionIsStructuredDocument<BackedDavFolder>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionNoSubs<BackedDavFolder>
            {
                Getter = (context, collection) => false
            },
            new DavExtCollectionReserved<BackedDavFolder>
            {
                Getter = (context, collection) => !collection.IsWritable
            },
            new DavExtCollectionHasSubs<BackedDavFolder>
            {
                Getter = (context, collection) => collection.GetItemsAsync(StorableType.Folder).AnyAsyncImpl().GetAwaiter().GetResult()
            },
            new DavExtCollectionObjectCount<BackedDavFolder>
            {
                Getter = (context, collection) => collection.GetItemsAsync(StorableType.File).ToArrayAsyncImpl().GetAwaiter().GetResult().Length
            },
            new DavExtCollectionVisibleCount<BackedDavFolder>
            {
                Getter = (context, collection) =>
                {
                    var items = collection.GetItemsAsync().ToArrayAsyncImpl().GetAwaiter().GetResult();
                    return items.OfType<IDavFolder>().Count(folder => (new DirectoryInfo(folder.GetDeepestWrapper().Inner.Id).Attributes & FileAttributes.Hidden) == 0) +
                           items.OfType<IDavFile>().Count(file => (new FileInfo(file.GetDeepestWrapper().Inner.Id).Attributes & FileAttributes.Hidden) == 0);
                }
            },

            // Win32 extensions
            new Win32CreationTime<BackedDavFolder>
            {
                Getter = (context, collection) => Directory.GetCreationTimeUtc(collection.GetDeepestWrapper().Inner.Id),
                Setter = (context, collection, value) =>
                {
                    Directory.SetCreationTimeUtc(collection.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastAccessTime<BackedDavFolder>
            {
                Getter = (context, collection) => Directory.GetLastAccessTimeUtc(collection.GetDeepestWrapper().Inner.Id),
                Setter = (context, collection, value) =>
                {
                    Directory.SetLastAccessTimeUtc(collection.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastModifiedTime<BackedDavFolder>
            {
                Getter = (context, collection) => Directory.GetLastWriteTimeUtc(collection.GetDeepestWrapper().Inner.Id),
                Setter = (context, collection, value) =>
                {
                    Directory.SetLastWriteTimeUtc(collection.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32FileAttributes<BackedDavFolder>
            {
                Getter = (context, collection) => new DirectoryInfo(collection.GetDeepestWrapper().Inner.Id).Attributes,
                Setter = (context, collection, value) =>
                {
                    new DirectoryInfo(collection.GetDeepestWrapper().Inner.Id).Attributes = value;
                    return HttpStatusCode.OK;
                }
            }
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="BackedDavFolder"/> class.
        /// </summary>
        /// <param name="folder">The backing folder.</param>
        /// <param name="isWritable">Whether the folder is writable.</param>
        /// <param name="lockingManager">The locking manager.</param>
        public BackedDavFolder(IFolder folder, bool isWritable, ILockingManager? lockingManager = null)
        {
            Inner = folder ?? throw new ArgumentNullException(nameof(folder));
            IsWritable = isWritable;
            LockingManager = lockingManager;
        }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in Inner.GetItemsAsync(type, cancellationToken).ConfigureAwait(false))
            {
                yield return item switch
                {
                    IFile file => NewFile(file),
                    IFolder folder => NewFolder(folder),
                    _ => throw new InvalidOperationException("Unknown storage item type.")
                };
            }
        }

        /// <inheritdoc/>
        public virtual Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            // Folder watcher in WebDav is not supported
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public virtual async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        {
            if (Inner is not IStorableChild storableChild)
                return null;

            var parent = await storableChild.GetParentAsync(cancellationToken).ConfigureAwait(false);
            if (parent is null)
                return null;

            return new BackedDavFolder(parent, IsWritable, LockingManager);
        }

        /// <inheritdoc/>
        public virtual async Task<IStorableChild> GetFirstByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (Inner is not IGetFirstByName getFirstByName)
                throw new NotSupportedException("Folder does not support GetFirstByName.");

            var item = await getFirstByName.GetFirstByNameAsync(name, cancellationToken).ConfigureAwait(false);

            return item switch
            {
                IFile file => NewFile(file),
                IFolder folder => NewFolder(folder),
                _ => throw new InvalidOperationException("Unknown storage item type.")
            };
        }

        /// <inheritdoc/>
        public Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, CancellationToken cancellationToken,
            CreateCopyOfDelegate fallback)
        {
            return CreateCopyOfAsync(fileToCopy, overwrite, fileToCopy.Name, cancellationToken, (mf, f, ov, _, ct) => fallback(mf, f, ov, ct));
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateCopyOfAsync(IFile fileToCopy, bool overwrite, string newName, CancellationToken cancellationToken, CreateRenamedCopyOfDelegate fallback)
        {
            // Return error
            if (!IsWritable
                || Inner is not IModifiableFolder innerModifiableFolder
                || fileToCopy is not IWrapper<IFile> { Inner: IChildFile innerFile })
                throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

            try
            {
                // Attempt to move the item to the destination collection
                var copiedFile = await innerModifiableFolder.CreateCopyOfAsync(innerFile, overwrite, newName, cancellationToken);
                return NewFile(copiedFile);
            }
            catch (IOException ioException) when (ioException.IsDiskFull())
            {
                throw new HttpListenerException((int)HttpStatusCode.InsufficientStorage);
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpListenerException((int)HttpStatusCode.Forbidden);
            }
        }

        /// <inheritdoc/>
        public Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, CancellationToken cancellationToken,
            MoveFromDelegate fallback)
        {
            return MoveFromAsync(fileToMove, source, overwrite, fileToMove.Name, cancellationToken, (mf, f, src, ov, _, ct) => fallback(mf, f, src, ov, ct));
        }

        /// <inheritdoc/>
        public async Task<IChildFile> MoveFromAsync(IChildFile fileToMove, IModifiableFolder source, bool overwrite, string newName,
            CancellationToken cancellationToken, MoveRenamedFromDelegate fallback)
        {
            try
            {
                if (Inner is not IMoveRenamedFrom innerMoveRenamedFrom
                    || fileToMove is not IWrapper<IFile> { Inner: IChildFile innerFile }
                    || source is not IWrapper<IFolder> { Inner: IModifiableFolder innerSource })
                    return await fallback(this, fileToMove, source, overwrite, newName, cancellationToken).ConfigureAwait(false);

                var movedFile = await innerMoveRenamedFrom.MoveFileImmediatelyFrom(innerFile, innerSource, overwrite, newName, cancellationToken).ConfigureAwait(false);
                return NewFile(movedFile);
            }
            catch (NotSupportedException)
            {
                throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpListenerException((int)HttpStatusCode.Forbidden);
            }
            catch (Exception)
            {
                throw new HttpListenerException((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc/>
        public virtual async Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
        {
            // Return error
            if (!IsWritable)
                throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

            if (Inner is not IModifiableFolder modifiableFolder)
                throw new HttpListenerException((int)HttpStatusCode.Forbidden, "Folder does not support deletion.");

            try
            {
                // Try to find the item to delete
                var itemToDelete = await Inner.GetFirstByNameAsync(item.Name, cancellationToken).ConfigureAwait(false);
                await modifiableFolder.DeleteAsync(itemToDelete, cancellationToken).ConfigureAwait(false);
            }
            catch (FileNotFoundException)
            {
                throw new HttpListenerException((int)HttpStatusCode.NotFound);
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpListenerException((int)HttpStatusCode.Forbidden);
            }
            catch (HttpListenerException)
            {
                throw;
            }
            catch (Exception)
            {
                // TODO(wd): Add logging
                throw new HttpListenerException((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteAsync(IStorableChild item, long sizeHint = -1L, bool deleteImmediately = false,
            CancellationToken cancellationToken = default)
        {
            if (Inner is IRecyclableFolder recyclableFolder)
            {
                try
                {
                    // Try to find the item to delete
                    var itemToDelete = await Inner.GetFirstByNameAsync(item.Name, cancellationToken).ConfigureAwait(false);
                    await recyclableFolder.DeleteAsync(itemToDelete, sizeHint, deleteImmediately, cancellationToken).ConfigureAwait(false);
                }
                catch (FileNotFoundException)
                {
                    throw new HttpListenerException((int)HttpStatusCode.NotFound);
                }
                catch (UnauthorizedAccessException)
                {
                    throw new HttpListenerException((int)HttpStatusCode.Forbidden);
                }
                catch (HttpListenerException)
                {
                    throw;
                }
                catch (Exception)
                {
                    // TODO(wd): Add logging
                    throw new HttpListenerException((int)HttpStatusCode.InternalServerError);
                }
            }
            else
            {
                await DeleteAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            // Return error
            if (!IsWritable)
                throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

            if (Inner is not IModifiableFolder modifiableFolder)
                throw new HttpListenerException((int)HttpStatusCode.Forbidden, "Folder does not support creating subfolders.");

            try
            {
                // Check if the folder already exists and handle overwrite
                try
                {
                    var existing = await Inner.GetFirstByNameAsync(name, cancellationToken).ConfigureAwait(false);
                    if (existing is IFolder)
                    {
                        if (!overwrite)
                            throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

                        // Delete the existing folder if overwrite is allowed
                        await modifiableFolder.DeleteAsync(existing, deleteImmediately: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (FileNotFoundException)
                {
                    // Item doesn't exist, which is fine
                }

                // Create the folder
                var newFolder = await modifiableFolder.CreateFolderAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
                return NewFolder(newFolder);
            }
            catch (HttpListenerException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpListenerException((int)HttpStatusCode.Forbidden);
            }
            catch (Exception)
            {
                throw new HttpListenerException((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            // Return error
            if (!IsWritable)
                throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

            if (Inner is not IModifiableFolder modifiableFolder)
                throw new HttpListenerException((int)HttpStatusCode.Forbidden, "Folder does not support creating files.");

            try
            {
                // Check if the file already exists and handle overwrite
                try
                {
                    var existing = await Inner.GetFirstByNameAsync(name, cancellationToken).ConfigureAwait(false);
                    if (existing is IFile)
                    {
                        if (!overwrite)
                            throw new HttpListenerException((int)HttpStatusCode.PreconditionFailed);

                        // Delete the existing file if overwrite is allowed
                        await modifiableFolder.DeleteAsync(existing, deleteImmediately: true, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (FileNotFoundException)
                {
                    // Item doesn't exist, which is fine
                }

                // Create the file
                var newFile = await modifiableFolder.CreateFileAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
                return NewFile(newFile);
            }
            catch (HttpListenerException)
            {
                throw;
            }
            catch (UnauthorizedAccessException)
            {
                throw new HttpListenerException((int)HttpStatusCode.Forbidden);
            }
            catch (Exception)
            {
                throw new HttpListenerException((int)HttpStatusCode.InternalServerError);
            }
        }

        /// <inheritdoc/>
        public virtual bool SupportsFastMove(IDavFolder destination, string destinationName, bool overwrite)
        {
            // Fast move is only supported if both source and destination are backed by the same type
            // and the inner folder supports IMoveFrom or similar
            return true;
        }

        /// <summary>
        /// Creates a new <see cref="BackedDavFile"/> for the given file.
        /// </summary>
        protected virtual BackedDavFile NewFile(IFile file)
        {
            return new BackedDavFile(file, IsWritable, LockingManager);
        }

        /// <summary>
        /// Creates a new <see cref="BackedDavFolder"/> for the given folder.
        /// </summary>
        protected virtual BackedDavFolder NewFolder(IFolder folder)
        {
            return new BackedDavFolder(folder, IsWritable, LockingManager);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not BackedDavFolder other)
                return false;

            return other.Id.Equals(Id, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

