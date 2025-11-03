namespace Foca.SerpApiSearch.Ui
{
    partial class ConfigForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblApiKey;
        private System.Windows.Forms.TextBox txtApiKey;
        private System.Windows.Forms.Button btnProbar;
        private System.Windows.Forms.Button btnGuardar;
        private System.Windows.Forms.Label lblPriority;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblMinInurl;
        private System.Windows.Forms.NumericUpDown numMinInurl;
        private System.Windows.Forms.Label lblMaxResults;
        private System.Windows.Forms.NumericUpDown numMaxResults;
        private System.Windows.Forms.Label lblMaxPages;
        private System.Windows.Forms.NumericUpDown numMaxPages;
        private System.Windows.Forms.Label lblDelayPages;
        private System.Windows.Forms.NumericUpDown numDelayPages;
        private System.Windows.Forms.Label lblMaxRequests;
        private System.Windows.Forms.NumericUpDown numMaxRequests;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Panel panelFooter;

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
            this.components = new System.ComponentModel.Container();
            this.lblApiKey = new System.Windows.Forms.Label();
            this.txtApiKey = new System.Windows.Forms.TextBox();
            this.btnProbar = new System.Windows.Forms.Button();
            this.btnGuardar = new System.Windows.Forms.Button();
            this.lblPriority = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.lblMinInurl = new System.Windows.Forms.Label();
            this.numMinInurl = new System.Windows.Forms.NumericUpDown();
            this.lblMaxResults = new System.Windows.Forms.Label();
            this.numMaxResults = new System.Windows.Forms.NumericUpDown();
            this.lblMaxPages = new System.Windows.Forms.Label();
            this.numMaxPages = new System.Windows.Forms.NumericUpDown();
            this.lblDelayPages = new System.Windows.Forms.Label();
            this.numDelayPages = new System.Windows.Forms.NumericUpDown();
            this.lblMaxRequests = new System.Windows.Forms.Label();
            this.numMaxRequests = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numMinInurl)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxResults)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPages)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDelayPages)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRequests)).BeginInit();
            this.SuspendLayout();
            // lblMinInurl
            // 
            this.lblMinInurl.AutoSize = true;
            this.lblMinInurl.Location = new System.Drawing.Point(12, 116);
            this.lblMinInurl.Name = "lblMinInurl";
            this.lblMinInurl.Size = new System.Drawing.Size(132, 13);
            this.lblMinInurl.TabIndex = 6;
            this.lblMinInurl.Text = "Longitud mínima para inurl:";
            // 
            // numMinInurl
            // 
            this.numMinInurl.Location = new System.Drawing.Point(184, 114);
            this.numMinInurl.Minimum = new decimal(new int[] {
            0,
            0,
            0,
            0});
            this.numMinInurl.Maximum = new decimal(new int[] {
            1024,
            0,
            0,
            0});
            this.numMinInurl.Name = "numMinInurl";
            this.numMinInurl.Size = new System.Drawing.Size(60, 20);
            this.numMinInurl.TabIndex = 7;
            this.numMinInurl.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
            // 
            // lblMaxResults
            // 
            this.lblMaxResults.AutoSize = true;
            // Mover "Máx. resultados" debajo del bloque izquierdo
            this.lblMaxResults.Location = new System.Drawing.Point(12, 194);
            this.lblMaxResults.Name = "lblMaxResults";
            this.lblMaxResults.Size = new System.Drawing.Size(168, 13);
            this.lblMaxResults.TabIndex = 8;
            this.lblMaxResults.Text = "Máx. resultados (0 = ilimitado):";
            // 
            // numMaxResults
            // 
            this.numMaxResults.Location = new System.Drawing.Point(184, 192);
            this.numMaxResults.Maximum = new decimal(new int[] {
            1000000,
            0,
            0,
            0});
            this.numMaxResults.Name = "numMaxResults";
            this.numMaxResults.Size = new System.Drawing.Size(90, 20);
            this.numMaxResults.TabIndex = 9;
            this.numMaxResults.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // lblMaxPages
            // 
            this.lblMaxPages.AutoSize = true;
            this.lblMaxPages.Location = new System.Drawing.Point(12, 142);
            this.lblMaxPages.Name = "lblMaxPages";
            this.lblMaxPages.Size = new System.Drawing.Size(150, 13);
            this.lblMaxPages.TabIndex = 10;
            this.lblMaxPages.Text = "Máx. páginas (0 = ilimitado):";
            // 
            // numMaxPages
            // 
            this.numMaxPages.Location = new System.Drawing.Point(184, 140);
            this.numMaxPages.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numMaxPages.Name = "numMaxPages";
            this.numMaxPages.Size = new System.Drawing.Size(60, 20);
            this.numMaxPages.TabIndex = 11;
            this.numMaxPages.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // lblDelayPages
            // 
            this.lblDelayPages.AutoSize = true;
            // Mover "Retardo entre páginas" debajo también (después de Máx. resultados)
            this.lblDelayPages.Location = new System.Drawing.Point(12, 220);
            this.lblDelayPages.Name = "lblDelayPages";
            this.lblDelayPages.Size = new System.Drawing.Size(171, 13);
            this.lblDelayPages.TabIndex = 12;
            this.lblDelayPages.Text = "Retardo entre páginas (ms, 0=sin):";
            // 
            // numDelayPages
            // 
            this.numDelayPages.Location = new System.Drawing.Point(184, 218);
            this.numDelayPages.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.numDelayPages.Name = "numDelayPages";
            this.numDelayPages.Size = new System.Drawing.Size(90, 20);
            this.numDelayPages.TabIndex = 13;
            this.numDelayPages.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // lblMaxRequests
            // 
            this.lblMaxRequests.AutoSize = true;
            this.lblMaxRequests.Location = new System.Drawing.Point(12, 168);
            this.lblMaxRequests.Name = "lblMaxRequests";
            this.lblMaxRequests.Size = new System.Drawing.Size(169, 13);
            this.lblMaxRequests.TabIndex = 14;
            this.lblMaxRequests.Text = "Máx. peticiones (0 = ilimitado):";
            // 
            // numMaxRequests
            // 
            this.numMaxRequests.Location = new System.Drawing.Point(184, 166);
            this.numMaxRequests.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
            this.numMaxRequests.Name = "numMaxRequests";
            this.numMaxRequests.Size = new System.Drawing.Size(60, 20);
            this.numMaxRequests.TabIndex = 15;
            this.numMaxRequests.Value = new decimal(new int[] {
            0,
            0,
            0,
            0});
            // 
            // lblApiKey
            // 
            this.lblApiKey.AutoSize = true;
            this.lblApiKey.Location = new System.Drawing.Point(12, 50);
            this.lblApiKey.Name = "lblApiKey";
            this.lblApiKey.Size = new System.Drawing.Size(53, 13);
            this.lblApiKey.TabIndex = 0;
            this.lblApiKey.Text = "API Key:";
            // 
            // txtApiKey
            // 
            this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtApiKey.Location = new System.Drawing.Point(90, 47);
            this.txtApiKey.Name = "txtApiKey";
            this.txtApiKey.Size = new System.Drawing.Size(382, 20);
            this.txtApiKey.TabIndex = 1;
            // 
            // btnProbar
            // 
            this.btnProbar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)));
            this.btnProbar.Location = new System.Drawing.Point(90, 74);
            this.btnProbar.Name = "btnProbar";
            this.btnProbar.AutoSize = true;
            this.btnProbar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnProbar.MinimumSize = new System.Drawing.Size(130, 28);
            this.btnProbar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnProbar.Size = new System.Drawing.Size(130, 28);
            this.btnProbar.TabIndex = 2;
            this.btnProbar.Text = "Probar conexión";
            this.btnProbar.UseVisualStyleBackColor = true;
            this.btnProbar.Click += new System.EventHandler(this.btnProbar_Click);
            // 
            // lblPriority
            // 
            this.lblPriority.AutoSize = true;
            this.lblPriority.Location = new System.Drawing.Point(12, 80);
            this.lblPriority.Name = "lblPriority";
            this.lblPriority.Size = new System.Drawing.Size(35, 13);
            this.lblPriority.TabIndex = 3;
            this.lblPriority.Text = "";
            // 
            // btnGuardar
            // 
            this.btnGuardar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnGuardar.Location = new System.Drawing.Point(12, 156);
            this.btnGuardar.Name = "btnGuardar";
            this.btnGuardar.AutoSize = true;
            this.btnGuardar.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.btnGuardar.MinimumSize = new System.Drawing.Size(90, 28);
            this.btnGuardar.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
            this.btnGuardar.Size = new System.Drawing.Size(90, 28);
            this.btnGuardar.TabIndex = 4;
            this.btnGuardar.Text = "Guardar";
            this.btnGuardar.UseVisualStyleBackColor = true;
            this.btnGuardar.Click += new System.EventHandler(this.btnGuardar_Click);
            // 
            // 
            // ConfigForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(600, 200);
            // panelHeader (estilo FOCA)
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(245, 245, 245);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Height = 40;
            this.lblTitle.AutoSize = true;
            this.lblTitle.Text = "Configuración SerpApi";
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(12, 11);
            this.panelHeader.Controls.Add(this.lblTitle);
            // (sin línea separadora)
            // panelFooter inferior
            this.panelFooter = new System.Windows.Forms.Panel();
            this.panelFooter.BackColor = System.Drawing.SystemColors.Control; // mismo color que fondo
            this.panelFooter.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelFooter.Height = 0; // no visible
            this.panelFooter.Visible = false;
            // Agregar primero para respetar docking
            this.Controls.Add(this.panelFooter);
            this.Controls.Add(this.panelHeader);
            this.Controls.Add(this.numMaxRequests);
            this.Controls.Add(this.lblMaxRequests);
            this.Controls.Add(this.numDelayPages);
            this.Controls.Add(this.lblDelayPages);
            this.Controls.Add(this.numMaxPages);
            this.Controls.Add(this.lblMaxPages);
            this.Controls.Add(this.numMinInurl);
            this.Controls.Add(this.lblMinInurl);
            this.Controls.Add(this.numMaxResults);
            this.Controls.Add(this.lblMaxResults);
            this.Controls.Add(this.btnGuardar);
            this.Controls.Add(this.lblPriority);
            this.Controls.Add(this.btnProbar);
            this.Controls.Add(this.txtApiKey);
            this.Controls.Add(this.lblApiKey);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ConfigForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Configuración";
            this.Load += new System.EventHandler(this.ConfigForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.numMinInurl)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxResults)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxPages)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDelayPages)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxRequests)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}


