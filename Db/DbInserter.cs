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
                foreach (ConnectionStringSettings connectionString in ConfigurationManager.ConnectionStrings)
                {
                    string cs = connectionString.ConnectionString.ToLower();
                    if (cs.Contains("data source") && cs.Contains("initial catalog"))
                    {
                        return connectionString.ConnectionString;
                    }
                }
                var nonDefaultConnection = ConfigurationManager.ConnectionStrings
                    .Cast<ConnectionStringSettings>()
                    .FirstOrDefault(cs => !string.IsNullOrEmpty(cs.ConnectionString) &&
                                         !cs.ConnectionString.Contains("LocalSqlServer") &&
                                         !cs.ConnectionString.Contains("DefaultConnection"));
                return nonDefaultConnection?.ConnectionString;
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

        public async Task<int> CreateProjectAsync(string projectName, string notes)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Ensure Projects table exists
                var exists = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME='Projects'", connection);
                if ((int)await exists.ExecuteScalarAsync() == 0)
                    throw new InvalidOperationException("No se encontró la tabla 'Projects' en la BD de FOCA.");

                // Detect columns
                bool hasNotes;
                using (var colCmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Projects' AND COLUMN_NAME='Notes'", connection))
                {
                    hasNotes = (int)await colCmd.ExecuteScalarAsync() > 0;
                }

                SqlCommand insert;
                if (hasNotes)
                {
                    insert = new SqlCommand("INSERT INTO [dbo].[Projects] ([ProjectName],[Notes]) OUTPUT INSERTED.[Id] VALUES (@n,@no)", connection);
                    insert.Parameters.AddWithValue("@n", projectName ?? (object)DBNull.Value);
                    insert.Parameters.AddWithValue("@no", string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes);
                }
                else
                {
                    insert = new SqlCommand("INSERT INTO [dbo].[Projects] ([ProjectName]) OUTPUT INSERTED.[Id] VALUES (@n)", connection);
                    insert.Parameters.AddWithValue("@n", projectName ?? (object)DBNull.Value);
                }

                var id = await insert.ExecuteScalarAsync();
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
                // Detect optional file name column to enrich insertions
                string fileNameCol = null;
                try
                {
                    var cmdCols = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@t", connection);
                    cmdCols.Parameters.AddWithValue("@t", table);
                    using (var rr = await cmdCols.ExecuteReaderAsync())
                    {
                        while (await rr.ReadAsync())
                        {
                            var col = rr.GetString(0);
                            if (string.Equals(col, "FileName", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(col, "Filename", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(col, "Name", StringComparison.OrdinalIgnoreCase))
                            {
                                fileNameCol = col;
                                break;
                            }
                        }
                    }
                }
                catch { }
                foreach (var url in distinct)
                {
                    string sql;
                    if (!string.IsNullOrEmpty(fileNameCol))
                    {
                        sql = $@"IF NOT EXISTS (SELECT 1 FROM [dbo].[{table}] WHERE [{urlCol}]=@u AND [{projectFk}]=@p)
BEGIN
    INSERT INTO [dbo].[{table}] ([{urlCol}],[{projectFk}],[{fileNameCol}]) VALUES (@u,@p,@fn)
END";
                    }
                    else
                    {
                        sql = $@"IF NOT EXISTS (SELECT 1 FROM [dbo].[{table}] WHERE [{urlCol}]=@u AND [{projectFk}]=@p)
BEGIN
    INSERT INTO [dbo].[{table}] ([{urlCol}],[{projectFk}]) VALUES (@u,@p)
END";
                    }
                    var cmd = new SqlCommand(sql, connection);
                    cmd.Parameters.AddWithValue("@u", url);
                    cmd.Parameters.AddWithValue("@p", projectId);
                    if (!string.IsNullOrEmpty(fileNameCol))
                    {
                        var fn = ExtractFileName(url);
                        if (string.IsNullOrWhiteSpace(fn))
                            cmd.Parameters.AddWithValue("@fn", DBNull.Value);
                        else
                            cmd.Parameters.AddWithValue("@fn", fn);
                    }
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


