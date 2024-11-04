﻿using Microsoft.Extensions.Logging;
using NWebDav.Server.ComponentModel;
using NWebDav.Server.Helpers;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace NWebDav.Server.Models
{
    internal class XmlReaderWriter : IXmlReaderWriter
    {
        private static readonly XmlWriterSettings s_xmlWriterSettings = new()
        {
            OmitXmlDeclaration = false,
            Indent = false,
            Encoding = new UTF8Encoding(false),
        };
        private readonly ILogger<XmlReaderWriter> _logger;

        public XmlReaderWriter(ILogger<XmlReaderWriter> logger)
        {
            _logger = logger;
        }

        public async Task<XDocument?> LoadXmlDocumentAsync(HttpListenerRequest request, CancellationToken cancellationToken)
        {
            // If there is no input stream, then there is no XML document
            if (request.InputStream == Stream.Null)
                return null;

            // Return null if no content has been specified
            var contentLength = request.ContentLength64;
            if (contentLength == 0)
                return null;

            // Obtain an XML document from the stream
            var xDocument = await XDocument.LoadAsync(request.InputStream, LoadOptions.None, cancellationToken);
            LogXmlDocument(xDocument);

            return xDocument;
        }

        /// <summary>
        /// Send an HTTP response with an XML body content.
        /// </summary>
        /// <param name="response">
        /// The HTTP response that needs to be sent.
        /// </param>
        /// <param name="statusCode">
        /// WebDAV status code that should be set.
        /// </param>
        /// <param name="xDocument">
        /// XML document that should be sent as the body of the message.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous response send.
        /// </returns>
        public async Task SendResponseAsync(HttpListenerResponse response, HttpStatusCode statusCode, XDocument xDocument)
        {
            if (xDocument.Root == null) throw new ArgumentException("The specified XML document doesn't have a root node", nameof(xDocument));

            // Set the response
            response.SetStatus(statusCode);

            // Obtain the result as an XML document
            await using var ms = new MemoryStream();

            // ReSharper disable once UseAwaitUsing (XML writer is not enabled to perform async writing)
            await using (var xmlWriter = XmlWriter.Create(ms, s_xmlWriterSettings))
            {
                // Add the namespaces (Win7 WebDAV client requires them like this)
                xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.DavNsPrefix, WebDavNamespaces.DavNs);
                xDocument.Root.SetAttributeValue(XNamespace.Xmlns + WebDavNamespaces.Win32NsPrefix, WebDavNamespaces.Win32Ns);

                // Write the XML document to the stream
                xDocument.WriteTo(xmlWriter);
            }

            // Flush
            ms.Flush();

            // Dump the XML document to the logging
            LogXmlDocument(xDocument);

            // Set content type/length
            response.ContentType = "text/xml; charset=\"utf-8\"";
            response.ContentLength64 = ms.Position;

            // Reset stream and write the stream to the result
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(response.OutputStream).ConfigureAwait(false);
        }

        private void LogXmlDocument(XDocument xDocument)
        {
            // Dump the XML document to the logging
            if (xDocument.Root != null && _logger.IsEnabled(LogLevel.Debug))
            {
                // Format the XML document as an in-memory text representation
                using var sw = new StringWriter();
                using (var xmlWriter = XmlWriter.Create(sw, new XmlWriterSettings
                {
                    OmitXmlDeclaration = false,
                    Indent = true,
                    Encoding = Encoding.UTF8,
                }))
                {
                    // Write the XML document to the stream
                    xDocument.WriteTo(xmlWriter);
                }

                _logger.LogDebug(sw.ToString());
            }
        }
    }

}
