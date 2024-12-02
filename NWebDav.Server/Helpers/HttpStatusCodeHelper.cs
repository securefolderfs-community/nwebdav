using System.Net;

namespace NWebDav.Server.Helpers
{
    /// <summary>
    /// Helper methods for the <see cref="HttpStatusCode"/> enumeration.
    /// </summary>
    public static class HttpStatusCodeHelper
    {
        /// <summary>
        /// Obtain the human-readable status description for the specified
        /// <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="httpStatusCode">
        /// Code for which the description should be obtained.
        /// </param>
        /// <returns>
        /// Human-readable representation of the WebDAV status code.
        /// </returns>
        public static string? GetStatusDescription(this HttpStatusCode httpStatusCode)
        {
            return httpStatusCode switch
            {
                HttpStatusCode.Processing => "Processing",
                HttpStatusCode.OK => "OK",
                HttpStatusCode.Created => "Created",
                HttpStatusCode.Accepted => "Accepted",
                HttpStatusCode.NoContent => "No Content",
                HttpStatusCode.PartialContent => "Partial Content",
                HttpStatusCode.MultiStatus => "Multi-Status",
                HttpStatusCode.NotModified => "Not Modified",
                HttpStatusCode.BadRequest => "Bad Request",
                HttpStatusCode.Unauthorized => "Unauthorized",
                HttpStatusCode.Forbidden => "Forbidden",
                HttpStatusCode.NotFound => "Not Found",
                HttpStatusCode.Conflict => "Conflict",
                HttpStatusCode.Gone => "Gone",
                HttpStatusCode.PreconditionFailed => "Precondition Failed",
                HttpStatusCode.MisdirectedRequest => "Misdirected Request",
                HttpStatusCode.UnprocessableEntity => "Unprocessable Entity",
                HttpStatusCode.Locked => "Locked",
                HttpStatusCode.FailedDependency => "Failed Dependency",
                HttpStatusCode.InternalServerError => "Internal Server Error",
                HttpStatusCode.NotImplemented => "Not Implemented",
                HttpStatusCode.BadGateway => "Bad Gateway",
                HttpStatusCode.ServiceUnavailable => "Service Unavailable",
                HttpStatusCode.InsufficientStorage => "Insufficient Storage",
                _ => null
            };
        }
    }
}
