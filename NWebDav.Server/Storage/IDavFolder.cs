using NWebDav.Server.Enums;
using OwlCore.Storage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV folder.
    /// </summary>
    // TODO: Implement interfaces
    public interface IDavFolder : IDavStorable, IChildFolder, IGetFirstByName//, IModifiableFolder, IGetItem, IGetItemRecursive
    {
        /// <summary>
        /// Gets the depth mode for enumerating directory contents.
        /// </summary>
        EnumerationDepthMode DepthMode { get; }
    }
}
