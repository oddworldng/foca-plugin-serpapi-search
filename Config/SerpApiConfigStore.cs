using System;
using System.IO;
using Newtonsoft.Json;

namespace Foca.SerpApiSearch.Config
{
    /// <summary>
    /// Persists SerpApi settings under %APPDATA%\FOCA\Plugins\SerpApiSearch\config.json
    /// </summary>
    public static class SerpApiConfigStore
    {
        public static string BaseDirectory
        {
            get
            {
                var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appdata, "FOCA", "Plugins", "SerpApiSearch");
            }
        }

        public static string ConfigPath => Path.Combine(BaseDirectory, "config.json");

        public static SerpApiSettings Load()
        {
            try
            {
                if (!File.Exists(ConfigPath)) return new SerpApiSettings();
                var json = File.ReadAllText(ConfigPath);
                var obj = JsonConvert.DeserializeObject<SerpApiSettings>(json) ?? new SerpApiSettings();
                return obj;
            }
            catch
            {
                return new SerpApiSettings();
            }
        }

        public static void Save(SerpApiSettings settings)
        {
            try
            {
                Directory.CreateDirectory(BaseDirectory);
                var json = JsonConvert.SerializeObject(settings ?? new SerpApiSettings(), Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }
    }
}


