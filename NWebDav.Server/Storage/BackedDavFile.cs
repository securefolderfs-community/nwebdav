using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using OwlCore.Storage;
using SecureFolderFS.Shared.Extensions;
using SecureFolderFS.Storage.Extensions;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// A WebDAV file backed by an <see cref="IFile"/> instance.
    /// </summary>
    [DebuggerDisplay("{Inner.Name}")]
    public class BackedDavFile : IDavFile
    {
        /// <summary>
        /// Gets the inner backing storage file.
        /// </summary>
        public IFile Inner { get; }

        /// <inheritdoc/>
        public string Name => Inner.Name;

        /// <inheritdoc/>
        public string Id => Inner.Id;

        /// <summary>
        /// Gets a value indicating whether this file is writable.
        /// </summary>
        public bool IsWritable { get; }

        /// <inheritdoc/>
        public IPropertyManager PropertyManager => DefaultPropertyManager;

        /// <inheritdoc/>
        public ILockingManager? LockingManager { get; }

        /// <summary>
        /// Gets the default property manager for <see cref="BackedDavFile"/>.
        /// </summary>
        public static PropertyManager<BackedDavFile> DefaultPropertyManager { get; } = new(new DavProperty<BackedDavFile>[]
        {
            // RFC-2518 properties
            new DavCreationDate<BackedDavFile>
            {
                Getter = (context, item) => File.GetCreationTimeUtc(item.GetDeepestWrapper().Inner.Id),
                Setter = (context, item, value) =>
                {
                    File.SetCreationTimeUtc(item.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new DavDisplayName<BackedDavFile>
            {
                Getter = (context, item) => item.Name
            },
            new DavGetContentLength<BackedDavFile>
            {
                Getter = (context, item) => Math.Max(0, item.Inner.GetSizeAsync().GetAwaiter().GetResult())
            },
            new DavGetContentType<BackedDavFile>
            {
                Getter = (context, item) => item.DetermineContentType()
            },
            new DavGetEtag<BackedDavFile>
            {
                Getter = (context, item) => $"{item.Inner.GetSizeAsync().GetAwaiter().GetResult()}-{File.GetLastWriteTimeUtc(item.GetDeepestWrapper().Inner.Id).ToFileTime()}"
            },
            new DavGetLastModified<BackedDavFile>
            {
                Getter = (context, item) => File.GetLastWriteTimeUtc(item.GetDeepestWrapper().Inner.Id),
                Setter = (context, item, value) =>
                {
                    File.SetLastWriteTimeUtc(item.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new DavGetResourceType<BackedDavFile>
            {
                Getter = (context, item) => null
            },

            // Default locking property handling via the LockingManager
            new DavLockDiscoveryDefault<BackedDavFile>(),
            new DavSupportedLockDefault<BackedDavFile>(),

            // Hopmann/Lippert collection properties
            // (although not a collection, the IsHidden property might be valuable)
            new DavExtCollectionIsHidden<BackedDavFile>
            {
                Getter = (context, item) => (new FileInfo(item.GetDeepestWrapper().Inner.Id).Attributes & FileAttributes.Hidden) != 0
            },

            // Win32 extensions
            new Win32CreationTime<BackedDavFile>
            {
                Getter = (context, item) => File.GetCreationTimeUtc(item.GetDeepestWrapper().Inner.Id),
                Setter = (context, item, value) =>
                {
                    File.SetCreationTimeUtc(item.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastAccessTime<BackedDavFile>
            {
                Getter = (context, item) => File.GetLastAccessTimeUtc(item.GetDeepestWrapper().Inner.Id),
                Setter = (context, item, value) =>
                {
                    File.SetLastAccessTimeUtc(item.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32LastModifiedTime<BackedDavFile>
            {
                Getter = (context, item) => File.GetLastWriteTimeUtc(item.GetDeepestWrapper().Inner.Id),
                Setter = (context, item, value) =>
                {
                    File.SetLastWriteTimeUtc(item.GetDeepestWrapper().Inner.Id, value);
                    return HttpStatusCode.OK;
                }
            },
            new Win32FileAttributes<BackedDavFile>
            {
                Getter = (context, item) => new FileInfo(item.GetDeepestWrapper().Inner.Id).Attributes,
                Setter = (context, item, value) =>
                {
                    new FileInfo(item.GetDeepestWrapper().Inner.Id).Attributes = value;
                    return HttpStatusCode.OK;
                }
            }
        });

        /// <summary>
        /// Initializes a new instance of the <see cref="BackedDavFile"/> class.
        /// </summary>
        /// <param name="file">The backing file.</param>
        /// <param name="isWritable">Whether the file is writable.</param>
        /// <param name="lockingManager">The locking manager.</param>
        public BackedDavFile(IFile file, bool isWritable, ILockingManager? lockingManager = null)
        {
            Inner = file ?? throw new ArgumentNullException(nameof(file));
            IsWritable = isWritable;
            LockingManager = lockingManager;
        }

        /// <inheritdoc/>
        public virtual async Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
        {
            return accessMode switch
            {
                FileAccess.Read => await Inner.OpenStreamAsync(FileAccess.Read, cancellationToken).ConfigureAwait(false),
                FileAccess.Write => await Inner.OpenStreamAsync(FileAccess.Write, cancellationToken).ConfigureAwait(false),
                FileAccess.ReadWrite => await Inner.OpenStreamAsync(FileAccess.ReadWrite, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException(nameof(accessMode), accessMode, null)
            };
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

        /// <summary>
        /// Determines the content type of the file based on its name.
        /// </summary>
        protected virtual string DetermineContentType()
        {
            return MimeTypeHelper.GetMimeType(Name);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj is not BackedDavFile other)
                return false;

            return other.Id.Equals(Id, StringComparison.CurrentCultureIgnoreCase);
        }
    }
}

