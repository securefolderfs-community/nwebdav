using NWebDav.Server.Storage;
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
        public async Task<LockResult> LockAsync(IDavStorable item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return Lock(item, lockType, lockScope, owner, lockRootUri, recursiveLock, timeouts);
        }

        /// <inheritdoc/>
        public async Task<HttpStatusCode> UnlockAsync(IDavStorable item, Uri token, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return Unlock(item, token);
        }

        /// <inheritdoc/>
        public async Task<LockResult> RefreshLockAsync(IDavStorable item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return RefreshLock(item, recursiveLock, timeouts, lockTokenUri);
        }

        /// <inheritdoc/>
        IEnumerable<LockEntry> ILockingManager.GetSupportedLocks(IDavStorable item)
        {
            return GetSupportedLocks(item);
        }

        /// <inheritdoc/>
        public async Task<bool> IsLockedAsync(IDavStorable item, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return IsLocked(item);
        }

        /// <inheritdoc/>
        public async Task<bool> HasLockAsync(IDavStorable item, Uri lockToken, CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            cancellationToken.ThrowIfCancellationRequested();

            return HasLock(item, lockToken);
        }

        /// <inheritdoc/>
        public abstract IAsyncEnumerable<ActiveLock> GetActiveLockInfoAsync(IDavStorable item, CancellationToken cancellationToken);

        protected abstract IEnumerable<LockEntry> GetSupportedLocks(IDavStorable item);

        protected abstract LockResult Lock(IDavStorable item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts);

        protected abstract HttpStatusCode Unlock(IDavStorable item, Uri token);

        protected abstract LockResult RefreshLock(IDavStorable item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri);

        protected abstract bool IsLocked(IDavStorable item);

        protected abstract bool HasLock(IDavStorable item, Uri lockToken);
    }
}
