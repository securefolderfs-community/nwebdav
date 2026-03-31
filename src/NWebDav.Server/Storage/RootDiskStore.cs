using System;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Helpers;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Wraps an <see cref="IStore"/>. Can be used to specify the root directory of the server.
    /// </summary>
    public sealed class RootDiskStore : IStore
    {
        private readonly string _remoteRootDirectory;
        private readonly IStore _root;

        public RootDiskStore(string remoteRootDirectory, IStore root)
        {
            _remoteRootDirectory = remoteRootDirectory;
            _root = root;
        }

        public Task<IDavStorable?> GetItemAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (!uri.LocalPath.StartsWith($"/{_remoteRootDirectory}"))
                return Task.FromResult<IDavStorable?>(null);

            return _root.GetItemAsync(UriHelper.RemoveRootDirectory(uri, _remoteRootDirectory), cancellationToken);
        }

        public Task<IDavFolder?> GetCollectionAsync(Uri uri, CancellationToken cancellationToken)
        {
            if (!uri.LocalPath.StartsWith($"/{_remoteRootDirectory}"))
                return Task.FromResult<IDavFolder?>(null);

            return _root.GetCollectionAsync(UriHelper.RemoveRootDirectory(uri, _remoteRootDirectory), cancellationToken);
        }
    }
}