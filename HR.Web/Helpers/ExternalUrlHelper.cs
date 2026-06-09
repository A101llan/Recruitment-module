using System;
using System.Configuration;
using System.Net;
using System.Web;

using HR.Web.Services;

namespace HR.Web.Helpers
{
    /// <summary>
    /// Helper for generating externally reachable base URLs.
    /// Uses SystemSettings/Web.config "ExternalBaseUrl" when set and valid for the current host;
    /// otherwise falls back to the incoming request (including the IIS application path, e.g. /HireHub).
    /// </summary>
    public static class ExternalUrlHelper
    {
        private const string DefaultBaseUrl = "http://localhost:5002";

        public static Uri GetBaseUri(HttpRequestBase request)
        {
            return ResolveBaseUri(
                request != null ? request.Url : null,
                request != null ? request.ApplicationPath : null);
        }

        public static Uri GetBaseUri(HttpRequest request)
        {
            return ResolveBaseUri(
                request != null ? request.Url : null,
                request != null ? request.ApplicationPath : null);
        }

        /// <summary>
        /// Full tenant portal URL: {base}/{tenantSlug} (safe for virtual directories and ExternalBaseUrl).
        /// </summary>
        public static string GetTenantPortalUrl(HttpRequestBase request, string tenantSlug)
        {
            var baseUrl = GetBaseUri(request).ToString().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(tenantSlug))
            {
                return baseUrl;
            }

            return baseUrl + "/" + tenantSlug.Trim().TrimStart('/');
        }

        public static string GetTenantPortalUrl(HttpRequest request, string tenantSlug)
        {
            var baseUrl = GetBaseUri(request).ToString().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(tenantSlug))
            {
                return baseUrl;
            }

            return baseUrl + "/" + tenantSlug.Trim().TrimStart('/');
        }

        private static Uri ResolveBaseUri(Uri requestUrl, string applicationPath)
        {
            var configuredBaseUri = GetConfiguredBaseUri();
            var requestBaseUri = BuildRequestBaseUri(requestUrl, applicationPath);

            Uri resolved;
            if (configuredBaseUri != null)
            {
                resolved = ShouldPreferRequestUrl(configuredBaseUri, requestUrl, requestBaseUri)
                    ? (requestBaseUri ?? configuredBaseUri)
                    : configuredBaseUri;
            }
            else
            {
                resolved = requestBaseUri ?? new Uri(DefaultBaseUrl, UriKind.Absolute);
            }

            return resolved ?? new Uri(DefaultBaseUrl, UriKind.Absolute);
        }

        private static Uri GetConfiguredBaseUri()
        {
            var settingsService = new SettingsService();
            var configured = settingsService.GetSetting("ExternalBaseUrl") ?? ConfigurationManager.AppSettings["ExternalBaseUrl"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                return null;
            }

            var trimmed = configured.Trim().TrimEnd('/');
            return Uri.TryCreate(trimmed, UriKind.Absolute, out var parsedUri) ? parsedUri : null;
        }

        private static Uri BuildRequestBaseUri(Uri requestUrl, string applicationPath)
        {
            if (requestUrl == null)
            {
                return null;
            }

            var builder = new UriBuilder(requestUrl.Scheme, requestUrl.Host);
            if (!IsDefaultPort(requestUrl.Scheme, requestUrl.Port))
            {
                builder.Port = requestUrl.Port;
            }

            builder.Path = NormalizeApplicationPath(applicationPath);
            return builder.Uri;
        }

        private static string NormalizeApplicationPath(string applicationPath)
        {
            if (string.IsNullOrWhiteSpace(applicationPath) || applicationPath == "/")
            {
                return "/";
            }

            return applicationPath.TrimEnd('/');
        }

        private static bool IsDefaultPort(string scheme, int port)
        {
            return (string.Equals(scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) && port == 80)
                || (string.Equals(scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) && port == 443);
        }

        /// <summary>
        /// Prefer the live request URL when settings still point at localhost but users reach a public host,
        /// or when both are loopback but ports/schemes differ (local dev).
        /// </summary>
        private static bool ShouldPreferRequestUrl(Uri configuredUri, Uri requestUrl, Uri requestBaseUri)
        {
            if (requestBaseUri == null || requestUrl == null)
            {
                return false;
            }

            if (configuredUri == null)
            {
                return true;
            }

            if (IsLoopbackHost(configuredUri.Host) && !IsLoopbackHost(requestUrl.Host))
            {
                return true;
            }

            return IsLocalDevPortOrSchemeMismatch(configuredUri, requestUrl);
        }

        private static bool IsLocalDevPortOrSchemeMismatch(Uri configuredUri, Uri requestUrl)
        {
            if (!IsLoopbackHost(configuredUri.Host) || !IsLoopbackHost(requestUrl.Host))
            {
                return false;
            }

            return configuredUri.Port != requestUrl.Port
                || !string.Equals(configuredUri.Scheme, requestUrl.Scheme, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackHost(string host)
        {
            if (string.IsNullOrWhiteSpace(host))
            {
                return false;
            }

            if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address);
        }
    }
}
