using OwlCore.Storage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV file.
    /// </summary>
    public interface IDavFile : IDavStorable, IChildFile
    {
    }
}
