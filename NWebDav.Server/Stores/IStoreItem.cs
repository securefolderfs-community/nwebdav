using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Props;

namespace NWebDav.Server.Stores;

public interface IStoreItem
{
    /// <summary>
    /// Gets the name of the item, with the extension (if any).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a unique identifier for this item.
    /// </summary>
    string UniqueKey { get; }

    /// <summary>
    /// Gets the property management instance.
    /// </summary>
    IPropertyManager? PropertyManager { get; }

    // Read access to the data
    Task<Stream> GetReadableStreamAsync(CancellationToken cancellationToken);

    // Write access to the data
    Task<DavStatusCode> UploadFromStreamAsync(Stream source, CancellationToken cancellationToken);

    // Copy support
    Task<StoreItemResult> CopyAsync(IStoreCollection destination, string name, bool overwrite, CancellationToken cancellationToken);
}