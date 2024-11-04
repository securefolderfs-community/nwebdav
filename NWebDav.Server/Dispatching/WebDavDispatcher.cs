using Microsoft.Extensions.Logging;
using NWebDav.Server.Stores;
using OwlCore.Storage;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NWebDav.Server.Dispatching
{
    /// <inheritdoc cref="IRequestDispatcher"/>
    public sealed class WebDavDispatcher : BaseDispatcher
    {
        private readonly IStore _store;
        private readonly IFolder _storageRoot;

        public WebDavDispatcher(IStore store, IFolder storageRoot, IRequestHandlerProvider requestHandlerFactory, ILogger? logger)
            : base(requestHandlerFactory, logger)
        {
            _store = store;
            _storageRoot = storageRoot;
        }

        /// <inheritdoc/>
        protected override async Task<bool> InvokeRequestAsync(IRequestHandler requestHandler, HttpListenerContext context, CancellationToken cancellationToken)
        {
            try
            {
                await requestHandler.HandleRequestAsync(context, _store, Logger, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (NotImplementedException)
            {
                return false;
            }
        }
    }
}
