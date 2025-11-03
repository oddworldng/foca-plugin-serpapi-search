using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Foca.SerpApiSearch.Api
{
    /// <summary>
    /// Minimal SerpApi client for DuckDuckGo engine.
    /// Handles rate limiting (RPM) and basic error handling.
    /// </summary>
    public class SerpApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly int _rpm;
        private readonly TimeSpan _timeout;
        private readonly int _maxRetries429;
        private readonly int _baseRetryDelayMs;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastRequest = DateTime.MinValue;

        public SerpApiClient()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("FOCA-SerpApiSearch/1.0");

            int rpm = 30;
            int timeoutSec = 20;
            int retries429 = 2;
            int baseDelayMs = 1500;
            try { rpm = int.Parse(ConfigurationManager.AppSettings["RequestsPerMinute"] ?? "30"); } catch { }
            try { timeoutSec = int.Parse(ConfigurationManager.AppSettings["SerpApiTimeoutSeconds"] ?? "20"); } catch { }
            try { retries429 = int.Parse(ConfigurationManager.AppSettings["SerpApiRetry429Max"] ?? "2"); } catch { }
            try { baseDelayMs = int.Parse(ConfigurationManager.AppSettings["SerpApiRetry429BaseDelayMs"] ?? "1500"); } catch { }
            _rpm = Math.Max(1, rpm);
            _timeout = TimeSpan.FromSeconds(Math.Max(5, timeoutSec));
            _httpClient.Timeout = _timeout;
            _maxRetries429 = Math.Max(0, retries429);
            _baseRetryDelayMs = Math.Max(250, baseDelayMs);
        }

        // No exponer JObject en la API pública para evitar fallos de importación del plugin
        public async Task<(bool ok, string error, string json)> TestConnectionAsync(string apiKey, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "API Key no configurada", null);
            var url = $"https://serpapi.com/search.json?engine=duckduckgo&q=test&no_cache=true&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}&api_key={Uri.EscapeDataString(apiKey)}";
            var (ok, err, jsonObj) = await GetAsync(url, ct);
            return (ok, err, jsonObj);
        }

        public async Task<(bool ok, string error, string json)> SearchAsync(string apiKey, string query, string kl, int page, CancellationToken ct = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "API Key no configurada", null);
            var sb = new StringBuilder("https://serpapi.com/search.json?engine=duckduckgo");
            sb.Append("&q=").Append(Uri.EscapeDataString(query ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(kl)) sb.Append("&kl=").Append(Uri.EscapeDataString(kl));
            // DuckDuckGo en SerpApi usa 'pageno' empezando en 1 para paginar
            if (page >= 0) sb.Append("&pageno=").Append(page + 1);
            sb.Append("&device=desktop");
            sb.Append("&no_cache=true");
            sb.Append("&t=").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append("&api_key=").Append(Uri.EscapeDataString(apiKey));
            var url = sb.ToString();
            var (ok, err, jsonObj) = await GetAsync(url, ct);
            return (ok, err, jsonObj);
        }

        public async Task<(bool ok, string error, string json)> SearchGoogleAsync(string apiKey, string query, string hl, string gl, int start, string googleDomain = null, int num = 100, string asSiteSearchHost = null, CancellationToken ct = default(CancellationToken), string asFiletype = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "API Key no configurada", null);
            var sb = new StringBuilder("https://serpapi.com/search.json?engine=google");
            sb.Append("&q=").Append(Uri.EscapeDataString(query ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(hl)) sb.Append("&hl=").Append(Uri.EscapeDataString(hl));
            if (!string.IsNullOrWhiteSpace(gl)) sb.Append("&gl=").Append(Uri.EscapeDataString(gl));
            if (!string.IsNullOrWhiteSpace(googleDomain)) sb.Append("&google_domain=").Append(Uri.EscapeDataString(googleDomain));
            if (num > 0) sb.Append("&num=").Append(num);
            if (start > 0) sb.Append("&start=").Append(start); // 0,10,20,...
            if (!string.IsNullOrWhiteSpace(asSiteSearchHost)) sb.Append("&as_sitesearch=").Append(Uri.EscapeDataString(asSiteSearchHost));
            if (!string.IsNullOrWhiteSpace(asFiletype)) sb.Append("&as_filetype=").Append(Uri.EscapeDataString(asFiletype));
            sb.Append("&filter=1");
            sb.Append("&safe=off");
            sb.Append("&no_cache=true");
            sb.Append("&t=").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append("&api_key=").Append(Uri.EscapeDataString(apiKey));
            var url = sb.ToString();
            var (ok, err, jsonObj) = await GetAsync(url, ct);
            return (ok, err, jsonObj);
        }

        // Devuelve string JSON para mantener la API pública libre de tipos externos
        private async Task<(bool ok, string error, string json)> GetAsync(string url, CancellationToken ct)
        {
            await RespectRateLimitAsync(ct);
            int attempt = 0;
            while (true)
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    // Evitar cachés intermedias
                    req.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true, NoStore = true };
                    req.Headers.Pragma.ParseAdd("no-cache");
                    var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                    var body = await res.Content.ReadAsStringAsync();
                    if (!res.IsSuccessStatusCode)
                    {
                        if (res.StatusCode == (HttpStatusCode)429)
                        {
                            // Backoff con Retry-After o exponencial
                            if (attempt >= _maxRetries429)
                                return (false, "Límite de peticiones superado (429). Espera unos segundos y vuelve a intentarlo.", null);

                            int delayMs = _baseRetryDelayMs * (int)Math.Pow(2, attempt);
                            try
                            {
                                if (res.Headers.RetryAfter != null)
                                {
                                    if (res.Headers.RetryAfter.Delta.HasValue)
                                        delayMs = (int)Math.Max(delayMs, res.Headers.RetryAfter.Delta.Value.TotalMilliseconds);
                                    else if (res.Headers.RetryAfter.Date.HasValue)
                                        delayMs = (int)Math.Max(delayMs, (res.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow).TotalMilliseconds);
                                }
                            }
                            catch { }
                            // Jitter
                            delayMs += new Random().Next(100, 400);
                            await Task.Delay(Math.Max(250, delayMs), ct);
                            attempt++;
                            continue;
                        }
                        if (res.StatusCode == HttpStatusCode.Unauthorized || res.StatusCode == HttpStatusCode.Forbidden)
                            return (false, "Acceso denegado por SerpApi (401/403). Verifica la API Key o el plan.", null);
                        return (false, $"Error HTTP {(int)res.StatusCode}: {res.ReasonPhrase}", null);
                    }
                    // No parseamos aquí para no requerir Newtonsoft.Json en firmas públicas
                    return (true, null, body);
                }
                catch (TaskCanceledException)
                {
                    return (false, "La solicitud a SerpApi expiró por timeout.", null);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    return (false, $"Error de red: {ex.Message}", null);
                }
            }
        }

        private async Task RespectRateLimitAsync(CancellationToken ct)
        {
            // Simple RPM limiter: ensure at least 60/rpm seconds between requests
            var minDelta = TimeSpan.FromSeconds(60.0 / _rpm);
            await _semaphore.WaitAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                var elapsed = now - _lastRequest;
                if (elapsed < minDelta)
                {
                    var wait = minDelta - elapsed;
                    if (wait > TimeSpan.Zero)
                    {
                        await Task.Delay(wait, ct);
                    }
                }
                _lastRequest = DateTime.UtcNow;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            try { _httpClient.Dispose(); } catch { }
            try { _semaphore.Dispose(); } catch { }
        }
    }
}


