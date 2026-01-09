using System.Threading;
using System.Threading.Tasks;
using NWebDav.Server.Enums;
using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV folder.
    /// </summary>
    public interface IDavFolder : IDavStorable, IChildFolder, IGetFirstByName, IModifiableFolder, IWrapper<IFolder>
    {
        /// <summary>
        /// Gets the depth mode for enumerating directory contents.
        /// </summary>
        EnumerationDepthMode DepthMode { get; }

        // TODO: ISupportsMove
        Task<IDavStorable> MoveItemAsync(IDavStorable item, IDavFolder destination, string destinationName, bool overwrite, CancellationToken cancellationToken = default);

        // Checks if the collection can be moved directly to the destination
        bool SupportsFastMove(IDavFolder destination, string destinationName, bool overwrite);
    }
}
