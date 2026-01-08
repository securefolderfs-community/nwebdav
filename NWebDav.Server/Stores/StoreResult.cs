using System.Net;
using NWebDav.Server.Storage;

namespace NWebDav.Server.Stores
{
    public readonly record struct StoreItemResult(HttpStatusCode Result, IDavStorable? Item = null);
    public readonly record struct StoreCollectionResult(HttpStatusCode Result, IDavFolder? Collection = null);
}
