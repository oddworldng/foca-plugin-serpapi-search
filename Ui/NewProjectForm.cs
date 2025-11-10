using System;
using System.Windows.Forms;

namespace Foca.SerpApiSearch.Ui
{
    public partial class NewProjectForm : Form
    {
        public string ProjectName { get; private set; }
        public string FolderPath { get; private set; }

        public NewProjectForm()
        {
            InitializeComponent();
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            var name = txtName.Text?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Indica un nombre de proyecto.", "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var folder = txtFolder.Text?.Trim();
            if (string.IsNullOrWhiteSpace(folder))
            {
                MessageBox.Show("Selecciona una carpeta de descarga.", "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                if (!System.IO.Directory.Exists(folder))
                {
                    System.IO.Directory.CreateDirectory(folder);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"No se pudo crear/validar la carpeta: {ex.Message}", "Nuevo proyecto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ProjectName = name;
            FolderPath = folder;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                if (folderBrowserDialog1.ShowDialog(this) == DialogResult.OK)
                {
                    txtFolder.Text = folderBrowserDialog1.SelectedPath;
                }
            }
            catch { }
        }
    }
}


