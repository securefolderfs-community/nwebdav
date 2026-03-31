using System.Net;

namespace NWebDav.Server.Extensions
{
    public static class HttpListenerResponseExtensions
    {
        public static void SetHeaderValue(this HttpListenerResponse response, string header, string value)
        {
            switch (header)
            {
                case "Content-Length":
                    response.ContentLength64 = long.Parse(value);
                    break;

                case "Content-Type":
                    response.ContentType = value;
                    break;

                default:
                    response.Headers[header] = value;
                    break;
            }
        }
    }
}
