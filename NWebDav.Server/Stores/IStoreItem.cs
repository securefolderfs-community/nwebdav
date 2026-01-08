using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Stores
{
    public interface IStoreItem : IDavStorable
    {
        // Property support
        IPropertyManager PropertyManager { get; }

        // Locking support
        ILockingManager? LockingManager { get; }

        // Copy support
        Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken);
    }
}
