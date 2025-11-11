using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Foca.SerpApiSearch.Api
{
    public sealed class BingSimpleClient : IDisposable
    {
        private static readonly HttpClient _http = CreateClient();

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();
            try { handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; } catch { }
            var c = new HttpClient(handler);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("FOCA-SerpApiSearch-BingSimple/1.0");
            try { c.DefaultRequestHeaders.Accept.ParseAdd("application/json"); } catch { }
            return c;
        }

        public async Task<(bool ok, string error, string json)> SearchPageAsync(
            string apiKey,
            string query,
            int first = 0,
            int count = 10,
            string setlang = null,
            string cc = null,
            CancellationToken ct = default(CancellationToken),
            Action<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "API Key no configurada", null);
            if (count <= 0) count = 10;
            if (first < 0) first = 0;

            int attempt = 0;
            while (attempt < 3)
            {
                bool useCache = (attempt >= 1); // intento 2 y 3 permiten caché
                bool dropLocale = (attempt >= 2); // intento 3 sin setlang/cc/mkt

                var sl = dropLocale ? null : setlang;
                var country = dropLocale ? null : cc;

                var url = BuildUrl(apiKey, query, first, count, sl, country);

                progress?.Invoke($"Bing: solicitando página {(first/10)+1} (intento {attempt+1})…");
                try
                {
#if FOCA_API
                    Foca.SerpApiSearch.PluginLogger.Debug($"[SerpApi][Bing] URL => {url}");
#endif
                    System.Diagnostics.Debug.WriteLine($"[SerpApi][Bing] URL => {url}");
                }
                catch { }
                var res = await GetAsync(url, ct);
                if (res.ok) return res;

                var err = (res.error ?? string.Empty).ToLowerInvariant();
                bool is5xx = err.Contains("error http 5") || err.Contains("503") || err.Contains("502") || err.Contains("504");
                bool isTimeout = err.Contains("timeout");
                if (!is5xx && !isTimeout)
                {
                    return res;
                }

                int delay = 1200 * (int)Math.Pow(2, attempt) + new Random().Next(100, 400);
                progress?.Invoke("Bing: reintentando…");
                try { await Task.Delay(delay, ct); } catch { }
                attempt++;
            }
            return (false, "Bing: 503/5xx tras reintentos. Inténtalo de nuevo en unos segundos.", null);
        }

        public async Task<(bool ok, string error, string json)> FollowNextAsync(string nextUrl, string apiKey, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(nextUrl)) return (false, "No hay siguiente página", null);
            var url = EnsureApiKey(nextUrl, apiKey);
            var res = await GetAsync(url, ct);
            if (res.ok) return res;

            var err = (res.error ?? string.Empty).ToLowerInvariant();
            bool is5xxOrTimeout = err.Contains("error http 5") || err.Contains("503") || err.Contains("502") || err.Contains("504") || err.Contains("timeout");
            if (!is5xxOrTimeout) return res;

            try { await Task.Delay(1200 + new Random().Next(100, 400), ct); } catch { }
            return await GetAsync(url, ct);
        }

        public static (List<string> links, string nextUrl) ExtractLinksAndNext(string json, string domainHost = null)
        {
            if (string.IsNullOrWhiteSpace(json)) return (new List<string>(), null);
            try
            {
                var jobj = JObject.Parse(json);

                var organic = jobj["organic_results"] as JArray;
                var links = organic?.OfType<JObject>()
                    .Select(o => ((o["link"]?.ToString() ?? o["url"]?.ToString()) ?? string.Empty).Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList() ?? new List<string>();

                // Filtro opcional por host objetivo (apex, www.apex o subdominios)
                if (!string.IsNullOrWhiteSpace(domainHost) && links.Count > 0)
                {
                    var apex = domainHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? domainHost.Substring(4) : domainHost;
                    links = links.Where(u =>
                    {
                        try
                        {
                            var h = new Uri(u).Host;
                            return h.Equals(apex, StringComparison.OrdinalIgnoreCase)
                                || h.Equals(domainHost, StringComparison.OrdinalIgnoreCase)
                                || h.EndsWith("." + apex, StringComparison.OrdinalIgnoreCase);
                        }
                        catch { return false; }
                    }).ToList();
                }

                // Añadir inline_results pero filtrados estrictamente por host objetivo
                var inline = jobj["inline_results"] as JArray;
                if (inline != null && inline.Count > 0)
                {
                    var inlineLinks = inline.OfType<JObject>()
                        .Select(o => ((o["link"]?.ToString() ?? o["url"]?.ToString()) ?? string.Empty).Trim())
                        .Where(s => !string.IsNullOrWhiteSpace(s))
                        .ToList();

                    if (!string.IsNullOrWhiteSpace(domainHost))
                    {
                        var apex = domainHost.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? domainHost.Substring(4) : domainHost;
                        inlineLinks = inlineLinks.Where(u =>
                        {
                            try
                            {
                                var h = new Uri(u).Host;
                                return h.Equals(apex, StringComparison.OrdinalIgnoreCase)
                                    || h.Equals(domainHost, StringComparison.OrdinalIgnoreCase)
                                    || h.EndsWith("." + apex, StringComparison.OrdinalIgnoreCase);
                            }
                            catch { return false; }
                        }).ToList();
                    }

                    foreach (var u in inlineLinks)
                    {
                        if (!links.Contains(u, StringComparer.OrdinalIgnoreCase))
                        {
                            links.Add(u);
                        }
                    }
                }

                var nextToken = jobj.SelectToken("serpapi_pagination.next")
                             ?? jobj.SelectToken("serpapi_pagination.next_link")
                             ?? jobj.SelectToken("pagination.next");
                var nextUrl = nextToken?.ToString();

                return (links, nextUrl);
            }
            catch
            {
                return (new List<string>(), null);
            }
        }

        private static string BuildUrl(string apiKey, string query, int first, int count, string setlang, string cc)
        {
            var u = $"https://serpapi.com/search.json?engine=bing&q={Uri.EscapeDataString(query ?? string.Empty)}" +
                    $"&safe_search=Off&device=desktop&api_key={Uri.EscapeDataString(apiKey)}";

            // En la primera página no enviamos first/count; a partir de la segunda, sí.
            if (first > 0)
            {
                u += $"&first={first}";
                if (count > 0) u += $"&count={count}";
            }

            if (!string.IsNullOrWhiteSpace(setlang)) u += $"&setlang={Uri.EscapeDataString(setlang)}";
            if (!string.IsNullOrWhiteSpace(cc)) u += $"&cc={Uri.EscapeDataString(cc)}";
            if (!string.IsNullOrWhiteSpace(setlang) && !string.IsNullOrWhiteSpace(cc))
            {
                var mkt = $"{setlang}-{cc.ToUpperInvariant()}";
                u += $"&mkt={Uri.EscapeDataString(mkt)}";
            }
            return u;
        }

        private static string EnsureApiKey(string url, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(url)) return url;
            if (url.IndexOf("api_key=", StringComparison.OrdinalIgnoreCase) >= 0) return url;
            return url + (url.Contains("?") ? "&" : "?") + "api_key=" + Uri.EscapeDataString(apiKey ?? string.Empty);
        }

        private static async Task<(bool ok, string error, string json)> GetAsync(string url, CancellationToken ct)
        {
            try
            {
                var res = await _http.GetAsync(url, ct);
                var body = await res.Content.ReadAsStringAsync();
                if (!res.IsSuccessStatusCode)
                {
                    if (res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden)
                        return (false, "Acceso denegado por SerpApi (401/403). Verifica la API Key o el plan.", null);
                    return (false, $"Error HTTP {(int)res.StatusCode}: {res.ReasonPhrase}", null);
                }
                try
                {
                    var jobj = JObject.Parse(body);
                    var explicitError = jobj["error"]?.ToString() ?? jobj["serpapi_error"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(explicitError))
                        return (false, $"SerpApi devolvió error: {explicitError}", null);
                }
                catch { }
                return (true, null, body);
            }
            catch (TaskCanceledException)
            {
                return (false, "Timeout al contactar con SerpApi (Bing).", null);
            }
            catch (Exception ex)
            {
                return (false, $"Error de red: {ex.Message}", null);
            }
        }

        public void Dispose()
        {
        }
    }
}


