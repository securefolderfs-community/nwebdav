using NWebDav.Server.Locking;
using NWebDav.Server.Props;
using OwlCore.Storage;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public interface IStoreItem : IStorable // TODO(wd): Replace with IDavFile, IDavStorable
    {
        // Property support
        IPropertyManager PropertyManager { get; }

        // Locking support
        ILockingManager? LockingManager { get; }

        // Copy support
        Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken);
    }
}
