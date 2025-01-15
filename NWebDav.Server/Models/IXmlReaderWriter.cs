using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace NWebDav.Server.ComponentModel
{
    public interface IXmlReaderWriter
    {
        Task<XDocument?> LoadXmlDocumentAsync(HttpListenerRequest request, CancellationToken cancellationToken);

        Task SendResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, XDocument xDocument);
    }
}
