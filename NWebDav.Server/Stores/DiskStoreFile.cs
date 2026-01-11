using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using NWebDav.Server.Storage;
using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;

namespace NWebDav.Server.Stores
{
    [DebuggerDisplay("{_fileInfo.FullPath}")]
    public sealed class DiskStoreFile : IDavFile
    {
        private readonly FileInfo _fileInfo;

        /// <inheritdoc/>
        public string Name => _fileInfo.Name;

        /// <inheritdoc/>
        public string Id => _fileInfo.FullName;

        /// <inheritdoc/>
        IFile? IWrapper<IFile>.Inner => null;

        public DiskStoreFile(ILockingManager lockingManager, FileInfo fileInfo, bool isWritable)
        {
            LockingManager = lockingManager;
            _fileInfo = fileInfo;
            IsWritable = isWritable;
        }

        /// <inheritdoc/>
        public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
        {
            var stream = accessMode switch
            {
                FileAccess.Read => _fileInfo.OpenRead(),
                FileAccess.Write => _fileInfo.OpenWrite(),
                FileAccess.ReadWrite => _fileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite),
                _ => throw new ArgumentOutOfRangeException(nameof(accessMode), accessMode, null)
            };

            return Task.FromResult<Stream>(stream);
        }

        /// <inheritdoc/>
        public Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        {
            var parentDirectory = _fileInfo.Directory;
            if (parentDirectory is null)
                return Task.FromResult<IFolder?>(null);

            return Task.FromResult<IFolder?>(new DiskStoreCollection(LockingManager, parentDirectory, IsWritable));
        }

        public static PropertyManager<DiskStoreFile> DefaultPropertyManager { get; } = new PropertyManager<DiskStoreFile>(new DavProperty<DiskStoreFile>[]
        {
            // RFC-2518 properties
            new DavCreationDate<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.CreationTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new DavDisplayName<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.Name
            },
            new DavGetContentLength<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.Length
            },
            new DavGetContentType<DiskStoreFile>
            {
                Getter = (context, item) => item.DetermineContentType()
            },
            new DavGetEtag<DiskStoreFile>
            {
                // Calculating the Etag is an expensive operation,
                // because we need to scan the entire file.
                IsExpensive = true,
                Getter = (context, item) => item.CalculateEtag()
            },
            new DavGetLastModified<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new DavGetResourceType<DiskStoreFile>
            {
                Getter = (context, item) => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<DiskStoreFile>(),
            new DavSupportedLockDefault<DiskStoreFile>(),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<DiskStoreFile>
            {
                Getter = (context, item) => (item._fileInfo.Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.CreationTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.CreationTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastAccessTime<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.LastAccessTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastAccessTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastModifiedTime<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.LastWriteTimeUtc,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.LastWriteTimeUtc = value;
                    return HttpStatusCode.OK;
                }
            },
            new Win32FileAttributes<DiskStoreFile>
            {
                Getter = (context, item) => item._fileInfo.Attributes,
                Setter = (context, item, value) =>
                {
                    item._fileInfo.Attributes = value;
                    return HttpStatusCode.OK;
                }
            }
        });

        public bool IsWritable { get; }


        public IPropertyManager PropertyManager => DefaultPropertyManager;
        public ILockingManager LockingManager { get; }

        public async Task<StoreItemResult> CopyAsync(IDavFolder destination, string name, bool overwrite, CancellationToken cancellationToken)
        {
            try
            {
                // If the destination is also a disk-store, then we can use the FileCopy API
                // (it's probably a bit more efficient than copying in C#)
                if (destination is DiskStoreCollection diskCollection)
                {
                    // Check if the collection is writable
                    if (!diskCollection.IsWritable)
                        return new StoreItemResult(HttpStatusCode.PreconditionFailed);

                    var destinationPath = Path.Combine(diskCollection.Id, name);

                    // Check if the file already exists
                    var fileExists = File.Exists(destinationPath);
                    if (fileExists && !overwrite)
                        return new StoreItemResult(HttpStatusCode.PreconditionFailed);

                    // Copy the file
                    File.Copy(_fileInfo.FullName, destinationPath, true);

                    // Return the appropriate status
                    return new StoreItemResult(fileExists ? HttpStatusCode.NoContent : HttpStatusCode.Created);
                }
                else
                {
                    // Create the item in the destination collection
                    IDavFile davFile;
                    try
                    {
                        davFile = (IDavFile)await destination.CreateFileAsync(name, overwrite, cancellationToken).ConfigureAwait(false);
                    }
                    catch (HttpListenerException ex)
                    {
                        return new StoreItemResult((HttpStatusCode)ex.ErrorCode);
                    }

                    // Copy the file content
                    try
                    {
                        await using var sourceStream = await OpenStreamAsync(FileAccess.Read, cancellationToken).ConfigureAwait(false);
                        await using var destinationStream = await davFile.OpenStreamAsync(FileAccess.Write, cancellationToken).ConfigureAwait(false);
                        await sourceStream.CopyToAsync(destinationStream, cancellationToken).ConfigureAwait(false);
                        await destinationStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (IOException ioException) when (ioException.IsDiskFull())
                    {
                        return new StoreItemResult(HttpStatusCode.InsufficientStorage, davFile);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        return new StoreItemResult(HttpStatusCode.Forbidden, davFile);
                    }

                    // Return result
                    return new StoreItemResult(HttpStatusCode.Created, davFile);
                }
            }
            catch (Exception exc)
            {
                // TODO(wd): Add logging
                //s_log.Log(LogLevel.Error, () => "Unexpected exception while copying data.", exc);
                return new StoreItemResult(HttpStatusCode.InternalServerError);
            }
        }

        public override int GetHashCode()
        {
            return _fileInfo.FullName.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is DiskStoreFile storeItem))
                return false;
            return storeItem._fileInfo.FullName.Equals(_fileInfo.FullName, StringComparison.CurrentCultureIgnoreCase);
        }

        private string DetermineContentType()
        {
            return MimeTypeHelper.GetMimeType(_fileInfo.Name);
        }

        private string CalculateEtag()
        {
            using (var stream = File.OpenRead(_fileInfo.FullName))
            {
                var hash = SHA256.Create().ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }
    }
}
