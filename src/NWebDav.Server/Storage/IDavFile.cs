using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV file.
    /// </summary>
    public interface IDavFile : IDavStorable, IChildFile, IWrapper<IFile>
    {
    }
}
