namespace Foca.SerpApiSearch.Ui
{
    partial class SearchForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblRootUrl;
        private System.Windows.Forms.TextBox txtRootUrl;
        private System.Windows.Forms.Label lblExtensions;
        private System.Windows.Forms.CheckedListBox chkListExtensions;
        private System.Windows.Forms.Label lblKl;
        private System.Windows.Forms.TextBox txtKl;
        private System.Windows.Forms.Button btnBuscar;
        private System.Windows.Forms.ListBox lstResults;
        private System.Windows.Forms.Label lblCount;
        private System.Windows.Forms.Button btnIncorporarExistente;
        private System.Windows.Forms.Button btnIncorporarNuevo;
        private System.Windows.Forms.Button btnExportar;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.TextBox txtQueryPreview;
        private System.Windows.Forms.CheckBox chkRestrictPath;
        private System.Windows.Forms.Label lblEngine;
        private System.Windows.Forms.ComboBox cmbEngine;
        private System.Windows.Forms.Label lblGoogleDomain;
        private System.Windows.Forms.ComboBox cmbGoogleDomain;
        private System.Windows.Forms.Label lblQuery;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelFooter;
        private System.Windows.Forms.Button btnCopy;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblRootUrl = new System.Windows.Forms.Label();
            this.txtRootUrl = new System.Windows.Forms.TextBox();
            this.lblExtensions = new System.Windows.Forms.Label();
            this.chkListExtensions = new System.Windows.Forms.CheckedListBox();
            this.lblKl = new System.Windows.Forms.Label();
            this.txtKl = new System.Windows.Forms.TextBox();
            this.btnBuscar = new System.Windows.Forms.Button();
            this.lstResults = new System.Windows.Forms.ListBox();
            this.lblCount = new System.Windows.Forms.Label();
            this.btnIncorporarExistente = new System.Windows.Forms.Button();
            this.btnIncorporarNuevo = new System.Windows.Forms.Button();
            this.btnExportar = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.txtQueryPreview = new System.Windows.Forms.TextBox();
            this.chkRestrictPath = new System.Windows.Forms.CheckBox();
            this.lblEngine = new System.Windows.Forms.Label();
            this.cmbEngine = new System.Windows.Forms.ComboBox();
            this.lblGoogleDomain = new System.Windows.Forms.Label();
            this.cmbGoogleDomain = new System.Windows.Forms.ComboBox();
            this.lblQuery = new System.Windows.Forms.Label();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelFooter = new System.Windows.Forms.Panel();
            this.btnCopy = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblRootUrl
            // 
            this.lblRootUrl.AutoSize = true;
            this.lblRootUrl.Location = new System.Drawing.Point(12, 50);
            this.lblRootUrl.Name = "lblRootUrl";
            this.lblRootUrl.Size = new System.Drawing.Size(55, 13);
            this.lblRootUrl.TabIndex = 0;
            this.lblRootUrl.Text = "URL raíz:";
            // 
            // txtRootUrl
            // 
            this.txtRootUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRootUrl.Location = new System.Drawing.Point(90, 47);
            this.txtRootUrl.Name = "txtRootUrl";
            this.txtRootUrl.Size = new System.Drawing.Size(458, 20);
            this.txtRootUrl.TabIndex = 1;
            // 
            // lblExtensions
            // 
            this.lblExtensions.AutoSize = true;
            this.lblExtensions.Location = new System.Drawing.Point(12, 75);
            this.lblExtensions.Name = "lblExtensions";
            this.lblExtensions.Size = new System.Drawing.Size(67, 13);
            this.lblExtensions.TabIndex = 2;
            this.lblExtensions.Text = "Extensiones:";
            // 
            // chkListExtensions
            // 
            this.chkListExtensions.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.chkListExtensions.CheckOnClick = true;
            this.chkListExtensions.Location = new System.Drawing.Point(90, 75);
            this.chkListExtensions.Name = "chkListExtensions";
            this.chkListExtensions.Size = new System.Drawing.Size(458, 94);
            this.chkListExtensions.TabIndex = 3;
            // 
            // lblKl
            // 
            this.lblKl.AutoSize = true;
            this.lblKl.Location = new System.Drawing.Point(12, 181);
            this.lblKl.Name = "lblKl";
            this.lblKl.Size = new System.Drawing.Size(74, 13);
            this.lblKl.TabIndex = 4;
            this.lblKl.Text = "Región (kl):";
            this.lblKl.Visible = false;
            // 
            // txtKl
            // 
            this.txtKl.Location = new System.Drawing.Point(260, 178);
            this.txtKl.Name = "txtKl";
            this.txtKl.Size = new System.Drawing.Size(150, 20);
            this.txtKl.TabIndex = 5;
            this.txtKl.Visible = false;
            // 
            // btnBuscar
            // 
            this.btnBuscar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBuscar.Location = new System.Drawing.Point(554, 6);
            this.btnBuscar.Name = "btnBuscar";
            this.btnBuscar.AutoSize = true;
            this.btnBuscar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnBuscar.MinimumSize = new System.Drawing.Size(90, 28);
            this.btnBuscar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnBuscar.Size = new System.Drawing.Size(90, 28);
            this.btnBuscar.TabIndex = 6;
            this.btnBuscar.Text = "Buscar";
            this.btnBuscar.UseVisualStyleBackColor = true;
            this.btnBuscar.Click += new System.EventHandler(this.btnBuscar_Click);
            // 
            // lstResults
            // 
            this.lstResults.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lstResults.FormattingEnabled = true;
            this.lstResults.IntegralHeight = false;
            this.lstResults.Location = new System.Drawing.Point(15, 302);
            this.lstResults.Name = "lstResults";
            this.lstResults.Size = new System.Drawing.Size(629, 141);
            this.lstResults.TabIndex = 7;
            // 
            // lblCount
            // 
            this.lblCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblCount.AutoSize = true;
            this.lblCount.Location = new System.Drawing.Point(12, 443);
            this.lblCount.Name = "lblCount";
            this.lblCount.Size = new System.Drawing.Size(69, 13);
            this.lblCount.TabIndex = 8;
            this.lblCount.Text = "0 resultados";
            // 
            // btnIncorporarExistente
            // 
            this.btnIncorporarExistente.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnIncorporarExistente.Location = new System.Drawing.Point(330, 436);
            this.btnIncorporarExistente.Name = "btnIncorporarExistente";
            this.btnIncorporarExistente.AutoSize = true;
            this.btnIncorporarExistente.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnIncorporarExistente.MinimumSize = new System.Drawing.Size(150, 28);
            this.btnIncorporarExistente.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnIncorporarExistente.Size = new System.Drawing.Size(150, 28);
            this.btnIncorporarExistente.TabIndex = 9;
            this.btnIncorporarExistente.Text = "Incorporar a proyecto";
            this.btnIncorporarExistente.UseVisualStyleBackColor = true;
            this.btnIncorporarExistente.Click += new System.EventHandler(this.btnIncorporarExistente_Click);
            // 
            // btnIncorporarNuevo
            // 
            this.btnIncorporarNuevo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnIncorporarNuevo.Location = new System.Drawing.Point(486, 436);
            this.btnIncorporarNuevo.Name = "btnIncorporarNuevo";
            this.btnIncorporarNuevo.AutoSize = true;
            this.btnIncorporarNuevo.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnIncorporarNuevo.MinimumSize = new System.Drawing.Size(170, 28);
            this.btnIncorporarNuevo.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnIncorporarNuevo.Size = new System.Drawing.Size(170, 28);
            this.btnIncorporarNuevo.TabIndex = 10;
            this.btnIncorporarNuevo.Text = "Incorporar a nuevo proyecto";
            this.btnIncorporarNuevo.UseVisualStyleBackColor = true;
            this.btnIncorporarNuevo.Click += new System.EventHandler(this.btnIncorporarNuevo_Click);
            // 
            // btnExportar
            // 
            this.btnExportar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExportar.Location = new System.Drawing.Point(554, 39);
            this.btnExportar.Name = "btnExportar";
            this.btnExportar.AutoSize = true;
            this.btnExportar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnExportar.MinimumSize = new System.Drawing.Size(110, 28);
            this.btnExportar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnExportar.Size = new System.Drawing.Size(110, 28);
            this.btnExportar.TabIndex = 11;
            this.btnExportar.Text = "Exportar CSV";
            this.btnExportar.UseVisualStyleBackColor = true;
            this.btnExportar.Click += new System.EventHandler(this.btnExportar_Click);
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.Location = new System.Drawing.Point(561, 436);
            this.btnClose.Name = "btnClose";
            this.btnClose.AutoSize = true;
            this.btnClose.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnClose.MinimumSize = new System.Drawing.Size(90, 28);
            this.btnClose.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnClose.Size = new System.Drawing.Size(90, 28);
            this.btnClose.TabIndex = 12;
            this.btnClose.Text = "Cerrar";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // txtQueryPreview
            // 
            this.txtQueryPreview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtQueryPreview.Location = new System.Drawing.Point(90, 244);
            this.txtQueryPreview.Name = "txtQueryPreview";
            this.txtQueryPreview.ReadOnly = true;
            this.txtQueryPreview.Multiline = true;
            this.txtQueryPreview.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtQueryPreview.Size = new System.Drawing.Size(454, 44);
            this.txtQueryPreview.TabIndex = 13;
            this.txtQueryPreview.DoubleClick += new System.EventHandler(this.txtQueryPreview_DoubleClick);
            // 
            // chkRestrictPath
            // 
            this.chkRestrictPath.AutoSize = true;
            this.chkRestrictPath.Checked = true;
            this.chkRestrictPath.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkRestrictPath.Enabled = false;
            this.chkRestrictPath.Location = new System.Drawing.Point(260, 209);
            this.chkRestrictPath.Name = "chkRestrictPath";
            this.chkRestrictPath.Size = new System.Drawing.Size(151, 17);
            this.chkRestrictPath.TabIndex = 14;
            this.chkRestrictPath.Text = "Restringir a ruta indicada";
            this.chkRestrictPath.UseVisualStyleBackColor = true;
            this.chkRestrictPath.Visible = false;
            // 
            // lblEngine
            // 
            this.lblEngine.AutoSize = true;
            // mover a la izquierda al ocultar Región/Restringir ruta y dejar margen superior
            this.lblEngine.Location = new System.Drawing.Point(12, 212);
            this.lblEngine.Name = "lblEngine";
            this.lblEngine.Size = new System.Drawing.Size(52, 13);
            this.lblEngine.TabIndex = 15;
            this.lblEngine.Text = "Buscador:";
            // 
            // cmbEngine
            // 
            this.cmbEngine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEngine.FormattingEnabled = true;
            this.cmbEngine.Items.AddRange(new object[] {
            "DuckDuckGo",
            "Google",
            "Bing"});
            this.cmbEngine.Location = new System.Drawing.Point(90, 209);
            this.cmbEngine.Name = "cmbEngine";
            this.cmbEngine.Size = new System.Drawing.Size(121, 21);
            this.cmbEngine.TabIndex = 16;
            this.cmbEngine.SelectedItem = "Google";
            this.cmbEngine.SelectedIndexChanged += new System.EventHandler(this.cmbEngine_SelectedIndexChanged);
            // 
            // lblGoogleDomain
            // 
            this.lblGoogleDomain.AutoSize = true;
            this.lblGoogleDomain.Location = new System.Drawing.Point(515, 212);
            this.lblGoogleDomain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblGoogleDomain.Name = "lblGoogleDomain";
            this.lblGoogleDomain.Size = new System.Drawing.Size(32, 13);
            this.lblGoogleDomain.TabIndex = 17;
            this.lblGoogleDomain.Text = "TLD:";
            // 
            // cmbGoogleDomain
            // 
            this.cmbGoogleDomain.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGoogleDomain.FormattingEnabled = true;
            this.cmbGoogleDomain.Items.AddRange(new object[] {
            "google.es",
            "google.com",
            "google.fr",
            "google.de"});
            this.cmbGoogleDomain.Location = new System.Drawing.Point(553, 209);
            this.cmbGoogleDomain.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbGoogleDomain.Name = "cmbGoogleDomain";
            this.cmbGoogleDomain.Size = new System.Drawing.Size(91, 21);
            this.cmbGoogleDomain.TabIndex = 18;
            this.cmbGoogleDomain.SelectedItem = "google.es";
            // lblQuery
            // 
            this.lblQuery.AutoSize = true;
            this.lblQuery.Location = new System.Drawing.Point(12, 246);
            this.lblQuery.Name = "lblQuery";
            this.lblQuery.Size = new System.Drawing.Size(47, 13);
            this.lblQuery.TabIndex = 19;
            this.lblQuery.Text = "Consulta:";
            // 
            // SearchForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(656, 473);
            // panelHeader (estilo FOCA)
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 40;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Text = "Búsqueda avanzada";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 11);
            this.panelHeader.Controls.Add(this.lblTitle);
            this.panelHeader.Controls.Add(this.btnBuscar);
            // panelFooter (separador inferior)
            this.panelFooter.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Height = 10;
            // btnCopy (copiar consulta)
            // 
            this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopy.Location = new System.Drawing.Point(554, 244);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.AutoSize = true;
            this.btnCopy.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnCopy.MinimumSize = new System.Drawing.Size(90, 28);
            this.btnCopy.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnCopy.Size = new System.Drawing.Size(90, 28);
            this.btnCopy.TabIndex = 20;
            this.btnCopy.Text = "Copiar";
            this.btnCopy.UseVisualStyleBackColor = true;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // Agregar paneles primero para respetar docking
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.cmbGoogleDomain);
            this.Controls.Add(this.lblGoogleDomain);
            this.Controls.Add(this.lblQuery);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.cmbEngine);
            this.Controls.Add(this.lblEngine);
            this.Controls.Add(this.chkRestrictPath);
            this.Controls.Add(this.txtQueryPreview);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnExportar);
            this.Controls.Add(this.lstResults);
            this.Controls.Add(this.btnIncorporarNuevo);
            this.Controls.Add(this.btnIncorporarExistente);
            this.Controls.Add(this.lblCount);
            this.Controls.Add(this.txtKl);
            this.Controls.Add(this.lblKl);
            this.Controls.Add(this.chkListExtensions);
            this.Controls.Add(this.lblExtensions);
            this.Controls.Add(this.txtRootUrl);
            this.Controls.Add(this.lblRootUrl);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(600, 350);
            this.Name = "SearchForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Buscar documentos (SerpApi)";
            this.Load += new System.EventHandler(this.SearchForm_Load);
            this.AcceptButton = this.btnBuscar;
            this.CancelButton = this.btnClose;
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}


