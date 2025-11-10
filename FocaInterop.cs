using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Foca.SerpApiSearch
{
#if FOCA_API
    internal static class FocaInterop
    {
        private static readonly object SyncRoot = new object();
        private static object _panelMetadataSearch;
        private static Type _panelMetadataSearchType;
        private static MethodInfo _handleLinkFoundMethod;
        private static MethodInfo _extractAllMetadataMethod;
        private static Type _eventsThreadsType;
        private static Type _collectionFoundGenericType;

        internal static bool IsAvailable
        {
            get
            {
                return TryEnsurePanel(out _);
            }
        }

        internal static bool TryHandleLinkFound(IEnumerable<Uri> uris, object sender = null)
        {
            if (!TryEnsurePanel(out object panel))
                return false;

            object eventArgs = CreateCollectionFoundEvent(uris ?? Enumerable.Empty<Uri>());
            if (eventArgs == null)
                return false;

            try
            {
                _handleLinkFoundMethod.Invoke(panel, new[] { sender, eventArgs });
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryExtractAllMetadata()
        {
            if (!TryEnsurePanel(out object panel))
                return false;

            if (_extractAllMetadataMethod == null)
            {
                _extractAllMetadataMethod = _panelMetadataSearchType?
                    .GetMethod("extractAllMetadataToolStripMenuItem_Click", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (_extractAllMetadataMethod == null)
                return false;

            try
            {
                _extractAllMetadataMethod.Invoke(panel, new object[] { panel, EventArgs.Empty });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryEnsurePanel(out object panel)
        {
            lock (SyncRoot)
            {
                if (_panelMetadataSearch != null && _handleLinkFoundMethod != null)
                {
                    panel = _panelMetadataSearch;
                    return true;
                }

                panel = null;

                try
                {
                    ResetPanelCache();

                    Type programType = Type.GetType("FOCA.Program, FOCA");
                    if (programType == null)
                    {
                        return false;
                    }

                    FieldInfo formMainField = programType.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static);
                    object formMain = formMainField?.GetValue(null);
                    if (formMain == null)
                    {
                        return false;
                    }

                    Type formMainType = formMain.GetType();
                    FieldInfo panelField = formMainType.GetField("panelMetadataSearch", BindingFlags.Instance | BindingFlags.Public);
                    _panelMetadataSearch = panelField?.GetValue(formMain);
                    if (_panelMetadataSearch == null)
                    {
                        return false;
                    }

                    _panelMetadataSearchType = _panelMetadataSearch.GetType();
                    _handleLinkFoundMethod = _panelMetadataSearchType.GetMethod("HandleLinkFoundEvent", BindingFlags.Instance | BindingFlags.Public);
                    if (_handleLinkFoundMethod == null)
                    {
                        _panelMetadataSearch = null;
                        return false;
                    }

                    if (_eventsThreadsType == null)
                    {
                        _eventsThreadsType = Type.GetType("FOCA.Threads.EventsThreads, SearcherCore");
                    }
                    if (_eventsThreadsType == null)
                    {
                        _eventsThreadsType = Type.GetType("FOCA.Threads.EventsThreads, FOCA");
                    }
                    _collectionFoundGenericType = _eventsThreadsType?.GetNestedType("CollectionFound`1", BindingFlags.Public);

                    panel = _panelMetadataSearch;
                    return true;
                }
                catch
                {
                    ResetPanelCache();
                    return false;
                }
            }
        }

        private static void ResetPanelCache()
        {
            _panelMetadataSearch = null;
            _panelMetadataSearchType = null;
            _handleLinkFoundMethod = null;
            _extractAllMetadataMethod = null;
        }

        private static object CreateCollectionFoundEvent(IEnumerable<Uri> uris)
        {
            try
            {
                if (_collectionFoundGenericType == null)
                    return null;

                Type closedType = _collectionFoundGenericType.MakeGenericType(typeof(Uri));
                IList<Uri> data = uris as IList<Uri> ?? uris.ToList();
                return Activator.CreateInstance(closedType, data);
            }
            catch
            {
                return null;
            }
        }

        // Refrescar listado de proyectos en PanelProject sin cambiar de panel
        internal static bool TryRefreshProjectsList()
        {
            try
            {
                var programT = Type.GetType("FOCA.Program, FOCA");
                var formMain = programT?.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (formMain == null) return false;

                var panelField = formMain.GetType().GetField("panelProject", BindingFlags.Public | BindingFlags.Instance);
                var panelProject = panelField?.GetValue(formMain);
                if (panelProject == null) return false;

                var loadMeth = panelProject.GetType().GetMethod("LoadProject", BindingFlags.Public | BindingFlags.Instance);
                if (loadMeth == null) return false;

                var invoke = formMain.GetType().GetMethod("Invoke", new[] { typeof(Delegate) });
                MethodInvoker action = delegate { loadMeth.Invoke(panelProject, null); };
                invoke?.Invoke(formMain, new object[] { action });
                return true;
            }
            catch { return false; }
        }

        // ----- Recarga silenciosa del proyecto en FOCA -----
        internal static bool TryLoadProjectById(int idProject)
        {
            try
            {
                var programT = Type.GetType("FOCA.Program, FOCA");
                var formMain = programT?.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var data = programT?.GetField("data", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (formMain == null || data == null) return false;

                // Establecer Id en memoria
                var projectField = data.GetType().GetField("Project", BindingFlags.Public | BindingFlags.Instance);
                var project = projectField?.GetValue(data);
                var propId = project?.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                if (propId != null) propId.SetValue(project, idProject, null);

                // Cargar datos desde BD
                var pmT = Type.GetType("FOCA.Core.ProjectManager, FOCA");
                var loadMeth = pmT?.GetMethod("LoadProjectDataController", BindingFlags.Public | BindingFlags.Static);
                loadMeth?.Invoke(null, new object[] { idProject });

                // Refrescar UI (lista y árbol) de forma silenciosa
                var panelField = formMain.GetType().GetField("panelMetadataSearch", BindingFlags.Public | BindingFlags.Instance);
                var panel = panelField?.GetValue(formMain);
                if (panel == null) return false;

                var listField = panel.GetType().GetField("listViewDocuments", BindingFlags.Public | BindingFlags.Instance);
                var listView = listField?.GetValue(panel);
                var listType = listView.GetType();

                var filesField = data.GetType().GetField("files", BindingFlags.Public | BindingFlags.Instance);
                var ficheros = filesField?.GetValue(data);
                var itemsProp = ficheros?.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                var items = itemsProp?.GetValue(ficheros, null) as System.Collections.IEnumerable;

                var beginUpdate = listType.GetMethod("BeginUpdate");
                var endUpdate = listType.GetMethod("EndUpdate");
                var itemsListProp = listType.GetProperty("Items");
                var lvItems = itemsListProp?.GetValue(listView, null);
                var clearMeth = lvItems?.GetType().GetMethod("Clear");

                var updateMeth = panel.GetType().GetMethod("listViewDocuments_Update", BindingFlags.Public | BindingFlags.Instance);
                var treeUpdateMeth = formMain.GetType().GetMethod("treeViewMetadata_UpdateDocumentsNumber", BindingFlags.Public | BindingFlags.Instance);

                var invoke = formMain.GetType().GetMethod("Invoke", new[] { typeof(Delegate) });
                MethodInvoker action = delegate
                {
                    if (beginUpdate != null) beginUpdate.Invoke(listView, null);
                    if (clearMeth != null) clearMeth.Invoke(lvItems, null);
                    if (items != null && updateMeth != null)
                    {
                        foreach (var fi in items)
                        {
                            updateMeth.Invoke(panel, new[] { fi });
                        }
                    }
                    if (treeUpdateMeth != null) treeUpdateMeth.Invoke(formMain, null);
                    if (endUpdate != null) endUpdate.Invoke(listView, null);
                };
                if (invoke != null) invoke.Invoke(formMain, new object[] { action });
                return true;
            }
            catch { return false; }
        }

        // Completar post-procesado de URLs insertadas vía BD (mapa, tecnologías, HEAD size y refresco de fila)
        internal static bool TryFinalizeUrls(IEnumerable<string> urls)
        {
            try
            {
                if (urls == null) return false;

                var programT = Type.GetType("FOCA.Program, FOCA");
                var formMain = programT?.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var data = programT?.GetField("data", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var cfg = programT?.GetField("cfgCurrent", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (formMain == null || data == null) return false;

                // panel y utilidades UI
                var panelField = formMain.GetType().GetField("panelMetadataSearch", BindingFlags.Public | BindingFlags.Instance);
                var panel = panelField?.GetValue(formMain);
                if (panel == null) return false;
                var httpSizeField = panel.GetType().GetField("HttpSizeDaemonInst", BindingFlags.Public | BindingFlags.Instance);
                var httpSize = httpSizeField?.GetValue(panel);
                var addUrlToDaemon = httpSize?.GetType().GetMethod("AddURL", BindingFlags.Public | BindingFlags.Instance);
                var updateLvi = panel.GetType().GetMethod("listViewDocuments_Update", BindingFlags.Public | BindingFlags.Instance);

                // acceso a Program.data.files.Items
                var filesField = data.GetType().GetField("files", BindingFlags.Public | BindingFlags.Instance);
                var ficheros = filesField?.GetValue(data);
                var itemsProp = ficheros?.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                var items = itemsProp?.GetValue(ficheros, null) as System.Collections.IEnumerable;

                Func<string, object> findFiByUrl = (string u) =>
                {
                    if (items == null) return null;
                    foreach (var fi in items)
                    {
                        var urlProp = fi.GetType().GetProperty("URL", BindingFlags.Public | BindingFlags.Instance);
                        var urlVal = urlProp?.GetValue(fi, null) as string;
                        if (!string.IsNullOrEmpty(urlVal) && string.Equals(urlVal, u, StringComparison.OrdinalIgnoreCase))
                            return fi;
                    }
                    return null;
                };

                // métodos de dominios y mapa
                var getDomain = data.GetType().GetMethod("GetDomain", BindingFlags.Public | BindingFlags.Instance);
                var addDomain = data.GetType().GetMethod("AddDomain", BindingFlags.Public | BindingFlags.Instance);

                foreach (var u in urls.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    Uri uri;
                    if (!Uri.TryCreate(u, UriKind.Absolute, out uri)) continue;

                    // Asegurar dominio presente
                    object domain = getDomain?.Invoke(data, new object[] { uri.Host });
                    if (domain == null && addDomain != null)
                    {
                        // source "Documents search" y MaxRecursion 0 como en el flujo original
                        addDomain.Invoke(data, new object[] { uri.Host, "Documents search", 0, cfg });
                        domain = getDomain?.Invoke(data, new object[] { uri.Host });
                    }

                    if (domain != null)
                    {
                        // domain.map.AddDocument(u) y AddUrl(u)
                        var mapProp = domain.GetType().GetProperty("map", BindingFlags.Public | BindingFlags.Instance);
                        var map = mapProp?.GetValue(domain, null);
                        var addDocument = map?.GetType().GetMethod("AddDocument", BindingFlags.Public | BindingFlags.Instance);
                        var addUrl = map?.GetType().GetMethod("AddUrl", BindingFlags.Public | BindingFlags.Instance);
                        addDocument?.Invoke(map, new object[] { u });
                        addUrl?.Invoke(map, new object[] { u });

                        // domain.techAnalysis.eventLinkFoundDetailed(null, CollectionFound<Uri>([uri]))
                        var techField = domain.GetType().GetField("techAnalysis", BindingFlags.Public | BindingFlags.Instance);
                        var tech = techField?.GetValue(domain);
                        if (tech != null)
                        {
                            var evt = CreateCollectionFoundEvent(new[] { uri });
                            var evtMeth = tech.GetType().GetMethod("eventLinkFoundDetailed", BindingFlags.Public | BindingFlags.Instance);
                            evtMeth?.Invoke(tech, new object[] { null, evt });
                        }
                    }

                    // Encolar HEAD y refrescar fila si existe el FilesItem
                    var fi = findFiByUrl(u);
                    if (fi != null)
                    {
                        addUrlToDaemon?.Invoke(httpSize, new[] { fi });
                        updateLvi?.Invoke(panel, new[] { fi });
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool TryReloadCurrentProject()
        {
            try
            {
                var programT = Type.GetType("FOCA.Program, FOCA");
                var data = programT?.GetField("data", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var project = data?.GetType().GetField("Project", BindingFlags.Public | BindingFlags.Instance)?.GetValue(data);
                var propId = project?.GetType().GetProperty("Id", BindingFlags.Public | BindingFlags.Instance);
                int id = 0;
                if (propId != null)
                {
                    object val = propId.GetValue(project, null);
                    if (val is int) id = (int)val;
                }
                if (id <= 0) return false;
                return TryLoadProjectById(id);
            }
            catch { return false; }
        }

        // Encolar descargas para un conjunto de URLs usando el pipeline interno de FOCA
        internal static bool TryEnqueueDownloadsForUrls(IEnumerable<string> urls)
        {
            try
            {
                if (urls == null) return false;
                var programT = Type.GetType("FOCA.Program, FOCA");
                var formMain = programT?.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                var data = programT?.GetField("data", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (formMain == null || data == null) return false;

                // panel y listView
                var panelField = formMain.GetType().GetField("panelMetadataSearch", BindingFlags.Public | BindingFlags.Instance);
                var panel = panelField?.GetValue(formMain);
                if (panel == null) return false;

                var listField = panel.GetType().GetField("listViewDocuments", BindingFlags.Public | BindingFlags.Instance);
                var listView = listField?.GetValue(panel) as System.Windows.Forms.ListView;
                if (listView == null) return false;

                var items = listView.Items;
                var wants = new System.Collections.Generic.HashSet<string>(urls, StringComparer.OrdinalIgnoreCase);
                var toDownload = new System.Collections.Generic.List<System.Windows.Forms.ListViewItem>();
                foreach (System.Windows.Forms.ListViewItem lvi in items)
                {
                    var urlText = lvi.SubItems.Count > 2 ? lvi.SubItems[2].Text : string.Empty;
                    if (wants.Contains(urlText))
                        toDownload.Add(lvi);
                }
                if (toDownload.Count == 0) return true;

                // leer carpeta de proyecto
                var projectField = data.GetType().GetField("Project", BindingFlags.Public | BindingFlags.Instance);
                var project = projectField?.GetValue(data);
                var folderProp = project?.GetType().GetProperty("FolderToDownload", BindingFlags.Public | BindingFlags.Instance);
                var folder = (folderProp?.GetValue(project, null) as string) ?? string.Empty;
                if (!folder.EndsWith("\\")) folder += "\\";

                // invocar método privado EnqueueFilestoDownload(List<ListViewItem>, string)
                var enqueue = panel.GetType().GetMethod("EnqueueFilestoDownload",
                                   System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (enqueue == null) return false;

                var invoke = formMain.GetType().GetMethod("Invoke", new[] { typeof(Delegate) });
                System.Windows.Forms.MethodInvoker action = delegate
                {
                    enqueue.Invoke(panel, new object[] { toDownload, folder });
                };
                invoke?.Invoke(formMain, new object[] { action });
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Reparar descargas 0 KB: reintenta GET directo y actualiza la UI (en background)
        internal static void FixZeroSizeDownloadsAsync(IEnumerable<string> urls, int attempts = 3, int delayMs = 3000)
        {
            if (urls == null) return;
            var urlsList = urls.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            if (urlsList.Count == 0) return;

            var t = new System.Threading.Thread(() =>
            {
                try
                {
                    var programT = Type.GetType("FOCA.Program, FOCA");
                    var formMain = programT?.GetField("FormMainInstance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    var data = programT?.GetField("data", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (formMain == null || data == null) return;

                    var panelField = formMain.GetType().GetField("panelMetadataSearch", BindingFlags.Public | BindingFlags.Instance);
                    var panel = panelField?.GetValue(formMain);
                    if (panel == null) return;
                    var updateLvi = panel.GetType().GetMethod("listViewDocuments_Update", BindingFlags.Public | BindingFlags.Instance);

                    var filesField = data.GetType().GetField("files", BindingFlags.Public | BindingFlags.Instance);
                    var ficheros = filesField?.GetValue(data);
                    var itemsProp = ficheros?.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                    var items = itemsProp?.GetValue(ficheros, null) as System.Collections.IEnumerable;
                    if (items == null) return;

                    object FindFi(string u)
                    {
                        foreach (var fi in items)
                        {
                            var urlProp = fi.GetType().GetProperty("URL", BindingFlags.Public | BindingFlags.Instance);
                            var urlVal = urlProp?.GetValue(fi, null) as string;
                            if (!string.IsNullOrEmpty(urlVal) && string.Equals(urlVal, u, StringComparison.OrdinalIgnoreCase))
                                return fi;
                        }
                        return null;
                    }

                    for (int k = 0; k < attempts; k++)
                    {
                        bool anyFixed = false;
                        foreach (var u in urlsList)
                        {
                            var fi = FindFi(u);
                            if (fi == null) continue;
                            var downloaded = (bool)(fi.GetType().GetProperty("Downloaded")?.GetValue(fi, null) ?? false);
                            var path = fi.GetType().GetProperty("Path")?.GetValue(fi, null) as string;
                            if (!downloaded || string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) continue;
                            var size = new System.IO.FileInfo(path).Length;
                            if (size > 0) continue; // ya tiene contenido

                            // Fallback GET
                            try
                            {
                                var req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(u);
                                req.Method = "GET";
                                req.AllowAutoRedirect = true;
                                req.AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate;
                                req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36";
                                try { req.Referer = new Uri(u).GetLeftPart(UriPartial.Authority); } catch { }
                                req.Headers[System.Net.HttpRequestHeader.AcceptLanguage] = "es-ES,es;q=0.9,en;q=0.8";
                                using (var resp = (System.Net.HttpWebResponse)req.GetResponse())
                                using (var rs = resp.GetResponseStream())
                                using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                                {
                                    rs.CopyTo(fs);
                                }
                                var newSize = (int)new System.IO.FileInfo(path).Length;
                                fi.GetType().GetProperty("Size")?.SetValue(fi, newSize, null);
                                if (newSize > 0)
                                {
                                    anyFixed = true;
                                    // refrescar UI en hilo de UI
                                    var invoke = formMain.GetType().GetMethod("Invoke", new[] { typeof(Delegate) });
                                    System.Windows.Forms.MethodInvoker action = delegate { updateLvi?.Invoke(panel, new[] { fi }); };
                                    invoke?.Invoke(formMain, new object[] { action });
                                }
                            }
                            catch { }
                        }
                        if (!anyFixed) System.Threading.Thread.Sleep(delayMs);
                    }
                }
                catch { }
            });
            t.IsBackground = true;
            t.Start();
        }
    }
#else
    internal static class FocaInterop
    {
        internal static bool IsAvailable => false;

        internal static bool TryHandleLinkFound(IEnumerable<Uri> uris, object sender = null) => false;

        internal static bool TryExtractAllMetadata() => false;
    }
#endif
}

