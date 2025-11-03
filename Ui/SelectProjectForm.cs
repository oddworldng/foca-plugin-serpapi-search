using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Foca.SerpApiSearch.Ui
{
    public partial class SelectProjectForm : Form
    {
        public ProjectInfo SelectedProject { get; private set; }
        private readonly string _connectionString;

        public SelectProjectForm()
        {
            InitializeComponent();
            _connectionString = ResolveConnectionString();
        }

        private static string ResolveConnectionString()
        {
            foreach (ConnectionStringSettings cs in ConfigurationManager.ConnectionStrings)
            {
                string s = cs.ConnectionString.ToLower();
                if (s.Contains("data source") && s.Contains("initial catalog")) return cs.ConnectionString;
            }
            var fallback = ConfigurationManager.ConnectionStrings.Cast<ConnectionStringSettings>()
                .FirstOrDefault(c => !string.IsNullOrEmpty(c.ConnectionString) && !c.ConnectionString.Contains("LocalSqlServer") && !c.ConnectionString.Contains("DefaultConnection"));
            return fallback?.ConnectionString;
        }

        private async void SelectProjectForm_Load(object sender, EventArgs e)
        {
            await LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            try
            {
                lstProjects.Items.Clear();
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = "SELECT [Id],[ProjectName] FROM [dbo].[Projects] ORDER BY [ProjectName]";
                    using (var cmd = new SqlCommand(query, connection))
                    using (var r = await cmd.ExecuteReaderAsync())
                    {
                        while (await r.ReadAsync())
                        {
                            lstProjects.Items.Add(new ProjectInfo { Id = Convert.ToInt32(r[0]), Name = r[1]?.ToString() ?? "Proyecto" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudieron cargar proyectos: {ex.Message}", "Seleccionar proyecto", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void txtFilter_TextChanged(object sender, EventArgs e)
        {
            var term = txtFilter.Text?.Trim().ToLower() ?? string.Empty;
            for (int i = 0; i < lstProjects.Items.Count; i++)
            {
                var p = (ProjectInfo)lstProjects.Items[i];
                lstProjects.SetSelected(i, false);
                lstProjects.TopIndex = 0;
            }
            // simple filtering by rebuilding list
            // Reload and filter when typing for simplicity
            _ = RefilterAsync(term);
        }

        private async Task RefilterAsync(string term)
        {
            try
            {
                lstProjects.BeginUpdate();
                lstProjects.Items.Clear();
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    string query = string.IsNullOrWhiteSpace(term)
                        ? "SELECT [Id],[ProjectName] FROM [dbo].[Projects] ORDER BY [ProjectName]"
                        : "SELECT [Id],[ProjectName] FROM [dbo].[Projects] WHERE [ProjectName] LIKE @t ORDER BY [ProjectName]";
                    using (var cmd = new SqlCommand(query, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(term)) cmd.Parameters.AddWithValue("@t", "%" + term + "%");
                        using (var r = await cmd.ExecuteReaderAsync())
                        {
                            while (await r.ReadAsync())
                            {
                                lstProjects.Items.Add(new ProjectInfo { Id = Convert.ToInt32(r[0]), Name = r[1]?.ToString() ?? "Proyecto" });
                            }
                        }
                    }
                }
            }
            catch { }
            finally { lstProjects.EndUpdate(); }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (lstProjects.SelectedItem is ProjectInfo p)
            {
                SelectedProject = p;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Selecciona un proyecto.", "Seleccionar proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }

    public class ProjectInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public override string ToString() => Name;
    }
}


