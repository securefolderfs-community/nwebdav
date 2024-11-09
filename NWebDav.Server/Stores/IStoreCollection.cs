using NWebDav.Server.Enums;
using OwlCore.Storage;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public interface IStoreCollection : IStoreItem // TODO(wd): Replace with IDavFolder, IDavStorable
    {
        // TODO: IFolder
        IAsyncEnumerable<IStoreItem> GetItemsAsync(StorableType type = StorableType.All, CancellationToken cancellationToken = default);

        // TODO: IGetFirstByName
        Task<IStoreItem> GetFirstByNameAsync(string name, CancellationToken cancellationToken = default);


        // Create items and collections and add to the collection
        Task<StoreItemResult> CreateItemAsync(string name, bool overwrite, CancellationToken cancellationToken);
        Task<StoreCollectionResult> CreateCollectionAsync(string name, bool overwrite, CancellationToken cancellationToken);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IStoreCollection destination, string destinationName, bool overwrite);

        // Move items between collections
        Task<StoreItemResult> MoveItemAsync(string sourceName, IStoreCollection destination, string destinationName, bool overwrite, CancellationToken cancellationToken);

        // Delete items from collection
        Task<HttpStatusCode> DeleteItemAsync(string name, CancellationToken cancellationToken);

        EnumerationDepthMode InfiniteDepthMode { get; }
    }
}
