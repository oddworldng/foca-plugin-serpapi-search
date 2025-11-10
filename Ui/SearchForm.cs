using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Foca.SerpApiSearch.Api;
using Foca.SerpApiSearch.Config;
using Foca.SerpApiSearch.Search;
using Newtonsoft.Json.Linq;
#pragma warning disable CS8019
using Foca.SerpApiSearch.Db;
#pragma warning restore CS8019
#if FOCA_API
using System.Reflection;
using PluginsAPI;
#endif

namespace Foca.SerpApiSearch.Ui
{
    public partial class SearchForm : Form
    {
        public bool Embedded { get; set; }
        private CancellationTokenSource _cts;
        private List<string> _results = new List<string>();

        public SearchForm()
        {
            InitializeComponent();
        }



        private void SearchForm_Load(object sender, EventArgs e)
        {
            // Ajustes para modo embebido en panel del host
            if (Embedded)
            {
                try
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.TopLevel = false;
                    this.CancelButton = null;
                }
                catch { }
            }
            try
            {
                // Asegurar botón dentro del header, anclado a la derecha y delante
                if (btnBuscar.Parent != panelHeader) btnBuscar.Parent = panelHeader;
                btnBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                btnBuscar.Top = 6;
                btnBuscar.Left = Math.Max(6, panelHeader.Width - btnBuscar.Width - 12);
                btnBuscar.BringToFront();
                // Reposicionar también en cambios de tamaño del header
                panelHeader.Resize -= PanelHeader_ResizeRepositionBuscar;
                panelHeader.Resize += PanelHeader_ResizeRepositionBuscar;
            }
            catch { }
            // Icono: Buscar (img/Search.png con fallback a img/search.png)
            try
            {
                var asm = typeof(SearchForm).Assembly;
                using (var stream = asm.GetManifestResourceStream("Foca.SerpApiSearch.img.Search.png") ??
                                   asm.GetManifestResourceStream("Foca.SerpApiSearch.img.search.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnBuscar.Image = sized;
                        }
                        btnBuscar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnBuscar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }
            string kl = ConfigurationManager.AppSettings["DefaultRegionKl"] ?? "es-es";
            txtKl.Text = kl;
            chkListExtensions.Items.Clear();
            var all = new[] { "pdf", "doc", "docx", "xls", "xlsx", "ppt" };
            foreach (var ext in all)
            {
                bool isDefaultChecked = string.Equals(ext, "pdf", StringComparison.OrdinalIgnoreCase);
                chkListExtensions.Items.Add(ext, isDefaultChecked);
            }
            // Selecciones por defecto del buscador
            try { cmbEngine.SelectedItem = "Google"; } catch { }
            try { cmbGoogleDomain.SelectedItem = "google.es"; } catch { }
            UpdateEngineUi();
            btnIncorporarExistente.Enabled = false;
            btnIncorporarNuevo.Enabled = false;
            btnExportar.Enabled = false;

            // Icono: Incorporar a proyecto (img/add_to_project.png)
            try
            {
                using (var stream = typeof(SearchForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.add_to_project.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnIncorporarExistente.Image = sized;
                        }
                        btnIncorporarExistente.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnIncorporarExistente.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }

            // Icono: Incorporar a nuevo proyecto (img/add_to_new_project.png)
            try
            {
                using (var stream = typeof(SearchForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.add_to_new_project.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnIncorporarNuevo.Image = sized;
                        }
                        btnIncorporarNuevo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnIncorporarNuevo.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }

            // Icono: Exportar CSV (img/export_csv.png)
            try
            {
                using (var stream = typeof(SearchForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.export_csv.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnExportar.Image = sized;
                        }
                        btnExportar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnExportar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }

            // Icono: Copiar consulta (img/copy.png)
            try
            {
                using (var stream = typeof(SearchForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.copy.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnCopy.Image = sized;
                        }
                        btnCopy.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnCopy.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }
        }

        private void PanelHeader_ResizeRepositionBuscar(object sender, EventArgs e)
        {
            try
            {
                btnBuscar.Left = Math.Max(6, panelHeader.Width - btnBuscar.Width - 12);
                btnBuscar.BringToFront();
            }
            catch { }
        }

        private void UpdateEngineUi()
        {
            try
            {
                bool isGoogle = string.Equals(cmbEngine.SelectedItem as string, "Google", StringComparison.OrdinalIgnoreCase);
                bool isBing = string.Equals(cmbEngine.SelectedItem as string, "Bing", StringComparison.OrdinalIgnoreCase);
                lblGoogleDomain.Visible = isGoogle;
                cmbGoogleDomain.Visible = isGoogle;
                // Mostrar controles de región (kl) para Google y Bing; ocultarlos en DuckDuckGo
                bool showKl = isGoogle || isBing;
                lblKl.Visible = showKl;
                txtKl.Visible = showKl;
                if (showKl)
                {
                    try { lblKl.Text = "Región (kl): lang-cc (ej. es-es)"; } catch { }
                }
                else
                {
                    try { lblKl.Text = "Región (kl):"; } catch { }
                }
            }
            catch { }
        }

        private void cmbEngine_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateEngineUi();
        }

        private async void btnBuscar_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await RunSearchAsync(_cts.Token);
        }

        private string ResolveApiKey()
        {
            var env = SerpApiSettings.ResolveApiKey();
            if (!string.IsNullOrWhiteSpace(env)) return env;
            var local = SerpApiConfigStore.Load()?.SerpApiKey;
            return local;
        }

        private async Task RunSearchAsync(CancellationToken ct)
        {
            btnBuscar.Enabled = false;
            lstResults.Items.Clear();
            _results.Clear();
            lblCount.Text = "0 resultados";

            try
            {
                var apiKey = ResolveApiKey();
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    MessageBox.Show("Configura la API Key de SerpApi primero.", "Búsqueda avanzada", MessageBoxButtons.OK, MessageBoxIcon.Information);
#if FOCA_API
                    return;
#else
                    return;
#endif
                }

                var domain = txtRootUrl.Text;
                var host = QueryBuilder.NormalizeToDomain(domain);
                var pathSegs = QueryBuilder.GetPathSegments(domain).Select(s => (s ?? string.Empty).ToLowerInvariant()).ToList();
                var selectedExts = chkListExtensions.CheckedItems.Cast<object>().Select(o => o.ToString()).ToArray();
                string engine = cmbEngine.SelectedItem as string ?? "DuckDuckGo";
                string query;
                if (string.Equals(engine, "Google", StringComparison.OrdinalIgnoreCase))
                {
                    query = QueryBuilder.BuildGoogle(domain, selectedExts);
                }
                else if (string.Equals(engine, "Bing", StringComparison.OrdinalIgnoreCase))
                {
                    query = QueryBuilder.BuildBing(domain, selectedExts);
                }
                else
                {
                    query = QueryBuilder.Build(domain, selectedExts);
                }
                // Mostrar la consulta exacta que enviamos al buscador vía SerpApi
                txtQueryPreview.Text = query;
                var kl = txtKl.Text?.Trim();

                // Precálculo de parámetros inmutables por búsqueda
                string asFiletype = (selectedExts != null && selectedExts.Length == 1) ? selectedExts[0] : null;
                string hl = null, gl = null, setlang = null, cc = null;
                var klNorm = (txtKl.Text ?? string.Empty).Trim();
                if (klNorm.Length >= 4 && klNorm.Contains("-"))
                {
                    var parts = klNorm.Split('-');
                    hl = setlang = parts[0].ToLowerInvariant();
                    gl = cc = parts[1].ToLowerInvariant();
                }
                var gDomain = (cmbGoogleDomain.SelectedItem as string) ?? "google.es";
                int maxResults = 0; // 0 = ilimitado
                int maxPages = 0;
                int delayMs = 0;
                int maxRequests = 0;
                bool applyBingDomainFilter = true;
#if FOCA_API
                var storedConfig = Foca.SerpApiSearch.Config.SerpApiConfigStore.Load();
                if (storedConfig != null)
                {
                    maxResults = storedConfig.MaxResults;
                    maxPages = storedConfig.MaxPagesPerSearch;
                    delayMs = storedConfig.DelayBetweenPagesMs;
                    maxRequests = storedConfig.MaxRequestsPerSearch;
                    applyBingDomainFilter = storedConfig.UseBingDomainFilter;
                }
#else
                try
                {
                    var cfg = Foca.SerpApiSearch.Config.SerpApiConfigStore.Load();
                    if (cfg != null)
                    {
                        maxResults = cfg.MaxResults;
                        maxPages = cfg.MaxPagesPerSearch;
                        delayMs = cfg.DelayBetweenPagesMs;
                        maxRequests = cfg.MaxRequestsPerSearch;
                        applyBingDomainFilter = cfg.UseBingDomainFilter;
                    }
                }
                catch { }
#endif

#if FOCA_API
                PluginLogger.ResetProgressCache();
                var keySource = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SERPAPI_API_KEY")) ? "SERPAPI_API_KEY" :
                    (storedConfig?.SerpApiKey != null ? "%APPDATA% config" : "Desconocido");
                string extensionsLabel = selectedExts.Length > 0 ? string.Join(", ", selectedExts) : "(todas)";
                string mkt = (!string.IsNullOrWhiteSpace(setlang) && !string.IsNullOrWhiteSpace(cc)) ? $"{setlang}-{cc.ToUpperInvariant()}" : "-";
                PluginLogger.Info("──────────── Inicio búsqueda SerpApi ────────────");
                PluginLogger.Info($"Motor seleccionado: {engine}");
                PluginLogger.Info($"Dominio introducido: {domain}");
                PluginLogger.Info($"Host normalizado: {host}");
                PluginLogger.Info($"Extensiones: {extensionsLabel}");
                PluginLogger.Info($"Región (kl): {(string.IsNullOrWhiteSpace(klNorm) ? "(vacía)" : klNorm)} | setlang={setlang ?? "-"} | cc={cc ?? "-"} | mkt={mkt}");
                PluginLogger.Info($"Google domain: {gDomain} | asFiletype: {asFiletype ?? "-"}");
                PluginLogger.Info($"API Key origen: {keySource}");
                PluginLogger.Info($"Límites configuración → MaxResults={maxResults} | MaxPages={maxPages} | MaxRequests={maxRequests} | DelayBetweenPagesMs={delayMs}");
                if (string.Equals(engine, "Bing", StringComparison.OrdinalIgnoreCase))
                {
                    PluginLogger.Info($"Bing filters=domain habilitado: {(applyBingDomainFilter ? "Sí" : "No")}");
                }
                PluginLogger.Info($"Consulta enviada: {query}");
#endif

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Feedback visible antes de la primera petición
                if (string.Equals(engine, "Bing", StringComparison.OrdinalIgnoreCase))
                {
                    lblCount.Text = "Conectando con Bing...";
                    await Task.Yield();
                }

                using (var client = new SerpApiClient())
                {
                    int requests = 0;
                    for (int page = 0; (maxResults == 0 || _results.Count < maxResults) && (maxPages == 0 || page < maxPages); page++)
                    {
                        ct.ThrowIfCancellationRequested();
                        (bool ok, string error, string json) res;
                        if (engine == "Google")
                        {
                            // Google pagina con start=0,10,20...
                            int start = page * 10;
                            res = await client.SearchGoogleAsync(apiKey, query, hl, gl, start, gDomain, 100, host, ct, asFiletype);
                        }
                        else if (engine == "Bing")
                        {
                            // Bing usa setlang/cc y paginación con 'first'
                            int first = page * 10;
                            // Primera página rápida: count=5 y, si no se indicó kl, omitir setlang/cc
                            int count = (page == 0 ? 5 : 10);
                            string sl = setlang, country = cc;
                            if (page == 0 && string.IsNullOrWhiteSpace(txtKl.Text))
                            {
                                sl = null;
                                country = null;
                            }
#if FOCA_API
                            PluginLogger.Info($"Bing: solicitando página {page + 1} (first={first}, count={count})");
#endif
                            res = await client.SearchBingAsync(apiKey, query, sl, country, first, host, applyBingDomainFilter, ct, count, UpdateBingStatus);
                        }
                        else
                        {
#if FOCA_API
                            PluginLogger.Info($"{engine}: solicitando página {page + 1}");
#endif
                            res = await client.SearchAsync(apiKey, query, kl, page, ct);
                        }
                        var ok = res.ok; var error = res.error; var json = res.json;
                        System.Diagnostics.Debug.WriteLine($"[SerpApi] engine={engine} page={page} ok={ok} req={requests}");
                        requests++;
                        if (maxRequests > 0 && requests >= maxRequests) break;
                        if (!ok)
                        {
                            MessageBox.Show(error ?? "Error de búsqueda", "Búsqueda avanzada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
#if FOCA_API
                            PluginLogger.Error($"{engine}: error en página {page + 1}: {error ?? "Error de búsqueda"}");
#endif
                            break;
                        }
                        var swPage = System.Diagnostics.Stopwatch.StartNew();
                        var tuple = ResultMapper.ExtractLinksAndHasNext(json);
                        var pageLinks = (tuple.links ?? Enumerable.Empty<string>()).ToList();
                        bool isLastPage = !tuple.hasNext;
                        if (pageLinks.Count == 0) break; // la API ya no devuelve más resultados

                        // Filtrar solo por host exacto. La ruta (inurl:...) se usa para sesgar la búsqueda
                        // pero no forzamos coincidencia estricta porque DuckDuckGo puede ignorar algunos inurl.
                        IEnumerable<string> filteredLinks;
                        int skippedDomain = 0;
                        int skippedPath = 0;

                        if (string.Equals(engine, "Bing", StringComparison.OrdinalIgnoreCase) && !applyBingDomainFilter)
                        {
                            filteredLinks = pageLinks;
                        }
                        else
                        {
                            filteredLinks = pageLinks
                                .Where(u =>
                                {
                                    var passDomain = QueryBuilder.IsUrlInDomain(u, domain);
                                    if (!passDomain) skippedDomain++;
                                    return passDomain;
                                })
                                .ToList();
                        }

                        var links = filteredLinks
                            .Where(u =>
                            {
                                var passPath = QueryBuilder.UrlPathContainsSegments(u, pathSegs, true);
                                if (!passPath) skippedPath++;
                                return passPath;
                            })
                            .ToList();

                        int addedThisPage = 0;
                        foreach (var link in links)
                        {
#if FOCA_API
                            string hostInfo = null;
                            bool inDomain = false;
                            bool pathOk = false;
                            try
                            {
                                var uri = new Uri(link);
                                hostInfo = uri.Host;
                                inDomain = QueryBuilder.IsUrlInDomain(link, domain);
                                pathOk = QueryBuilder.UrlPathContainsSegments(link, pathSegs, true);
                            }
                            catch
                            {
                                hostInfo = "(invalid uri)";
                            }
                            PluginLogger.Debug($"Filtro enlace: {link}");
                            PluginLogger.Debug($"    host={hostInfo} inDomain={inDomain} pathOk={pathOk}");
#endif
                            // Filtro por extensiones seleccionadas (si hay)
                            bool allowedExt = (selectedExts == null || selectedExts.Length == 0);
                            if (!allowedExt)
                            {
                                try
                                {
                                    var path = new Uri(link).AbsolutePath.ToLowerInvariant();
                                    foreach (var ext in selectedExts)
                                    {
                                        var e = (ext ?? string.Empty).ToLowerInvariant();
                                        if (!string.IsNullOrEmpty(e) && path.EndsWith("." + e))
                                        {
                                            allowedExt = true;
                                            break;
                                        }
                                    }
                                }
                                catch
                                {
                                    allowedExt = false;
                                }
                            }
                            if (!allowedExt)
                            {
#if FOCA_API
                                PluginLogger.Debug($"Descartado por extensión: {link}");
#endif
                                continue;
                            }
                            if (seen.Add(link))
                            {
                                _results.Add(link);
                                lstResults.Items.Add(link);
                                addedThisPage++;
                                if (_results.Count >= maxResults) break;
                            }
                        }
                        lblCount.Text = _results.Count + " resultados";
                        // Ceder al UI thread para refrescar inmediatamente
                        await Task.Yield();
                        swPage.Stop();
                        System.Diagnostics.Debug.WriteLine($"[SerpApi] added={addedThisPage} total={_results.Count} last={isLastPage} pageMs={swPage.ElapsedMilliseconds}");
#if FOCA_API
                        PluginLogger.Debug($"{engine}: página {page + 1} → enlaces={pageLinks.Count}, añadidos={addedThisPage}, total={_results.Count}, última={isLastPage}");
                        PluginLogger.Debug($"{engine}: descartados dominio={skippedDomain}, descartados ruta={skippedPath}");
#endif
                        // No asumimos tamaño de página fijo; continuamos hasta quedarnos sin resultados
                        if (addedThisPage == 0) break; // corta si no añade nada nuevo (evita bucles)
                        if (isLastPage) break; // no hay siguiente página anunciado por SerpAPI
                        if (delayMs > 0) await Task.Delay(delayMs, ct);
                    }
                }

                // Resumen final alineado con Excel plugin (mensaje claro)
                lblCount.Text = $"Se han encontrado {_results.Count} resultados";
                ShowDbImportButtons();
#if FOCA_API
                PluginLogger.Info($"Fin búsqueda {engine}. Resultados totales: {_results.Count}");
                PluginLogger.Info("────────────────────────────────────────────────");
#endif
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error durante la búsqueda: {ex.Message}", "Búsqueda avanzada", MessageBoxButtons.OK, MessageBoxIcon.Error);
#if FOCA_API
                PluginLogger.Error($"Excepción en RunSearchAsync: {ex}");
#endif
            }
            finally
            {
                btnBuscar.Enabled = true;
            }
        }

        private void ShowOnlyIncorporateCurrentProjectButton()
        {
            try
            {
                btnIncorporarExistente.Text = "Incorporar a proyecto actual";
                btnIncorporarExistente.Visible = true;
                btnIncorporarExistente.Enabled = _results.Count > 0;

                btnIncorporarNuevo.Visible = false;
                btnExportar.Visible = false;
            }
            catch { }
        }

        private void ShowDbImportButtons()
        {
            try
            {
                btnIncorporarExistente.Text = "Insertar a un proyecto existente";
                btnIncorporarExistente.Visible = true;
                btnIncorporarExistente.Enabled = _results.Count > 0;

                btnIncorporarNuevo.Text = "Insertar a un proyecto nuevo";
                btnIncorporarNuevo.Visible = true;
                btnIncorporarNuevo.Enabled = _results.Count > 0;

                btnExportar.Visible = true;
            }
            catch { }
        }

        private void FinalizeAfterImportUI()
        {
            try
            {
                btnIncorporarExistente.Enabled = false;
                btnIncorporarExistente.Visible = false;
                btnIncorporarNuevo.Visible = false;
                btnExportar.Visible = false;
            }
            catch { }
        }

        private async Task ImportUrlsToCurrentProjectAsync()
        {
            // Ceder el hilo para evitar advertencia CS1998 y mantener firma async para llamadas await en handlers
            await Task.Yield();
            if (_results == null || _results.Count == 0)
            {
                MessageBox.Show("No hay resultados que incorporar.", "Incorporar a proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
#if FOCA_API
            int sent = 0;
            try
            {
                foreach (var url in _results.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Import.ImportEventCaller(new ImportObject(Import.Operation.AddUrl, new PluginsAPI.ImportElements.URL(url)));
                    sent++;
                }
                MessageBox.Show($"Enviadas {sent} URL(s) al proyecto actual.", "Incorporar a proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                FinalizeAfterImportUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo incorporar al proyecto actual: {ex.Message}", "Incorporar a proyecto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
#else
            MessageBox.Show("Esta acción requiere FOCA (PluginsAPI). Ejecútalo dentro de FOCA.", "Incorporar a proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif
        }

        private async void btnIncorporarExistente_Click(object sender, EventArgs e)
        {
            await InsertUrlsToExistingProjectViaDbAsync();
        }

        // Compatibilidad: el diseñador puede seguir apuntando a este manejador
        private async void btnIncorporarNuevo_Click(object sender, EventArgs e)
        {
            await InsertUrlsToNewProjectViaDbAsync();
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (_results.Count == 0) return;
            using (var sfd = new SaveFileDialog())
            {
                var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                sfd.Filter = "CSV (*.csv)|*.csv|Todos los archivos (*.*)|*.*";
                sfd.FileName = $"{stamp}_SerpApiSearch_results.csv";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        System.IO.File.WriteAllLines(sfd.FileName, _results);
                        MessageBox.Show("Exportación completada.", "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"No se pudo exportar: {ex.Message}", "Exportar CSV", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void txtQueryPreview_DoubleClick(object sender, EventArgs e)
        {
            try { Clipboard.SetText(txtQueryPreview.Text ?? string.Empty); } catch { }
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            try { Clipboard.SetText(txtQueryPreview.Text ?? string.Empty); } catch { }
        }

        private void FinalizeFlowUI()
        {
            try
            {
                btnBuscar.Enabled = false;
                btnIncorporarExistente.Enabled = false;
                btnIncorporarNuevo.Enabled = false;
                btnExportar.Enabled = false;
            }
            catch { }
        }



        private void UpdateBingStatus(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action<string>(UpdateBingStatus), message); } catch { }
                return;
            }
            lblCount.Text = message;
#if FOCA_API
            PluginLogger.Progress(message);
#endif
        }

        private async Task InsertUrlsToNewProjectViaDbAsync()
        {
            if (_results == null || _results.Count == 0)
            {
                MessageBox.Show("No hay resultados que insertar.", "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new NewProjectForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    var inserter = new DbInserter();
                    var domain = QueryBuilder.NormalizeToDomain(txtRootUrl.Text);
                    var newProjectId = await inserter.CreateProjectAsync(dlg.ProjectName, null, domain, dlg.FolderPath);
                    var (inserted, duplicates) = await inserter.InsertUrlsAsync(newProjectId, _results.ToArray());

                    MessageBox.Show(
                        $"Proyecto creado (Id={newProjectId}). Insertadas {inserted} URL(s). Duplicadas: {duplicates}.",
                        "Nuevo proyecto",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Recarga silenciosa en FOCA
#if FOCA_API
                    try
                    {
                        FocaInterop.TryLoadProjectById(newProjectId);
                        FocaInterop.TryRefreshProjectsList();
                        // Encolar descargas para fijar tamaño y dejar lista la extracción
                        FocaInterop.TryEnqueueDownloadsForUrls(_results);
                        FocaInterop.TryFinalizeUrls(_results);
                        // Reparar 0 KB si persisten tras descarga
                        FocaInterop.FixZeroSizeDownloadsAsync(_results, 3, 3000);
                    }
                    catch { }
#endif

                    FinalizeAfterImportUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo crear el proyecto o insertar las URLs: {ex.Message}", "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async Task InsertUrlsToExistingProjectViaDbAsync()
        {
            if (_results == null || _results.Count == 0)
            {
                MessageBox.Show("No hay resultados que insertar.", "Proyecto existente", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new SelectProjectForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var project = dlg.SelectedProject;
                if (project == null)
                {
                    MessageBox.Show("Debes seleccionar un proyecto.", "Proyecto existente", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                try
                {
                    var inserter = new DbInserter();
                    var (inserted, duplicates) = await inserter.InsertUrlsAsync(project.Id, _results.ToArray());

                    MessageBox.Show(
                        $"Insertadas {inserted} URL(s) en '{project.Name}' (Id={project.Id}). Duplicadas: {duplicates}.",
                        "Proyecto existente",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    // Recarga silenciosa en FOCA
#if FOCA_API
                    try
                    {
                        FocaInterop.TryReloadCurrentProject();
                        FocaInterop.TryRefreshProjectsList();
                        // Encolar descargas para fijar tamaño y dejar lista la extracción
                        FocaInterop.TryEnqueueDownloadsForUrls(_results);
                        FocaInterop.TryFinalizeUrls(_results);
                        // Reparar 0 KB si persisten tras descarga
                        FocaInterop.FixZeroSizeDownloadsAsync(_results, 3, 3000);
                    }
                    catch { }
#endif

                    FinalizeAfterImportUI();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo insertar en el proyecto: {ex.Message}", "Proyecto existente", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static List<Uri> BuildUriList(IEnumerable<string> urls)
        {
            var data = new List<Uri>();

            if (urls == null)
            {
                return data;
            }

            foreach (var url in urls)
            {
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    data.Add(uri);
                }
            }

            return data;
        }
    }
}


