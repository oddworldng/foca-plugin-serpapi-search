using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Foca.SerpApiSearch.Db
{
    /// <summary>
    /// Inserts URLs into FOCA DB, detecting FilesITems/UrlsItems and avoiding duplicates.
    /// Reuses FOCA connection string as in foca-excel-export.
    /// </summary>
    public class DbInserter
    {
        private readonly string _connectionString;

        public DbInserter()
        {
            _connectionString = ResolveConnectionString();
        }

        private static string ResolveConnectionString()
        {
            try
            {
				// 1) Si existe FocaContextDb (igual que FOCA), priorizarla
#if FOCA_API
				var fromFoca = ConfigurationManager.ConnectionStrings["FocaContextDb"]?.ConnectionString;
				if (!string.IsNullOrWhiteSpace(fromFoca))
					return fromFoca;
#endif

				// 2) Standalone: usar FocaContextDb si está presente en el config del plugin
				var focaConn = ConfigurationManager.ConnectionStrings["FocaContextDb"]?.ConnectionString;
				if (!string.IsNullOrWhiteSpace(focaConn))
					return focaConn;

				// 3) Heurística ampliada: primera que tenga server|data source e initial catalog
				foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
				{
					var cs = (connectionString?.ConnectionString ?? string.Empty).ToLowerInvariant();
					if ((cs.Contains("data source") || cs.Contains("server")) && cs.Contains("initial catalog"))
						return connectionString.ConnectionString;
				}

				// 4) Último recurso: cualquiera no por defecto
				var nonDefaultConnection = ConfigurationManager.ConnectionStrings
					.Cast<ConnectionStringSettings>()
					.FirstOrDefault(cs => !string.IsNullOrEmpty(cs.ConnectionString) &&
										 !cs.Name.Equals("LocalSqlServer", StringComparison.OrdinalIgnoreCase) &&
										 !cs.Name.Equals("DefaultConnection", StringComparison.OrdinalIgnoreCase));
				if (nonDefaultConnection != null)
					return nonDefaultConnection.ConnectionString;

				throw new InvalidOperationException("No se encontró ninguna cadena de conexión válida.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("No se pudo obtener el connection string de FOCA", ex);
            }
        }

        public async Task<(string table, string urlColumn, string projectFkColumn)> DetectUrlsTableAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Prefer FilesITems then UrlsItems
                string[] candidates = { "FilesITems", "UrlsItems", "FilesItems" };
                foreach (var t in candidates)
                {
                    var check = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@t", connection);
                    check.Parameters.AddWithValue("@t", t);
                    var exists = (int)await check.ExecuteScalarAsync() > 0;
                    if (!exists) continue;
                    // Determine columns
                    var urlCol = await FindUrlColumnAsync(connection, t);
                    var projectFk = await FindProjectFkAsync(connection, t);
                    if (!string.IsNullOrEmpty(urlCol) && !string.IsNullOrEmpty(projectFk))
                        return (t, urlCol, projectFk);
                }
            }
            // Fallback
            return ("FilesITems", "URL", "IdProject");
        }

        private static async Task<string> FindUrlColumnAsync(SqlConnection connection, string table)
        {
            var cmd = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", connection);
            cmd.Parameters.AddWithValue("@t", table);
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var c = r.GetString(0);
                    if (string.Equals(c, "URL", StringComparison.OrdinalIgnoreCase)) return c;
                    if (c.ToLower().Contains("url")) return c;
                }
            }
            return null;
        }

        private static async Task<string> FindProjectFkAsync(SqlConnection connection, string table)
        {
            var cmd = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", connection);
            cmd.Parameters.AddWithValue("@t", table);
            using (var r = await cmd.ExecuteReaderAsync())
            {
                while (await r.ReadAsync())
                {
                    var c = r.GetString(0);
                    if (string.Equals(c, "IdProject", StringComparison.OrdinalIgnoreCase)) return c;
                    if (string.Equals(c, "ProjectId", StringComparison.OrdinalIgnoreCase)) return c;
                    if (c.ToLower().Contains("project") && c.ToLower().EndsWith("id")) return c;
                }
            }
            return null;
        }

        public async Task<int> CreateProjectAsync(string projectName, string notes, string domain = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Ensure Projects table exists
                var exists = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Projects'", connection);
                if ((int)await exists.ExecuteScalarAsync() == 0)
                    throw new InvalidOperationException("No se encontró la tabla 'Projects' en la BD de FOCA.");

                // Detect columns y restricciones mínimas (evitar NULL en columnas sin default)
                bool hasProjectNotes = false;
                bool hasNotes = false; // compatibilidad con esquemas antiguos
                bool hasProjectState = false, projectStateNotNullableNoDefault = false;
                bool hasProjectSaveFile = false, projectSaveFileNotNullableNoDefault = false;
                bool hasDomain = false, domainNotNullableNoDefault = false;
                bool hasProjectDate = false, projectDateNotNullableNoDefault = false;
                bool hasFolderToDownload = false, folderToDownloadNotNullableNoDefault = false;
                try
                {
                    using (var cols = new SqlCommand("SELECT COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Projects'", connection))
                    using (var r = await cols.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            var col = (r[0] as string) ?? string.Empty;
                            var isNullable = string.Equals(r[1] as string, "YES", StringComparison.OrdinalIgnoreCase);
                            var hasDefault = !(r[2] is DBNull);
                            if (string.Equals(col, "ProjectNotes", StringComparison.OrdinalIgnoreCase)) hasProjectNotes = true;
                            if (string.Equals(col, "Notes", StringComparison.OrdinalIgnoreCase)) hasNotes = true;
                            if (string.Equals(col, "ProjectState", StringComparison.OrdinalIgnoreCase))
                            {
                                hasProjectState = true;
                                projectStateNotNullableNoDefault = !isNullable && !hasDefault;
                            }
                            if (string.Equals(col, "ProjectSaveFile", StringComparison.OrdinalIgnoreCase))
                            {
                                hasProjectSaveFile = true;
                                projectSaveFileNotNullableNoDefault = !isNullable && !hasDefault;
                            }
                            if (string.Equals(col, "Domain", StringComparison.OrdinalIgnoreCase))
                            {
                                hasDomain = true;
                                domainNotNullableNoDefault = !isNullable && !hasDefault;
                            }
                            if (string.Equals(col, "ProjectDate", StringComparison.OrdinalIgnoreCase))
                            {
                                hasProjectDate = true;
                                projectDateNotNullableNoDefault = !isNullable && !hasDefault;
                            }
                            if (string.Equals(col, "FolderToDownload", StringComparison.OrdinalIgnoreCase))
                            {
                                hasFolderToDownload = true;
                                folderToDownloadNotNullableNoDefault = !isNullable && !hasDefault;
                            }
                        }
                    }
                }
                catch { }

                // Construir INSERT dinámico con columnas soportadas
                var columns = new System.Collections.Generic.List<string>();
                var values = new System.Collections.Generic.List<string>();
                var cmdTextPrefix = "INSERT INTO [dbo].[Projects] (";
                var cmd = new SqlCommand();

                columns.Add("[ProjectName]");
                values.Add("@n");
                cmd.Parameters.AddWithValue("@n", projectName ?? (object)DBNull.Value);

                if (hasProjectNotes)
                {
                    columns.Add("[ProjectNotes]");
                    values.Add("@pn");
                    cmd.Parameters.AddWithValue("@pn", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                }
                else if (hasNotes)
                {
                    columns.Add("[Notes]");
                    values.Add("@no");
                    cmd.Parameters.AddWithValue("@no", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                }

                if (hasDomain)
                {
                    columns.Add("[Domain]");
                    values.Add("@d");
                    cmd.Parameters.AddWithValue("@d", string.IsNullOrWhiteSpace(domain) ? (object)DBNull.Value : domain);
                }

                if (hasProjectState)
                {
                    // FOCA espera proyectos inicializados para habilitar flujos (InitializedUnsave = 1)
                    columns.Add("[ProjectState]");
                    values.Add("@ps");
                    cmd.Parameters.AddWithValue("@ps", 1);
                }

                if (hasProjectSaveFile)
                {
                    columns.Add("[ProjectSaveFile]");
                    values.Add("@psf");
                    cmd.Parameters.AddWithValue("@psf", 1);
                }

                if (hasProjectDate)
                {
                    columns.Add("[ProjectDate]");
                    values.Add("@pdt");
                    cmd.Parameters.AddWithValue("@pdt", DateTime.Now);
                }

                if (hasFolderToDownload)
                {
                    columns.Add("[FolderToDownload]");
                    values.Add("@fd");
                    var defFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FOCA Files");
                    cmd.Parameters.AddWithValue("@fd", defFolder);
                }

                cmd.CommandText = cmdTextPrefix + string.Join(",", columns) + ") OUTPUT INSERTED.[Id] VALUES (" + string.Join(",", values) + ")";
                cmd.Connection = connection;

                var id = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(id);
            }
        }

        public async Task<(int inserted, int duplicates)> InsertUrlsAsync(int projectId, string[] urls)
        {
            if (urls == null || urls.Length == 0) return (0, 0);
            var distinct = urls.Select(u => (u ?? string.Empty).Trim()).Where(u => u.Length > 0).Distinct().ToArray();
            var (table, urlCol, projectFk) = await DetectUrlsTableAsync();

            int ins = 0, dup = 0;
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Detect columnas presentes y cuáles son NOT NULL sin default
                var allColumns = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var required = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var cmdCols = new SqlCommand("SELECT COLUMN_NAME, IS_NULLABLE, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", connection))
                {
                    cmdCols.Parameters.AddWithValue("@t", table);
                    using (var rr = await cmdCols.ExecuteReaderAsync())
                    {
                        while (await rr.ReadAsync())
                        {
                            var col = rr.GetString(0);
                            allColumns.Add(col);
                            var isNullable = string.Equals((rr[1] as string) ?? "", "YES", StringComparison.OrdinalIgnoreCase);
                            var hasDefault = !(rr[2] is DBNull);
                            if (!isNullable && !hasDefault &&
                                !string.Equals(col, "Id", StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(col, urlCol, StringComparison.OrdinalIgnoreCase) &&
                                !string.Equals(col, projectFk, StringComparison.OrdinalIgnoreCase))
                            {
                                required.Add(col);
                            }
                        }
                    }
                }

                // Algunas columnas típicas en FilesITems
                bool hasExt = allColumns.Contains("Ext"); // rellenar Ext siempre que exista la columna
                bool needDownloaded = required.Contains("Downloaded");
                bool needMetadataExtracted = required.Contains("MetadataExtracted");
                bool needDate = required.Contains("Date");
                bool needModifiedDate = required.Contains("ModifiedDate");
                bool needSize = required.Contains("Size");
                bool needDiarioAnalyzed = required.Contains("DiarioAnalyzed");
                bool needDiarioPrediction = required.Contains("DiarioPrediction");
                bool needPath = required.Contains("Path");

                foreach (var url in distinct)
                {
                    // Construir INSERT dinámico con columnas estrictamente necesarias
                    var cols = new System.Collections.Generic.List<string> { $"[{urlCol}]", $"[{projectFk}]" };
                    var vals = new System.Collections.Generic.List<string> { "@u", "@p" };

                    if (hasExt) { cols.Add("[Ext]"); vals.Add("@ext"); }
                    if (needDownloaded) { cols.Add("[Downloaded]"); vals.Add("@dwn"); }
                    if (needMetadataExtracted) { cols.Add("[MetadataExtracted]"); vals.Add("@mext"); }
                    if (needDate) { cols.Add("[Date]"); vals.Add("@dt"); }
                    if (needModifiedDate) { cols.Add("[ModifiedDate]"); vals.Add("@mdt"); }
                    if (needSize) { cols.Add("[Size]"); vals.Add("@sz"); }
                    if (needDiarioAnalyzed) { cols.Add("[DiarioAnalyzed]"); vals.Add("@dan"); }
                    if (needDiarioPrediction) { cols.Add("[DiarioPrediction]"); vals.Add("@dpr"); }
                    if (needPath) { cols.Add("[Path]"); vals.Add("@pth"); }

                    string sql = $@"IF NOT EXISTS (SELECT 1 FROM [dbo].[{table}] WHERE [{urlCol}]=@u AND [{projectFk}]=@p)
BEGIN
    INSERT INTO [dbo].[{table}] ({string.Join(",", cols)}) VALUES ({string.Join(",", vals)})
END";

                    var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@u", url);
                    cmd.Parameters.AddWithValue("@p", projectId);

                    if (hasExt)
                    {
                        string ext = null;
                        try
                        {
                            var u = new Uri(url, UriKind.Absolute);
                            // Usar Path.GetExtension para incluir el punto (".pdf")
                            ext = System.IO.Path.GetExtension(u.AbsolutePath);
                            if (!string.IsNullOrEmpty(ext)) ext = ext.ToLowerInvariant();
                        }
                        catch { }
                        cmd.Parameters.AddWithValue("@ext", (object)(string.IsNullOrEmpty(ext) ? ".bin" : ext));
                    }
                    if (needDownloaded) cmd.Parameters.AddWithValue("@dwn", 0);
                    if (needMetadataExtracted) cmd.Parameters.AddWithValue("@mext", 0);
                    if (needDate) cmd.Parameters.AddWithValue("@dt", DateTime.Now);
                    if (needModifiedDate) cmd.Parameters.AddWithValue("@mdt", DateTime.Now);
                    if (needSize) cmd.Parameters.AddWithValue("@sz", 0);
                    if (needDiarioAnalyzed) cmd.Parameters.AddWithValue("@dan", 0);
                    if (needDiarioPrediction) cmd.Parameters.AddWithValue("@dpr", 0);
                    if (needPath) cmd.Parameters.AddWithValue("@pth", "");

                    var affected = await cmd.ExecuteNonQueryAsync();
                    if (affected > 0) ins++; else dup++;
                }
            }
            return (ins, dup);
        }

        private static string ExtractFileName(string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url)) return null;
                Uri uri;
                if (Uri.TryCreate(url, UriKind.Absolute, out uri))
                {
                    var seg = uri.Segments;
                    if (seg != null && seg.Length > 0) return seg[seg.Length - 1].Trim('/');
                }
                var idx = url.LastIndexOf('/') + 1;
                return idx > 0 && idx < url.Length ? url.Substring(idx) : url;
            }
            catch { return null; }
        }
    }
}


