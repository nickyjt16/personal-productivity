using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ProductivityHub.Api.Services;

// Best-effort fetch of a web page's <title>. Used to give bare URLs a friendly
// label. Bounded by a short timeout and a small read cap; any failure returns null.
public partial class TitleFetcher(IHttpClientFactory httpFactory, IConfiguration config,
    ILogger<TitleFetcher> logger)
{
    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public async Task<string?> FetchAsync(string url, CancellationToken ct)
    {
        // Allows tests (and anyone who wants offline behaviour) to switch this off.
        if (!(config.GetValue<bool?>("Bookmarks:AutoFetchTitles") ?? true)) return null;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            var client = httpFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ProductivityHub/1.0)");

            using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            if (!res.IsSuccessStatusCode) return null;

            var mediaType = res.Content.Headers.ContentType?.MediaType;
            if (mediaType is not null && !mediaType.Contains("html", StringComparison.OrdinalIgnoreCase))
                return null;

            // Read just the first chunk — the <title> lives in <head>.
            await using var stream = await res.Content.ReadAsStreamAsync(cts.Token);
            var buffer = new byte[256 * 1024];
            int total = 0, read;
            while (total < buffer.Length &&
                   (read = await stream.ReadAsync(buffer.AsMemory(total, buffer.Length - total), cts.Token)) > 0)
                total += read;

            var html = Encoding.UTF8.GetString(buffer, 0, total);
            var match = TitleTag().Match(html);
            if (!match.Success) return null;

            var title = Whitespace().Replace(WebUtility.HtmlDecode(match.Groups[1].Value), " ").Trim();
            if (title.Length == 0) return null;
            return title.Length > 300 ? title[..300] : title;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Title fetch failed for {Url}", url);
            return null;
        }
    }
}
