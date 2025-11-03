#if FOCA_API
using PluginsAPI;
#endif

namespace Foca.ExportImport
{
#if !FOCA_API
    // Interfaz mínima para compilar fuera de FOCA. En FOCA usamos la del host.
    public interface IFocaPlugin
    {
        string Name { get; }
        string Description { get; }
        string Author { get; }
        string Version { get; }
        void Initialize();
    }
#endif

    public sealed class FocaSerpApiSearchPlugin : 
#if FOCA_API
        PluginsAPI.IFocaPlugin
#else
        IFocaPlugin
#endif
    {
        public string Name => "FOCA SerpApi Search";
        public string Description => "Búsqueda avanzada de documentos via SerpApi";
        public string Author => "Andrés Nacimiento";
        public string Version => "1.0.0";

        public void Initialize()
        {
            // Igual que foca-excel-export: inicializa el resolver antes de dependencias
            Foca.SerpApiSearch.AssemblyResolver.Init();
            // Forzar cctor de EarlyBinder para asegurar el hook y dejar rastro en log local
            Foca.SerpApiSearch.EarlyBinder.Touch();
            System.Windows.Forms.Application.ApplicationExit += (s, e) => { };
        }
    }
}


