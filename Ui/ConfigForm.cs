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
        public bool Embedded { get; set; }
        public ConfigForm()
        {
            InitializeComponent();
        }

        private void ConfigForm_Load(object sender, EventArgs e)
        {
            // Ajustes para modo embebido en panel del host
            if (Embedded)
            {
                try
                {
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.TopLevel = false;
                }
                catch { }
            }
            var env = SerpApiSettings.ResolveApiKey();
            var local = SerpApiConfigStore.Load()?.SerpApiKey;
            txtApiKey.Text = string.IsNullOrWhiteSpace(env) ? local : env;
            // Ocultar texto de prioridad (no se muestra en la UI)
            lblPriority.Text = string.Empty;
            lblPriority.Visible = false;
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

            // Cargar icono de Guardar si existe (recurso embebido img/save.png)
            try
            {
                using (var stream = typeof(ConfigForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.save.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            // Tamaño similar al usado en el plugin Excel (iconos pequeños 16x16)
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnGuardar.Image = sized;
                        }
                        btnGuardar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnGuardar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                        // Asegurar altura mínima para que el icono respire
                        if (btnGuardar.MinimumSize.Height < 32) btnGuardar.MinimumSize = new System.Drawing.Size(btnGuardar.MinimumSize.Width, 32);
                    }
                }
            }
            catch { }

            // Icono de conexión (img/connection.png)
            try
            {
                using (var stream = typeof(ConfigForm).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.connection.png"))
                {
                    if (stream != null)
                    {
                        using (var img = System.Drawing.Image.FromStream(stream))
                        {
                            var sized = new System.Drawing.Bitmap(img, new System.Drawing.Size(16, 16));
                            btnProbar.Image = sized;
                        }
                        btnProbar.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
                        btnProbar.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
                    }
                }
            }
            catch { }

            // Asegurar que el botón queda por delante del footer y con margen estable
            try { btnGuardar.BringToFront(); } catch { }
            try { this.Resize -= ConfigForm_Resize; } catch { }
            this.Resize += ConfigForm_Resize;
            ConfigForm_Resize(this, EventArgs.Empty);
        }

        private void ConfigForm_Resize(object sender, EventArgs e)
        {
            try
            {
                // Dejar un margen inferior de 12 px respecto al panelFooter (altura 10)
                int footerHeight = (panelFooter != null && panelFooter.Visible) ? panelFooter.Height : 0;
                int bottomMargin = footerHeight + 12;
                int newTop = Math.Max(0, this.ClientSize.Height - bottomMargin - btnGuardar.Height);
                btnGuardar.Top = newTop;
                btnGuardar.Left = 12;
            }
            catch { }
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
                        if (!Embedded)
                        {
                            this.DialogResult = DialogResult.OK;
                            this.Close();
                        }
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
                if (!Embedded)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo guardar la configuración: {ex.Message}", "Configuración de SerpApi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cancelar eliminado para vista embebida
    }
}


