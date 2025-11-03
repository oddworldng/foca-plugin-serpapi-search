using System;
using System.Windows.Forms;

namespace Foca.SerpApiSearch.Ui
{
    public partial class NewProjectForm : Form
    {
        public string ProjectName { get; private set; }
        public string ProjectNotes { get; private set; }

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
            ProjectName = name;
            ProjectNotes = txtNotes.Text?.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}


