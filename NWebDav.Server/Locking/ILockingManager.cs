using NWebDav.Server.Stores;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.Locking
{
    // TODO: Call the locking methods from the handlers
    public interface ILockingManager
    {
        IEnumerable<LockEntry> GetSupportedLocks(IStoreItem item);

        Task<LockResult> LockAsync(IStoreItem item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken);

        Task<HttpStatusCode> UnlockAsync(IStoreItem item, Uri token, CancellationToken cancellationToken);

        Task<LockResult> RefreshLockAsync(IStoreItem item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken);

        IAsyncEnumerable<ActiveLock> GetActiveLockInfoAsync(IStoreItem item, CancellationToken cancellationToken);

        Task<bool> IsLockedAsync(IStoreItem item, CancellationToken cancellationToken);

        Task<bool> HasLockAsync(IStoreItem item, Uri lockToken, CancellationToken cancellationToken);
    }
}
