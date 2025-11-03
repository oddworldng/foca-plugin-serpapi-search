using System;
using System.IO;
using System.Reflection;

namespace Foca.SerpApiSearch
{
    internal static class AssemblyResolver
    {
        private static bool _initialized;

        public static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromPluginFolder;
        }

        private static Assembly ResolveFromPluginFolder(object sender, ResolveEventArgs args)
        {
            try
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                var baseDir = Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location);
                var probePaths = new[]
                {
                    baseDir,
                    Path.Combine(baseDir ?? string.Empty, "lib"),
                };

                foreach (var dir in probePaths)
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    var candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate))
                        return Assembly.LoadFrom(candidate);
                }
            }
            catch { }
            return null;
        }
    }
}

// Inicialización temprana sin ModuleInitializer (C# 7.3):
// usamos un tipo estático con constructor estático. El CLR garantiza que
// el cctor se ejecuta antes del primer acceso a cualquier miembro del tipo
// y también durante la reflexión de tipos en la mayoría de escenarios.
namespace Foca.SerpApiSearch
{
    internal static class EarlyBinder
    {
        static EarlyBinder()
        {
            try
            {
                AssemblyResolver.Init();
                try
                {
                    var p = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FocaSerpApiSearch.plugin.log");
                    System.IO.File.AppendAllText(p, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + "Early binder executed" + Environment.NewLine);
                }
                catch { }
            }
            catch { }
        }

        // Método de referencia para forzar JIT del tipo desde el ctor del plugin
        public static void Touch() { }
    }
}


