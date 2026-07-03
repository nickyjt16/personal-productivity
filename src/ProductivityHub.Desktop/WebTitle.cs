using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ProductivityHub.Desktop;

// Best-effort fetch of a page's <title> for bare URLs.
public static partial class WebTitle
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(6) };

    [GeneratedRegex(@"<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleTag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    public static async Task<string?> FetchAsync(string url)
    {
        if (!App.Settings.AutoFetchTitles) return null;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            return null;
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, uri);
            req.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; ProductivityHub/1.0)");
            using var res = await Http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
            if (!res.IsSuccessStatusCode) return null;
            var html = await res.Content.ReadAsStringAsync();
            var m = TitleTag().Match(html);
            if (!m.Success) return null;
            var title = Whitespace().Replace(WebUtility.HtmlDecode(m.Groups[1].Value), " ").Trim();
            if (title.Length == 0) return null;
            return title.Length > 300 ? title[..300] : title;
        }
        catch { return null; }
    }
}
