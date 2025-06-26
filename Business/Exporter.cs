using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class Exporter
    {
        /// <summary>
        /// Lors d'un export de FULL DEX, exporte les dex solo puis le main
        /// </summary>
        /// <returns></returns>
        public static string DoFullExport(DataConnexion connexion, AppSettings appSettings, GlobalAppSettings globalAppSettings, bool forced = true)
        {
            List<User> users = connexion.GetAllUserPlatforms();

            // si ce n'est pas forcé par le management, on export uniquement les dex avec maj recente
            if (!forced)
            {
                users = users.Where(user => user.lastCatch() > appSettings.LastFullExport).ToList();
            }

            // on ajoute les users à exporter, y compris ceux qui n'ont pas "capturer", mais fais des actions modifiant le dex
            appSettings.UsersToExport.ForEach(user =>
            {
                if (!users.Where(u => u.Code_user == user.Code_user || (u.Pseudo == user.Pseudo && u.Platform == user.Platform)).Any())
                {
                    users.Add(user);
                }
            }
            );
            appSettings.UsersToExport = [];

            int count = 0;

            // Configurer le parallélisme sur 8 threads maximum
            ParallelOptions options = new ParallelOptions { MaxDegreeOfParallelism = 8 };

            Parallel.ForEach(users, options, user =>
            {
                // Instancier ExportSoloDex pour cet utilisateur
                var data = new ExportSoloDex(appSettings, user, connexion, globalAppSettings);

                // Définir le nom du fichier
                data.filename = $"{user.Pseudo}.html";

                // Attendre la fin de l'opération d'export
                data.ExportFile();

                // Incrémenter le compteur de manière thread-safe
                Interlocked.Increment(ref count);
            });
            // on exporte le main
            var export = new ExportMain(appSettings, connexion, globalAppSettings);
            export.filename = "main.html";
            export.ExportFile().Wait();

            appSettings.LastFullExport = DateTime.Now;

            return $"Export done. {count}/{users.Count} personal files created + main file created.";
        }
    }
}