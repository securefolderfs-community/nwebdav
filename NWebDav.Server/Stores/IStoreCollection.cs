using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Stores
{
    public interface IStoreCollection : IStoreItem, IDavFolder
    {
        // TODO: ISupportsMove
        Task<IStoreItem> MoveItemAsync_Dav(IStoreItem storeItem, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken = default);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite);
    }
}
