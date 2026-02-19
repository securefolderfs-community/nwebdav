using System;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Locking;
using OwlCore.Storage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// A WebDAV store backed by an <see cref="IFolder"/> as the root.
    /// </summary>
    public class BackedDavStore : IStore
    {
        private readonly IFolder _rootFolder;
        private readonly bool _isWritable;
        private readonly ILockingManager? _lockingManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackedDavStore"/> class.
        /// </summary>
        /// <param name="rootFolder">The root folder for the store.</param>
        /// <param name="isWritable">Whether the store is writable.</param>
        /// <param name="lockingManager">The locking manager.</param>
        public BackedDavStore(IFolder rootFolder, bool isWritable = true, ILockingManager? lockingManager = null)
        {
            _rootFolder = rootFolder ?? throw new ArgumentNullException(nameof(rootFolder));
            _isWritable = isWritable;
            _lockingManager = lockingManager ?? new InMemoryLockingManager();
        }

        /// <inheritdoc/>
        public virtual async Task<IDavStorable?> GetItemAsync(Uri uri, CancellationToken cancellationToken)
        {
            // Get the relative path from the URI
            var relativePath = GetRelativePath(uri);

            // If empty path, return root folder
            if (string.IsNullOrEmpty(relativePath))
                return new BackedDavFolder(_rootFolder, _isWritable, _lockingManager);

            // Try to find the item by navigating through the path
            try
            {
                var item = await NavigateToItemAsync(relativePath, cancellationToken).ConfigureAwait(false);
                if (item is null)
                    return null;

                return item switch
                {
                    IFile file => new BackedDavFile(file, _isWritable, _lockingManager),
                    IFolder folder => new BackedDavFolder(folder, _isWritable, _lockingManager),
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IDavFolder?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken)
        {
            // Get the relative path from the URI
            var relativePath = GetRelativePath(uri);

            // If empty path, return root folder
            if (string.IsNullOrEmpty(relativePath))
                return new BackedDavFolder(_rootFolder, _isWritable, _lockingManager);

            // Try to find the folder by navigating through the path
            try
            {
                var item = await NavigateToItemAsync(relativePath, cancellationToken).ConfigureAwait(false);
                if (item is IFolder folder)
                    return new BackedDavFolder(folder, _isWritable, _lockingManager);

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the relative path from a URI.
        /// </summary>
        protected virtual string GetRelativePath(Uri uri)
        {
            // Decode the URI path and remove leading slash
            var path = Uri.UnescapeDataString(uri.LocalPath);
            if (path.StartsWith("/"))
                path = path.Substring(1);

            return path;
        }

        /// <summary>
        /// Navigates to an item by relative path.
        /// </summary>
        protected virtual async Task<IStorable?> NavigateToItemAsync(string relativePath, CancellationToken cancellationToken)
        {
            // Split the path into segments
            var segments = relativePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
                return _rootFolder;

            IFolder currentFolder = _rootFolder;

            // Navigate through each segment except the last
            for (int i = 0; i < segments.Length - 1; i++)
            {
                var segment = segments[i];

                if (currentFolder is not IGetFirstByName getFirstByName)
                    return null;

                var item = await getFirstByName.GetFirstByNameAsync(segment, cancellationToken).ConfigureAwait(false);
                if (item is not IFolder folder)
                    return null;

                currentFolder = folder;
            }

            // Get the final item
            var lastSegment = segments[segments.Length - 1];
            if (currentFolder is IGetFirstByName finalGetFirstByName)
            {
                try
                {
                    return await finalGetFirstByName.GetFirstByNameAsync(lastSegment, cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    return null;
                }
            }

            return null;
        }
    }
}

