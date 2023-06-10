using System;
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
            return new Uri($"{uriText}/{path}", UriKind.Absolute);
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

        public static Uri EscapePercentSigns(Uri uri, bool escapePercent = false)
        {
            return new Uri(EscapePercentSigns(uri.ToString(), escapePercent));
        }
        
        public static string EscapePercentSigns(string path, bool escapePercent = false)
        {
            return escapePercent ? path.Replace("%", "%25") : Regex.Replace(path, "%(?!25)", "%25");
        }

        internal static Uri RemoveRootDirectory(Uri uri, string rootDirectory)
        {
            return new($"{uri.Scheme}://{uri.Host}:{uri.Port}{Regex.Replace(EscapePercentSigns(uri, true).LocalPath, $"^\\/{rootDirectory}", string.Empty)}");
        }
    }
}
