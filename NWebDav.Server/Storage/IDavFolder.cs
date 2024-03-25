using NWebDav.Server.Enums;
using OwlCore.Storage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV folder.
    /// </summary>
    // TODO: Implement Copy/Move
    public interface IDavFolder : IDavStorable, IChildFolder, IModifiableFolder, IGetItem, IGetItemRecursive, IGetFirstByName
    {
        /// <summary>
        /// Gets the depth mode for enumerating directory contents.
        /// </summary>
        EnumerationDepthMode DepthMode { get; }
    }
}
