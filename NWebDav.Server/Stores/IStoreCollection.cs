using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Stores
{
    public interface IStoreCollection : IStoreItem, IDavFolder
    {
        // TODO: ISupportsMove
        Task<IStoreItem> MoveItemAsync_Dav(IStoreItem storeItem, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken = default);


        // Create items and collections and add to the collection
        Task<StoreItemResult> CreateItemAsync_Dav(string name, bool overwrite, CancellationToken cancellationToken);
        Task<StoreCollectionResult> CreateCollectionAsync_Dav(string name, bool overwrite, CancellationToken cancellationToken);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove_Dav(IStoreCollection destination, string destinationName, bool overwrite);
    }
}
