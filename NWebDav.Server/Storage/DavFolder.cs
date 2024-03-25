using NWebDav.Server.Enums;
using OwlCore.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Storage
{
    public class DavFolder : DavStorable<IFolder>, IDavFolder
    {
        /// <inheritdoc/>
        public virtual EnumerationDepthMode DepthMode { get; } = EnumerationDepthMode.Assume0;

        public DavFolder(IFolder inner)
            : base(inner)
        {
        }

        /// <inheritdoc/>
        public virtual async IAsyncEnumerable<IStorableChild> GetItemsAsync(StorableType type = StorableType.All, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in Inner.GetItemsAsync(type, cancellationToken))
            {
                yield return item switch
                {
                    IChildFile file => (IStorableChild)Wrap(file),
                    IChildFolder folder => (IStorableChild)Wrap(folder),
                    _ => throw new InvalidOperationException("The enumerated item was neither a file nor a folder.")
                };
            }
        }

        /// <inheritdoc/>
        public async Task<IStorableChild> GetFirstByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await Inner.GetFirstByNameAsync(name, cancellationToken) switch
            {
                IChildFile file => (IStorableChild)Wrap(file),
                IChildFolder folder => (IStorableChild)Wrap(folder),
                _ => throw new InvalidCastException("Could not match the item to neither a file nor a folder.")
            };
        }

        /// <inheritdoc/>
        public async Task<IStorableChild> GetItemRecursiveAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!id.Contains(Id))
                throw new FileNotFoundException("The provided Id does not belong to an item in this folder.");

            return await Inner.GetItemRecursiveAsync(id, cancellationToken) switch
            {
                IChildFile file => (IStorableChild)Wrap(file),
                IChildFolder folder => (IStorableChild)Wrap(folder),
                _ => throw new InvalidCastException("Could not match the item to neither a file nor a folder.")
            };
        }

        /// <inheritdoc/>
        public async Task<IStorableChild> GetItemAsync(string id, CancellationToken cancellationToken = default)
        {
            if (!id.Contains(Id))
                throw new FileNotFoundException("The provided Id does not belong to an item in this folder.");

            return await Inner.GetItemAsync(id, cancellationToken) switch
            {
                IChildFile file => (IStorableChild)Wrap(file),
                IChildFolder folder => (IStorableChild)Wrap(folder),
                _ => throw new InvalidCastException("Could not match the item to neither a file nor a folder.")
            };
        }

        /// <inheritdoc/>
        public Task<IFolderWatcher> GetFolderWatcherAsync(CancellationToken cancellationToken = default)
        {
            // TODO(ns): Implement FolderWatcher for CryptoFolder
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public Task DeleteAsync(IStorableChild item, CancellationToken cancellationToken = default)
        {
            if (Inner is not IModifiableFolder modifiableFolder)
                throw new NotSupportedException("Modifying folder contents is not supported.");

            return modifiableFolder.DeleteAsync(item, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<IChildFolder> CreateFolderAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (Inner is not IModifiableFolder modifiableFolder)
                throw new NotSupportedException("Modifying folder contents is not supported.");

            var folder = await modifiableFolder.CreateFolderAsync(name, overwrite, cancellationToken);
            return (IChildFolder)Wrap(folder);
        }

        /// <inheritdoc/>
        public async Task<IChildFile> CreateFileAsync(string name, bool overwrite = false, CancellationToken cancellationToken = default)
        {
            if (Inner is not IModifiableFolder modifiableFolder)
                throw new NotSupportedException("Modifying folder contents is not supported.");

            var file = await modifiableFolder.CreateFileAsync(name, overwrite, cancellationToken);
            return (IChildFile)Wrap(file);
        }
    }
}
