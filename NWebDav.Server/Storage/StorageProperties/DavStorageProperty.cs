using System;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Storage.StorageProperties
{
    /// <inheritdoc cref="IDavProperty"/>
    internal abstract class DavStorageProperty : IDavProperty
    {
        /// <inheritdoc/>
        public string Name { get; }

        /// <inheritdoc/>
        public object Value { get; }

        /// <inheritdoc/>
        public event EventHandler<object>? ValueUpdated;

        protected DavStorageProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        /// <inheritdoc/>
        public abstract Task ModifyAsync(object newValue, CancellationToken cancellationToken = default);

        /// <inheritdoc/>
        public abstract void Dispose();
    }
}
