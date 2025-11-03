using System;
using System.Windows.Forms;

namespace Foca.SerpApiSearch.Ui
{
    public class MainControl : UserControl
    {
        private readonly TabControl _tabs;
        private readonly TabPage _tabSearch;
        private readonly TabPage _tabConfig;

        public MainControl()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill, Alignment = TabAlignment.Top };            
            _tabSearch = new TabPage("Buscar");
            _tabConfig = new TabPage("Configuración");
            _tabs.TabPages.Add(_tabSearch);
            _tabs.TabPages.Add(_tabConfig);
            Controls.Add(_tabs);

            this.Load += MainControl_Load;
        }

        private void MainControl_Load(object sender, EventArgs e)
        {
            try
            {
                // Embed SearchForm
                var search = new SearchForm { Embedded = true, TopLevel = false, Dock = DockStyle.Fill };
                _tabSearch.Controls.Clear();
                _tabSearch.Controls.Add(search);
                search.Show();

                // Embed ConfigForm
                var config = new ConfigForm { Embedded = true, TopLevel = false, Dock = DockStyle.Fill };
                _tabConfig.Controls.Clear();
                _tabConfig.Controls.Add(config);
                config.Show();
            }
            catch { }
        }
    }
}


