using OwlCore.Storage;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Storage
{
    public class DavFile : DavStorable<IFile>, IDavFile
    {
        public DavFile(IFile inner)
            : base(inner)
        {
        }

        /// <inheritdoc/>
        public Task<Stream> OpenStreamAsync(FileAccess accessMode, CancellationToken cancellationToken = default)
        {
            return Inner.OpenStreamAsync(accessMode, cancellationToken);
        }
    }
}
