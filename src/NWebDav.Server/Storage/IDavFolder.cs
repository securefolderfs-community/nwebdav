using NWebDav.Server.Enums;
using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;
using SecureFolderFS.Storage.Recyclable;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV folder.
    /// </summary>
    public interface IDavFolder : IDavStorable, IChildFolder, IGetFirstByName, ICreateRenamedCopyOf, IMoveRenamedFrom, IRecyclableFolder, IWrapper<IFolder>
    {
        /// <summary>
        /// Gets the depth mode for enumerating directory contents.
        /// </summary>
        EnumerationDepthMode DepthMode { get; }

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IDavFolder destination, string destinationName, bool overwrite);
    }
}
