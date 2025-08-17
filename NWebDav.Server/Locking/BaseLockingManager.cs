using NWebDav.Server.Stores;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Locking
{
    /// <inheritdoc cref="ILockingManager"/>
    public abstract class BaseLockingManager : ILockingManager
    {
        /// <inheritdoc/>
        public async Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return Lock(item, lockType, lockScope, owner, lockRootUri, recursiveLock, timeouts);
        }

        /// <inheritdoc/>
        public async Task<HttpStatusCode> UnlockAsync(IStoreItem item, Uri token, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return Unlock(item, token);
        }

        /// <inheritdoc/>
        public async Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return RefreshLock(item, recursiveLock, timeouts, lockTokenUri);
        }

        /// <inheritdoc/>
        IEnumerable<LockEntry> ILockingManager.GetSupportedLocks(IStoreItem item)
        {
            return GetSupportedLocks(item);
        }

        /// <inheritdoc/>
        public async Task<bool> IsLockedAsync(IStoreItem item, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return IsLocked(item);
        }

        /// <inheritdoc/>
        public async Task<bool> HasLockAsync(IStoreItem item, Uri lockToken, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return HasLock(item, lockToken);
        }

        /// <inheritdoc/>
        public abstract IAsyncEnumerable<ActiveLock> GetActiveLockInfoAsync(IStoreItem item, CancellationToken cancellationToken);

        protected abstract IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

        protected abstract LockResult Lock(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts);

        protected abstract HttpStatusCode Unlock(IStoreItem item, Uri token);

        protected abstract LockResult RefreshLock(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri);

        protected abstract bool IsLocked(IStoreItem item);

        protected abstract bool HasLock(IStoreItem item, Uri lockToken);
    }
}
