using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NWebDav.Server.Stores
{
    public interface IStoreFile : IStoreItem
    {
        // Read/Write access to the data
        Task<Stream> GetReadableStreamAsync(HttpListenerContext context);
        Task<HttpStatusCode> UploadFromStreamAsync(HttpListenerContext context, Stream source);
    }
}
