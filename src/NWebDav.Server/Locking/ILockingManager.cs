using NWebDav.Server.Storage;
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
        IEnumerable<LockEntry> GetSupportedLocks(IDavStorable item);

        Task<LockResult> LockAsync(IDavStorable item, LockType lockType, LockScope lockScope, XElement owner, Uri lockRootUri, bool recursiveLock, IEnumerable<int> timeouts, CancellationToken cancellationToken);

        Task<HttpStatusCode> UnlockAsync(IDavStorable item, Uri token, CancellationToken cancellationToken);

        Task<LockResult> RefreshLockAsync(IDavStorable item, bool recursiveLock, IEnumerable<int> timeouts, Uri lockTokenUri, CancellationToken cancellationToken);

        IAsyncEnumerable<ActiveLock> GetActiveLockInfoAsync(IDavStorable item, CancellationToken cancellationToken);

        Task<bool> IsLockedAsync(IDavStorable item, CancellationToken cancellationToken);

        Task<bool> HasLockAsync(IDavStorable item, Uri lockToken, CancellationToken cancellationToken);
    }
}
