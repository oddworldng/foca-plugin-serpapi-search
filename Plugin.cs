using System;
using System.IO;
using System.Windows.Forms;
#if FOCA_API
using PluginsAPI;
using PluginsAPI.Elements;
using System.Linq;
using System.Reflection;
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

#if FOCA_API
    internal static class PluginLogger
    {
        private static readonly object _progressLock = new object();
        private static string _lastProgressMessage;
        private static readonly Lazy<LoggerBinding> _binding = new Lazy<LoggerBinding>(ResolveBinding);
        private static bool _debugEnabledCached = false;
        private static DateTime _debugCacheTimestamp = DateTime.MinValue;

        private sealed class LoggerBinding
        {
            public MethodInfo LogThis { get; set; }
            public Type LogType { get; set; }
            public Type ModuleEnum { get; set; }
            public Type LogLevelEnum { get; set; }
            public object ModuleWebSearch { get; set; }
            public object LevelLow { get; set; }
            public object LevelMedium { get; set; }
            public object LevelHigh { get; set; }
            public object LevelError { get; set; }
        }

        private static LoggerBinding ResolveBinding()
        {
            try
            {
                var focaAssembly = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, "FOCA", StringComparison.OrdinalIgnoreCase));
                if (focaAssembly == null) return null;

                var logType = focaAssembly.GetType("FOCA.Log");
                var moduleEnum = logType?.GetNestedType("ModuleType");
                var logEnum = logType?.GetNestedType("LogType");
                var programType = focaAssembly.GetType("FOCA.Program");
                var logThis = programType?.GetMethod("LogThis", BindingFlags.Public | BindingFlags.Static);

                if (logType == null || moduleEnum == null || logEnum == null || logThis == null)
                    return null;

                object ParseEnum(Type enumType, string name)
                {
                    try { return Enum.Parse(enumType, name, true); }
                    catch { return null; }
                }

                var moduleWebSearch = ParseEnum(moduleEnum, "WebSearch");
                var levelLow = ParseEnum(logEnum, "low");
                var levelMedium = ParseEnum(logEnum, "medium");
                var levelHigh = ParseEnum(logEnum, "high");
                var levelError = ParseEnum(logEnum, "error");

                if (moduleWebSearch == null || levelLow == null || levelMedium == null || levelHigh == null || levelError == null)
                    return null;

                return new LoggerBinding
                {
                    LogThis = logThis,
                    LogType = logType,
                    ModuleEnum = moduleEnum,
                    LogLevelEnum = logEnum,
                    ModuleWebSearch = moduleWebSearch,
                    LevelLow = levelLow,
                    LevelMedium = levelMedium,
                    LevelHigh = levelHigh,
                    LevelError = levelError
                };
            }
            catch
            {
                return null;
            }
        }

        private static void Write(string message, object levelValue, string fallbackLevel)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            var binding = _binding.Value;
            if (binding != null)
            {
                try
                {
                    var logEntry = Activator.CreateInstance(binding.LogType, binding.ModuleWebSearch, message, levelValue);
                    binding.LogThis.Invoke(null, new[] { logEntry });
                    return;
                }
                catch
                {
                    // si falla, hacemos fallback
                }
            }

            try { System.Diagnostics.Debug.WriteLine($"[SerpApiSearch][{fallbackLevel}] {message}"); } catch { }
        }

        private static bool IsDebugEnabled()
        {
            var now = DateTime.UtcNow;
            if ((now - _debugCacheTimestamp).TotalSeconds > 5)
            {
                try
                {
                    _debugEnabledCached = Config.SerpApiConfigStore.Load()?.DebugMode ?? false;
                }
                catch
                {
                    _debugEnabledCached = false;
                }
                _debugCacheTimestamp = now;
            }
            return _debugEnabledCached;
        }

        public static void Info(string message) => Write(message, _binding.Value?.LevelMedium, "INFO");
        public static void Debug(string message)
        {
            if (!IsDebugEnabled()) return;
            Write(message, _binding.Value?.LevelLow, "DEBUG");
        }
        public static void Error(string message) => Write(message, _binding.Value?.LevelError, "ERROR");
        public static void Success(string message) => Write(message, _binding.Value?.LevelHigh, "SUCCESS");

        public static void Progress(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            lock (_progressLock)
            {
                if (string.Equals(message, _lastProgressMessage, StringComparison.OrdinalIgnoreCase)) return;
                _lastProgressMessage = message;
            }
            Debug(message);
        }

        public static void ResetProgressCache()
        {
            lock (_progressLock) { _lastProgressMessage = null; }
            _debugCacheTimestamp = DateTime.MinValue;
        }
    }
#else
    internal static class PluginLogger
    {
        private static void Write(string level, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            try { System.Diagnostics.Debug.WriteLine($"[SerpApiSearch][{level}] {message}"); } catch { }
        }

        public static void Info(string message) => Write("INFO", message);
        public static void Debug(string message) => Write("DEBUG", message);
        public static void Error(string message) => Write("ERROR", message);
        public static void Success(string message) => Write("SUCCESS", message);
        public static void Progress(string message) => Debug(message);
        public static void ResetProgressCache() { }
    }
#endif

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

                // Cargar UI integrada en el panel (estilo iframe)
                var main = new Ui.MainControl { Dock = DockStyle.Fill };
                hostPanel.Controls.Add(main);
                hostPanel.Visible = true;

                var root = new ToolStripMenuItem(this._name);

                // Cargar icono si existe (opcional)
                TryLoadIcon(root);

                // Asegurar que al hacer clic se muestre nuestro panel embebido
                root.Click += (s, e) =>
                {
                    try { hostPanel.Visible = true; hostPanel.BringToFront(); } catch { }
                };

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
