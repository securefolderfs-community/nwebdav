using OwlCore.Storage;
using SecureFolderFS.Shared.ComponentModel;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Storage
{
    public abstract class DavStorable<TCapability> : IDavStorable, IWrappable<IFile>, IWrappable<IFolder>, IWrapper<TCapability>
        where TCapability : IStorable
    {
        /// <inheritdoc/>
        public TCapability Inner { get; }

        /// <inheritdoc/>
        public virtual string Id => Inner.Id;

        /// <inheritdoc/>
        public virtual string Name => Inner.Name;

        protected DavStorable(TCapability inner)
        {
            Inner = inner;
        }

        /// <inheritdoc/>
        public async Task<IFolder?> GetParentAsync(CancellationToken cancellationToken = default)
        {
            if (Inner is not IStorableChild storableChild)
                throw new NotSupportedException("Retrieving the parent folder is not supported.");

            // TODO: Make sure we don't go outside the root
            var parent = await storableChild.GetParentAsync(cancellationToken);
            if (parent is null)
                return null;

            return (IFolder?)Wrap(parent);
        }

        /// <inheritdoc/>
        public virtual IWrapper<IFile> Wrap(IFile file)
        {
            return new DavFile(file);
        }

        /// <inheritdoc/>
        public virtual IWrapper<IFolder> Wrap(IFolder folder)
        {
            return new DavFolder(folder);
        }
    }
}
