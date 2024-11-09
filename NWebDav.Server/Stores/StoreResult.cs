using System.Net;

namespace NWebDav.Server.Stores
{
    public readonly record struct StoreItemResult(HttpStatusCode Result, IStoreItem? Item = null);
    public readonly record struct StoreCollectionResult(HttpStatusCode Result, IStoreCollection? Collection = null);
}
