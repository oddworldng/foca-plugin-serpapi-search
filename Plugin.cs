using System;
using System.IO;
using System.Windows.Forms;
#if FOCA_API
using PluginsAPI;
using PluginsAPI.Elements;
#endif

namespace Foca.SerpApiSearch
{
	// Standalone interface (used when compiled without FOCA host)
	public interface IFocaPlugin
	{
		string Name { get; }
		string Description { get; }
		string Author { get; }
		string Version { get; }
		void Initialize();
	}

    public sealed class SerpApiSearchPlugin : IFocaPlugin
	{
		public string Name => "FOCA SerpApi Search";
		public string Description => "Búsqueda avanzada de documentos vía SerpApi";
		public string Author => "Andrés Nacimiento";
		public string Version => "1.0.0";

		public void Initialize()
		{
			// No-op for library mode
			Application.ApplicationExit += (s, e) => { };
		}
	}
}

#if FOCA_API
namespace Foca
{
	using System.Drawing;
    using Foca.SerpApiSearch;
    using Foca.SerpApiSearch.Ui;
    using Ui = Foca.SerpApiSearch.Ui;

	internal static class PluginDiag
	{
        private static readonly string LogPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocaSerpApiSearch.plugin.log");
		public static void Log(string message)
		{
			try { File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + message + Environment.NewLine); } catch { }
		}
	}

    public class Plugin
	{
		private string _name = "Búsqueda avanzada";
		private string _description = "Búsqueda de documentos (SerpApi)";
		private readonly Export export;

		public Export exportItems { get { return this.export; } }

		public string name
		{
			get { return this._name; }
			set { this._name = value; }
		}

		public string description
		{
			get { return this._description; }
			set { this._description = value; }
		}

        public Plugin()
		{
			try
			{
				PluginDiag.Log("Plugin ctor start");
                // Inicializar el resolver y forzar el cctor de EarlyBinder para asegurar el AssemblyResolve
                Foca.SerpApiSearch.AssemblyResolver.Init();
                Foca.SerpApiSearch.EarlyBinder.Touch();
				this.export = new Export();

                var hostPanel = new Panel { Dock = DockStyle.Fill, Visible = false };
                var pluginPanel = new PluginPanel(hostPanel, false);
                this.export.Add(pluginPanel);
				PluginDiag.Log("PluginPanel added");

				var root = new ToolStripMenuItem(this._name);

				// Cargar icono si existe (opcional)
				TryLoadIcon(root);

                // Submenú Configuración
                var miConfig = new ToolStripMenuItem("Configuración");
				miConfig.Click += (s, e) =>
				{
					try
					{
						using (var dlg = new Ui.ConfigForm())
						{
							dlg.ShowDialog();
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Error al abrir 'Configuración de SerpApi': {ex.Message}",
							"Búsqueda avanzada", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				};

				// Submenú Buscar
                var miBuscar = new ToolStripMenuItem("Buscar");
				miBuscar.Click += (s, e) =>
				{
					try
					{
						using (var dlg = new Ui.SearchForm())
						{
							dlg.ShowDialog();
						}
					}
					catch (Exception ex)
					{
						MessageBox.Show($"Error al abrir 'Buscar': {ex.Message}",
							"Búsqueda avanzada", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				};

				root.DropDownItems.Add(miConfig);
				root.DropDownItems.Add(miBuscar);

				var pluginMenu = new PluginToolStripMenuItem(root);
				this.export.Add(pluginMenu);
				PluginDiag.Log("Menu added");
            }
            catch (Exception ex)
			{
                // Igual que en foca-excel-export: log y relanzar para que FOCA muestre el error
                PluginDiag.Log("Plugin ctor error: " + ex.Message);
                throw;
			}
		}

		private static void TryLoadIcon(ToolStripMenuItem root)
		{
			try
			{
				string asmDir = Path.GetDirectoryName(typeof(Plugin).Assembly.Location) ?? AppDomain.CurrentDomain.BaseDirectory;
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string[] candidates = new[]
				{
					Path.Combine(asmDir, "img", "icon.png"),
					Path.Combine(baseDir, "img", "icon.png"),
					Path.Combine(asmDir, "icon.png"),
					Path.Combine(baseDir, "icon.png")
				};
				foreach (var p in candidates)
				{
					try
					{
						if (File.Exists(p))
						{
							using (var fs = File.OpenRead(p))
							{
								root.Image = Image.FromStream(fs);
								PluginDiag.Log($"Icon loaded from: {p}");
								return;
							}
						}
					}
					catch (Exception ex)
					{
						PluginDiag.Log($"Failed to load icon from {p}: {ex.Message}");
					}
				}

				// Fallback: embedded resource
				try
				{
                    using (var stream = typeof(Plugin).Assembly.GetManifestResourceStream("Foca.SerpApiSearch.img.icon.png"))
					{
						if (stream != null)
						{
							root.Image = Image.FromStream(stream);
                            PluginDiag.Log("Icon loaded from embedded resource: Foca.SerpApiSearch.img.icon.png");
						}
						else
						{
                            PluginDiag.Log("Embedded icon resource not found: Foca.SerpApiSearch.img.icon.png");
						}
					}
				}
				catch (Exception ex)
				{
					PluginDiag.Log("Failed to load embedded icon: " + ex.Message);
				}
			}
			catch { }
		}
	}
}
#endif
