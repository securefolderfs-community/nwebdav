using System;

namespace NWebDav.Server.Extensions
{
    public static class UriExtensions
    {
        public static string GetEncodedUrl(this Uri url)
        {
            var encodedUrl = new UriBuilder
            {
                Scheme = url.Scheme,
                Host = url.Host,
                Port = url.IsDefaultPort ? -1 : url.Port,
                Path = Uri.EscapeDataString(url.AbsolutePath),
                Query = url.Query
            };

            return encodedUrl.ToString();
        }
    }
}
