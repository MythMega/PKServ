using PKServ.Binding;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Business.Exports.JsonExporters
{
    /// <summary>
    /// Copie les fichiers HTML/CSS/JS statiques dans WebExport (toujours, même si plus récents).
    /// </summary>
    public static class StaticFileCopier
    {
        private static readonly string WebExportDir = "WebExport";
        private static readonly string AssetsSourceDir = Path.Combine("StaticWeb", "assets");

        // Options partagées — créées une seule fois
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = null
        };

        public static JsonSerializerOptions GetOptions() => _options;

        /// <summary>
        /// Sérialise <paramref name="obj"/> et écrit le fichier JSON de façon asynchrone.
        /// </summary>
        public static async Task WriteJsonAsync(string filename, object obj)
        {
            string path = Path.Combine(WebExportDir, "Data", "json", filename);
            string json = JsonSerializer.Serialize(obj, _options);
            await File.WriteAllTextAsync(path, json);
        }

        /// <summary>
        /// Copie tous les fichiers statiques depuis StaticWeb/ vers WebExport/.
        /// Écrase toujours les fichiers existants.
        /// </summary>
        public static void CopyAll()
        {
            string sourceDir = "StaticWeb";
            if (!Directory.Exists(sourceDir)) return;
            CopyDirectory(sourceDir, WebExportDir);
        }

        private static void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (string file in Directory.GetFiles(source))
            {
                string destFile = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }
            foreach (string dir in Directory.GetDirectories(source))
            {
                string destSubDir = Path.Combine(dest, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        /// <summary>
        /// Assure que les dossiers JSON de données existent.
        /// </summary>
        public static void EnsureDataDirectories()
        {
            Directory.CreateDirectory(Path.Combine(WebExportDir, "Data", "json"));
            Directory.CreateDirectory(Path.Combine(WebExportDir, "Data", "json", "users"));
        }
    }
}
