using OwlCore.Storage;

namespace NWebDav.Server.Storage
{
    /// <summary>
    /// Represents a WebDAV storable object. This is the base interface for all WebDAV file system objects.
    /// </summary>
    public interface IDavStorable : IStorableChild
    {
    }
}
