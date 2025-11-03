using System;
using System.Configuration;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Foca.SerpApiSearch.Api;
using Foca.SerpApiSearch.Config;

namespace Foca.SerpApiSearch.Ui
{
    public partial class ConfigForm : Form
    {
        public ConfigForm()
        {
            InitializeComponent();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            var env = SerpApiSettings.ResolveApiKey();
            var local = SerpApiConfigStore.Load()?.SerpApiKey;
            txtApiKey.Text = string.IsNullOrWhiteSpace(env) ? local : env;
            lblPriority.Text = "Prioridad de lectura: 1) SERPAPI_API_KEY  2) config.json";
            // Cargar MinInurlSegmentLength
            var cfg = SerpApiConfigStore.Load();
            if (cfg != null)
            {
                try { numMinInurl.Value = Math.Max(0, Math.Min(32, cfg.MinInurlSegmentLength)); } catch { }
                try { numMaxResults.Value = Math.Max(0, Math.Min(1000000, cfg.MaxResults)); } catch { }
                try { numMaxPages.Value = Math.Max(0, Math.Min(10000, cfg.MaxPagesPerSearch)); } catch { }
                try { numDelayPages.Value = Math.Max(0, Math.Min(60000, cfg.DelayBetweenPagesMs)); } catch { }
                try { numMaxRequests.Value = Math.Max(0, Math.Min(100000, cfg.MaxRequestsPerSearch)); } catch { }
            }
            toolTip1.SetToolTip(lblPriority, "La variable de entorno SERPAPI_API_KEY tiene prioridad sobre config.json en %APPDATA%\\FOCA\\Plugins\\SerpApiSearch\\config.json");
        }

        private async void btnProbar_Click(object sender, EventArgs e)
        {
            await TestAsync();
        }

        private async Task TestAsync()
        {
            btnProbar.Enabled = false;
            try
            {
                using (var client = new SerpApiClient())
                {
                    var (ok, error, _) = await client.TestConnectionAsync(txtApiKey.Text);
                    if (ok)
                    {
                        MessageBox.Show("Conexión correcta con SerpApi.", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(error ?? "No se pudo validar la API Key.", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al probar conexión: {ex.Message}", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnProbar.Enabled = true;
            }
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            try
            {
                var env = SerpApiSettings.ResolveApiKey();
                if (!string.IsNullOrWhiteSpace(env))
                {
                    var r = MessageBox.Show("Existe SERPAPI_API_KEY definida en el entorno. ¿Deseas guardar también en config.json? (SERPAPI_API_KEY tendrá prioridad)",
                        "Configuración de SerpApi", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r == DialogResult.No)
                    {
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                        return;
                    }
                }
                SerpApiConfigStore.Save(new SerpApiSettings {
                    SerpApiKey = txtApiKey.Text?.Trim(),
                    MinInurlSegmentLength = (int)numMinInurl.Value,
                    MaxResults = (int)numMaxResults.Value,
                    MaxPagesPerSearch = (int)numMaxPages.Value,
                    DelayBetweenPagesMs = (int)numDelayPages.Value,
                    MaxRequestsPerSearch = (int)numMaxRequests.Value
                });
                MessageBox.Show("Configuración guardada correctamente.", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar la configuración: {ex.Message}", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}


