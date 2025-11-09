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
using Foca.SerpApiSearch;

namespace Foca.SerpApiSearch.Api
{
    /// <summary>
    /// Minimal SerpApi client for DuckDuckGo engine.
    /// Handles rate limiting (RPM) and basic error handling.
    /// </summary>
    public class SerpApiClient : IDisposable
    {
        private static readonly HttpClient _sharedHttpClient = CreateClient();
        private readonly HttpClient _httpClient;
        private readonly int _rpm;
        private readonly TimeSpan _timeout;
        private readonly int _maxRetries429;
        private readonly int _baseRetryDelayMs;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastRequest = DateTime.MinValue;

        private static HttpClient CreateClient()
        {
            var handler = new HttpClientHandler();
            try { handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate; } catch { }
            var c = new HttpClient(handler);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("FOCA-SerpApiSearch/1.1");
            return c;
        }

        public SerpApiClient()
        {
            _httpClient = _sharedHttpClient;

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
            try { _httpClient.Timeout = _timeout; } catch { }
            // Asegurar un timeout efectivo mínimo de 90s para evitar expiraciones al usar Bing async
            try
            {
                var minEffective = TimeSpan.FromSeconds(90);
                if (_httpClient.Timeout < minEffective) _httpClient.Timeout = minEffective;
            }
            catch { }
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

        public async Task<(bool ok, string error, string json)> SearchBingAsync(string apiKey, string query, string setlang, string cc, int first, string domainHost, bool applyDomainFilter, CancellationToken ct = default(CancellationToken), int count = 0, Action<string> progress = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return (false, "API Key no configurada", null);

            bool enableDebug = Debugger.IsAttached;
            try { enableDebug = enableDebug || bool.Parse(ConfigurationManager.AppSettings["SerpApiDebugLogging"] ?? "false"); } catch { }
#if FOCA_API
            Action<string> log = msg =>
            {
                if (string.IsNullOrWhiteSpace(msg)) return;
                PluginLogger.Debug(msg);
                if (!enableDebug) return;
                try { Debug.WriteLine($"[SerpApi][Bing] {msg}"); } catch { }
            };
#else
            Action<string> log = msg =>
            {
                if (!enableDebug || string.IsNullOrWhiteSpace(msg)) return;
                try { Debug.WriteLine($"[SerpApi][Bing] {msg}"); } catch { }
            };
#endif

            progress?.Invoke("Bing: enviando búsqueda inicial…");

            // Construcción de la URL (Bing)
            var sb = new StringBuilder("https://serpapi.com/search.json?engine=bing");
            sb.Append("&q=").Append(Uri.EscapeDataString(query ?? string.Empty));
            if (!string.IsNullOrWhiteSpace(setlang)) sb.Append("&setlang=").Append(Uri.EscapeDataString(setlang)); // ej. es
            if (!string.IsNullOrWhiteSpace(cc)) sb.Append("&cc=").Append(Uri.EscapeDataString(cc));               // ej. es
            if (!string.IsNullOrWhiteSpace(setlang) && !string.IsNullOrWhiteSpace(cc))
            {
                // mkt recomendado por SerpApi para Bing
                var mkt = $"{setlang}-{cc.ToUpperInvariant()}";
                sb.Append("&mkt=").Append(Uri.EscapeDataString(mkt));
            }
            sb.Append("&first=").Append(Math.Max(0, first)); // 0,10,20,... (enviar siempre)
            if (count > 0) sb.Append("&count=").Append(count);
            sb.Append("&safe_search=Off");
            sb.Append("&device=desktop");
            if (applyDomainFilter && !string.IsNullOrWhiteSpace(domainHost))
            {
                var cleanHost = domainHost.Trim();
                if (!string.IsNullOrWhiteSpace(cleanHost))
                {
                    sb.Append("&filters=").Append(Uri.EscapeDataString($"domain:{cleanHost}"));
                    log($"Filtro domain aplicado: {cleanHost}");
                }
            }
            else if (!applyDomainFilter)
            {
                log("Filtro domain deshabilitado para Bing.");
            }
            sb.Append("&async=true");     // modo asíncrono para evitar timeouts
            sb.Append("&no_cache=false"); // permitir caché para acelerar
            sb.Append("&t=").Append(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            sb.Append("&api_key=").Append(Uri.EscapeDataString(apiKey));
            var url = sb.ToString();
            log($"URL => {url}");
            var overallStart = DateTime.UtcNow;

            // Primera petición
            var (ok, err, body) = await GetAsync(url, ct);
            if (!ok)
            {
                var message = err ?? "Error desconocido al contactar con SerpApi.";
                if (message.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                    message = "Timeout HTTP al contactar con SerpApi (Bing). La respuesta inicial no llegó a completarse.";
                progress?.Invoke("Bing: fallo al contactar con SerpApi.");
                log($"Primer intento falló: {message}");
#if FOCA_API
                PluginLogger.Error($"Bing: fallo al contactar con SerpApi. {message}");
#endif
                return (false, message, null);
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                const string message = "SerpApi devolvió una respuesta vacía.";
                progress?.Invoke("Bing: respuesta vacía de SerpApi.");
                log(message);
#if FOCA_API
                PluginLogger.Error("Bing: respuesta vacía de SerpApi.");
#endif
                return (false, message, null);
            }

            try
            {
                var jobjInitial = JObject.Parse(body);
                return await HandleBingResponseAsync(jobjInitial, body, apiKey, progress, log, overallStart, ct);
            }
            catch (Exception ex)
            {
                var message = $"Respuesta de SerpApi no es JSON válido: {ex.Message}";
                progress?.Invoke("Bing: JSON inválido recibido.");
                log(message);
#if FOCA_API
                PluginLogger.Error($"Bing: JSON inválido recibido. {ex.Message}");
#endif
                return (false, message, null);
            }
        }

        private async Task<(bool ok, string error, string json)> HandleBingResponseAsync(JObject jobj, string rawBody, string apiKey, Action<string> progress, Action<string> log, DateTime overallStart, CancellationToken ct)
        {
            var metadata = jobj["search_metadata"] as JObject;
            var requestId = metadata?["id"]?.ToString();
            var status = metadata?["status"]?.ToString();
            var statusDetail = metadata?["status_detail"]?.ToString();
            var endpoint = metadata?["json_endpoint"]?.ToString();
            var explicitError = jobj["error"]?.ToString() ?? jobj["serpapi_error"]?.ToString();
            var captcha = jobj["captcha_result"]?.ToString();
            var organic = jobj["organic_results"] as JArray;
            var organicCount = organic?.Count ?? 0;

            log($"Estado recibido: status={status ?? "<null>"} detail={statusDetail ?? "<null>"} organic={organicCount} id={requestId ?? "<null>"} endpoint={endpoint ?? "<null>"}");

            if (!string.IsNullOrWhiteSpace(explicitError))
            {
                var message = $"SerpApi devolvió error: {explicitError}";
                progress?.Invoke("Bing: error devuelto por SerpApi.");
                log(message);
#if FOCA_API
                PluginLogger.Error($"Bing: error devuelto por SerpApi. {explicitError}");
#endif
                return (false, message, null);
            }

            if (!string.IsNullOrWhiteSpace(captcha) && !string.Equals(captcha, "Success", StringComparison.OrdinalIgnoreCase))
            {
                var message = $"SerpApi requiere captcha (resultado: {captcha}).";
                progress?.Invoke("Bing: captcha requerido por SerpApi.");
                log(message);
#if FOCA_API
                PluginLogger.Error($"Bing: captcha requerido por SerpApi (resultado {captcha}).");
#endif
                return (false, message, null);
            }

            if (string.Equals(status, "Error", StringComparison.OrdinalIgnoreCase))
            {
                var detail = !string.IsNullOrWhiteSpace(statusDetail) ? $" Detalle: {statusDetail}" : string.Empty;
                var message = $"SerpApi reporta estado de error.{detail}";
                progress?.Invoke("Bing: estado de error devuelto.");
                log(message);
#if FOCA_API
                PluginLogger.Error($"Bing: estado de error devuelto por SerpApi.{detail}");
#endif
                return (false, message, null);
            }

            if (organicCount > 0 ||
                string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Cached", StringComparison.OrdinalIgnoreCase))
            {
                progress?.Invoke("Bing: resultados listos.");
                log($"Resultados listos (organic={organicCount}, status={status}).");
#if FOCA_API
                PluginLogger.Success($"Bing: resultados listos (status={status ?? "null"}, organic={organicCount}).");
#endif
                return (true, null, rawBody);
            }

            var endpointUrl = ResolveBingEndpoint(endpoint, requestId, apiKey);
            if (string.IsNullOrWhiteSpace(endpointUrl))
            {
                var message = $"No se pudo resolver el endpoint de resultados (async). Id={requestId ?? "N/D"}";
                progress?.Invoke("Bing: no se obtuvo json_endpoint.");
                log(message);
#if FOCA_API
                PluginLogger.Error(message);
#endif
                return (false, message, null);
            }

            progress?.Invoke($"Bing: {status ?? "Procesando"}…");

            int pollIntervalMs = 1500;
            int maxWaitMs = 45000;
            try { pollIntervalMs = Math.Max(500, int.Parse(ConfigurationManager.AppSettings["SerpApiBingAsyncPollingIntervalMs"] ?? "1500")); } catch { }
            try { maxWaitMs = Math.Max(5000, int.Parse(ConfigurationManager.AppSettings["SerpApiBingAsyncMaxWaitMs"] ?? "45000")); } catch { }

            var started = DateTime.UtcNow;
            while ((DateTime.UtcNow - started).TotalMilliseconds < maxWaitMs)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Delay(pollIntervalMs, ct);

                var elapsed = DateTime.UtcNow - overallStart;
                var remaining = TimeSpan.FromMilliseconds(Math.Max(0, maxWaitMs - (DateTime.UtcNow - started).TotalMilliseconds));
                progress?.Invoke($"Bing: sondeando ({elapsed.TotalSeconds:0}s, resta {remaining.TotalSeconds:0}s)…");
                log($"Sondeo endpoint {endpointUrl} (elapsed {elapsed.TotalSeconds:0}s)");

                var (ok, err, body) = await GetAsync(endpointUrl, ct);
                if (!ok)
                {
                    var message = err ?? "Error desconocido durante el sondeo async de Bing.";
                    log($"Sondeo falló: {message}");
                    if (!string.IsNullOrWhiteSpace(err) && err.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0)
                        progress?.Invoke("Bing: timeout HTTP durante el sondeo. Reintentando…");
#if FOCA_API
                    PluginLogger.Debug($"Bing: sondeo falló. {message}");
#endif
                    continue;
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    log("Sondeo devolvió cuerpo vacío; continúo esperando.");
#if FOCA_API
                    PluginLogger.Debug("Bing: sondeo devolvió cuerpo vacío; se reintenta.");
#endif
                    continue;
                }

                JObject jobjPoll;
                try
                {
                    jobjPoll = JObject.Parse(body);
                }
                catch (Exception ex)
                {
                    log($"JSON inválido durante sondeo: {ex.Message}");
#if FOCA_API
                    PluginLogger.Debug($"Bing: JSON inválido durante sondeo. {ex.Message}");
#endif
                    continue;
                }

                var metaPoll = jobjPoll["search_metadata"] as JObject;
                var pollStatus = metaPoll?["status"]?.ToString();
                var pollDetail = metaPoll?["status_detail"]?.ToString();
                var pollEndpoint = metaPoll?["json_endpoint"]?.ToString();
                var pollError = jobjPoll["error"]?.ToString() ?? jobjPoll["serpapi_error"]?.ToString();
                var pollCaptcha = jobjPoll["captcha_result"]?.ToString();
                var pollOrganic = jobjPoll["organic_results"] as JArray;
                var pollOrganicCount = pollOrganic?.Count ?? 0;

                log($"Sondeo estado={pollStatus ?? "<null>"} detail={pollDetail ?? "<null>"} organic={pollOrganicCount}");

                // Archivo temprano: si llevamos >20s y hay id, intentamos recuperar el archivo directamente
                if ((DateTime.UtcNow - overallStart).TotalSeconds > 20 && !string.IsNullOrWhiteSpace(requestId))
                {
                    var earlyUrl = $"https://serpapi.com/searches/{requestId}.json?api_key={Uri.EscapeDataString(apiKey)}";
                    var (okEarly, errEarly, bodyEarly) = await GetAsync(earlyUrl, ct);
                    if (okEarly && !string.IsNullOrWhiteSpace(bodyEarly))
                    {
                        try
                        {
                            var early = JObject.Parse(bodyEarly);
                            var earlyStatus = early.SelectToken("search_metadata.status")?.ToString();
                            var earlyOrganic = early["organic_results"] as JArray;
                            if ((earlyOrganic?.Count ?? 0) > 0 ||
                                string.Equals(earlyStatus, "Success", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(earlyStatus, "Cached", StringComparison.OrdinalIgnoreCase))
                            {
                                progress?.Invoke("Bing: resultados listos (archivo).");
                                log($"Resultados obtenidos por archivo temprano (organic={earlyOrganic?.Count ?? 0}, status={earlyStatus}).");
#if FOCA_API
                                PluginLogger.Success($"Bing: resultados por archivo temprano (status={earlyStatus ?? "null"}, organic={earlyOrganic?.Count ?? 0}).");
#endif
                                return (true, null, bodyEarly);
                            }
                        }
                        catch
                        {
                            // ignorar y continuar sondeando
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(pollError))
                {
                    var message = $"SerpApi devolvió error durante el sondeo: {pollError}";
                    progress?.Invoke("Bing: error devuelto por SerpApi durante el sondeo.");
                    log(message);
#if FOCA_API
                    PluginLogger.Error($"Bing: error durante sondeo. {pollError}");
#endif
                    return (false, message, null);
                }

                if (!string.IsNullOrWhiteSpace(pollCaptcha) && !string.Equals(pollCaptcha, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    var message = $"SerpApi requiere captcha durante el sondeo (resultado: {pollCaptcha}).";
                    progress?.Invoke("Bing: captcha requerido por SerpApi.");
                    log(message);
#if FOCA_API
                    PluginLogger.Error($"Bing: captcha requerido durante sondeo (resultado {pollCaptcha}).");
#endif
                    return (false, message, null);
                }

                if (pollOrganicCount > 0 ||
                    string.Equals(pollStatus, "Success", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(pollStatus, "Cached", StringComparison.OrdinalIgnoreCase))
                {
                    progress?.Invoke("Bing: resultados listos.");
                    log($"Resultados listos tras sondeo (organic={pollOrganicCount}, status={pollStatus}).");
#if FOCA_API
                    PluginLogger.Success($"Bing: resultados listos tras sondeo (status={pollStatus ?? "null"}, organic={pollOrganicCount}).");
#endif
                    return (true, null, body);
                }

                if (string.Equals(pollStatus, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    var detail = !string.IsNullOrWhiteSpace(pollDetail) ? $" Detalle: {pollDetail}" : string.Empty;
                    var message = $"SerpApi reporta estado de error durante sondeo.{detail}";
                    progress?.Invoke("Bing: estado de error devuelto.");
                    log(message);
#if FOCA_API
                    PluginLogger.Error($"Bing: estado de error durante sondeo.{detail}");
#endif
                    return (false, message, null);
                }

                if (!string.IsNullOrWhiteSpace(pollEndpoint) && !pollEndpoint.Equals(endpointUrl, StringComparison.OrdinalIgnoreCase))
                {
                    var updatedEndpoint = ResolveBingEndpoint(pollEndpoint, requestId, apiKey);
                    if (!string.IsNullOrWhiteSpace(updatedEndpoint))
                    {
                        endpointUrl = updatedEndpoint;
                        log($"Actualizado endpoint a {endpointUrl}");
#if FOCA_API
                        PluginLogger.Debug($"Bing: endpoint actualizado a {endpointUrl}");
#endif
                    }
                }

                progress?.Invoke($"Bing: {pollStatus ?? "Procesando"} ({elapsed.TotalSeconds:0}s)…");
            }

            var totalSeconds = (DateTime.UtcNow - overallStart).TotalSeconds;
            var timeoutMessage = $"Timeout asincrónico esperando resultados de Bing tras {totalSeconds:0} segundos (id={requestId ?? "N/D"}).";
            progress?.Invoke("Bing: timeout asincrónico esperando resultados.");
            log(timeoutMessage);
#if FOCA_API
            PluginLogger.Error(timeoutMessage);
#endif

            // Intento final: recuperar el archivo de búsqueda si existe
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                try
                {
                    var archiveUrl = $"https://serpapi.com/searches/{requestId}.json?api_key={Uri.EscapeDataString(apiKey)}";
                    progress?.Invoke("Bing: recuperando archivo de búsqueda final…");
                    log($"Intento final con archivo: {archiveUrl}");
                    var (okArchive, errArchive, bodyArchive) = await GetAsync(archiveUrl, ct);
                    if (okArchive && !string.IsNullOrWhiteSpace(bodyArchive))
                    {
                        var archiveObj = JObject.Parse(bodyArchive);
                        var finalStatus = archiveObj.SelectToken("search_metadata.status")?.ToString();
                        var finalDetail = archiveObj.SelectToken("search_metadata.status_detail")?.ToString();
                        var archiveOrganic = archiveObj["organic_results"] as JArray;
                        log($"Archivo final status={finalStatus ?? "null"} detail={finalDetail ?? "null"} organic={archiveOrganic?.Count ?? 0}");
#if FOCA_API
                        PluginLogger.Debug($"Bing: archivo final status={finalStatus ?? "null"} detail={finalDetail ?? "null"} organic={archiveOrganic?.Count ?? 0}");
#endif
                        if (archiveOrganic != null && archiveOrganic.Count > 0)
                        {
                            progress?.Invoke("Bing: resultados listos (archivo).");
                            log("Resultados obtenidos desde archivo de búsqueda final.");
#if FOCA_API
                            PluginLogger.Success("Bing: resultados obtenidos desde archivo de búsqueda final.");
#endif
                            return (true, null, bodyArchive);
                        }
                        bool logFallback = Debugger.IsAttached;
                        try { logFallback = logFallback || bool.Parse(ConfigurationManager.AppSettings["SerpApiDebugLogging"] ?? "false"); } catch { }
                        if (logFallback)
                        {
                            var snippet = bodyArchive.Length > 2000 ? bodyArchive.Substring(0, 2000) + "…[truncado]" : bodyArchive;
                            log($"JSON fallback (truncado): {snippet}");
#if FOCA_API
                            PluginLogger.Debug($"Bing: JSON fallback => {snippet}");
#endif
                        }
                        log("Archivo final recuperado pero sin resultados orgánicos.");
#if FOCA_API
                        PluginLogger.Debug("Bing: archivo final recuperado pero sin orgánicos.");
#endif
                    }
                    else if (!okArchive)
                    {
                        log($"Archivo final no disponible: {errArchive}");
#if FOCA_API
                        PluginLogger.Debug($"Bing: archivo final no disponible. {errArchive}");
#endif
                    }
                }
                catch (Exception ex)
                {
                    log($"Error recuperando archivo final: {ex.Message}");
#if FOCA_API
                    PluginLogger.Debug($"Bing: error recuperando archivo final. {ex.Message}");
#endif
                }
            }

            return (false, timeoutMessage, null);
        }

        private static string ResolveBingEndpoint(string endpoint, string requestId, string apiKey)
        {
            if (string.IsNullOrWhiteSpace(endpoint) && !string.IsNullOrWhiteSpace(requestId))
                endpoint = $"https://serpapi.com/searches/{requestId}.json";
            if (string.IsNullOrWhiteSpace(endpoint)) return null;
            return endpoint + (endpoint.Contains("?") ? "&" : "?") + "api_key=" + Uri.EscapeDataString(apiKey);
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
                    // Forzar HTTP/1.1 para evitar problemas con proxys/IDS que degradan HTTP/2
                    req.Version = System.Net.HttpVersion.Version11;
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
                        // Reintentos básicos para 5xx
                        if ((int)res.StatusCode >= 500 && (int)res.StatusCode < 600)
                        {
                            if (attempt >= _maxRetries429)
                                return (false, $"Error HTTP {(int)res.StatusCode}: {res.ReasonPhrase}", null);
                            int delayMs = _baseRetryDelayMs * (int)Math.Pow(2, attempt) + new Random().Next(100, 400);
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
                    attempt++;
                    if (attempt <= _maxRetries429)
                    {
                        int delayMs = _baseRetryDelayMs * (int)Math.Pow(2, attempt) + new Random().Next(100, 400);
                        await Task.Delay(Math.Max(250, delayMs), ct);
                        continue;
                    }
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
            try { _semaphore.Dispose(); } catch { }
        }
    }
}


