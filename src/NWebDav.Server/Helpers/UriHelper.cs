using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NWebDav.Server.Helpers
{
    public static class UriHelper
    {
        public static Uri Combine(Uri baseUri, string path)
        {
            var uriText = baseUri.OriginalString;
            if (uriText.EndsWith("/"))
                uriText = uriText.Substring(0, uriText.Length - 1);

            // Escape the appended segment; otherwise names containing '%XX' sequences
            // are treated as pre-encoded and round-trip to a different name
            return new Uri($"{uriText}/{Uri.EscapeDataString(path)}", UriKind.Absolute);
        }

        public static string ToEncodedString(Uri entryUri)
        {
            return entryUri
                .AbsoluteUri
                .Replace("#", "%23")
                .Replace("[", "%5B")
                .Replace("]", "%5D");
        }

        public static string GetDecodedPath(Uri uri)
        {
            return uri.LocalPath + Uri.UnescapeDataString(uri.Fragment);
        }

        internal static Uri RemoveRootDirectory(Uri uri, string rootDirectory)
        {
            // Strip the root directory from the decoded path, then re-escape each segment.
            // Rebuilding the URI from the decoded path directly would cause '%XX' sequences
            // in item names to be decoded a second time further down the pipeline.
            var strippedPath = new Regex($"^\\/{Regex.Escape(rootDirectory)}").Replace(uri.LocalPath, string.Empty);
            var escapedPath = string.Join('/', strippedPath.Split('/').Select(Uri.EscapeDataString));

            return new($"{uri.Scheme}://{uri.Host}:{uri.Port}{escapedPath}");
        }
    }
}
