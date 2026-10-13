using System.Net.Http.Headers;
using System.Text;

namespace DataEditorX.Common
{
    /// <summary>
    /// Retrieves the small release manifest without reusing GitHub's cached raw response.
    /// </summary>
    internal static class UpdateMetadataReader
    {
        internal static async Task<(bool Success, string Content, string Error)> ReadAsync(
            HttpClient client, string url, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using CancellationTokenSource deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(timeout);
            try
            {
                using HttpRequestMessage request = CreateRequest(url);
                using HttpResponseMessage response = await client
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token)
                    .ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    return (false, "", $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                }

                // ResponseHeadersRead completes before the body arrives. Keep the same
                // deadline through the read so an unresponsive body cannot hang the check.
                await using Stream stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
                using StreamReader reader = new(stream, Encoding.UTF8);
                string content = await reader.ReadToEndAsync(deadline.Token).ConfigureAwait(false);
                return (true, content, "");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return (false, "", "Update check canceled.");
            }
            catch (OperationCanceledException)
            {
                return (false, "", "The update server did not finish responding before the update check timed out.");
            }
            catch (Exception ex)
            {
                return (false, "", ex.GetBaseException().Message);
            }
        }

        private static HttpRequestMessage CreateRequest(string url)
        {
            Uri uri = new(url, UriKind.Absolute);
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("The update metadata URL must use HTTP or HTTPS.");
            }

            // GitHub raw responses are cached for several minutes. A new cache key lets
            // each user-requested check avoid reusing an older manifest response.
            // Limit this to GitHub: arbitrary query additions can invalidate signed URLs.
            if (uri.Host.Equals("raw.githubusercontent.com", StringComparison.OrdinalIgnoreCase))
            {
                UriBuilder builder = new(uri);
                string query = builder.Query.TrimStart('?');
                builder.Query = (query.Length > 0 ? query + "&" : "")
                    + "_dataeditorx_check=" + Guid.NewGuid().ToString("N");
                uri = builder.Uri;
            }

            HttpRequestMessage request = new(HttpMethod.Get, uri);
            request.Headers.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MaxAge = TimeSpan.Zero
            };
            request.Headers.Pragma.Add(new NameValueHeaderValue("no-cache"));
            return request;
        }
    }
}
