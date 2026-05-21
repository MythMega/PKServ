using PKServ.Business.Exports.JsonExporters;
using PKServ.Business.Exports.JsonExporters;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class Exporter
    {
        /// <summary>
        /// Lors d'un export de FULL DEX, exporte les dex solo puis le main.
        /// Les exports utilisateurs sont parallélisés, les exports JSON statiques aussi.
        /// </summary>
        public static string DoFullExport(DataConnexion connexion, AppSettings appSettings, GlobalAppSettings globalAppSettings, bool forced = true)
        {
            List<User> users = connexion.GetAllUserPlatforms();

            if (!forced)
            {
                // OPTIM P4 : anciennement, user.lastCatch() était appelé pour chaque utilisateur
                // dans le Where() — chaque appel ouvrait une connexion SQLite et chargeait toutes
                // les entrées de l'utilisateur. Pour N utilisateurs = N aller-retours SQL.
                // GetLastCatchPerUser() récupère les MAX(DataLastCatch) de TOUS les utilisateurs
                // en une seule requête GROUP BY → 1 connexion quelle que soit la taille de la base.
                var lastCatchMap = connexion.GetLastCatchPerUser();
                users = users.Where(user =>
                {
                    string key = $"{user.Pseudo}|{user.Platform}";
                    return lastCatchMap.TryGetValue(key, out DateTime lc) && lc > appSettings.LastFullExport;
                }).ToList();
            }

            foreach (var user in appSettings.UsersToExport)
            {
                bool alreadyIncluded = users.Any(u =>
                    u.Code_user == user.Code_user ||
                    (u.Pseudo == user.Pseudo && u.Platform == user.Platform));
                if (!alreadyIncluded)
                    users.Add(user);
            }

            if (users.Count == 0)
                return "No Export required.";

            appSettings.UsersToExport = [];

            // ── Exports utilisateurs en parallèle ────────────────────────────
            var userTasks = users.Select(u =>
                JsonExportUser.ExportUserAsync(u, connexion, appSettings, globalAppSettings));
            Task.WhenAll(userTasks).Wait();

            // ── users_by_platform.json ────────────────────────────────────────
            JsonExportUser.ExportUsersByPlatform(connexion.GetAllUserPlatforms());

            // ── Exports JSON statiques en parallèle ───────────────────────────
            StaticFileCopier.EnsureDataDirectories();
            Task.WhenAll(
                JsonExportPages.ExportMainAsync(connexion, appSettings, globalAppSettings),
                JsonExportPages.ExportPokeStatsAsync(connexion, appSettings, globalAppSettings),
                JsonExportPages.ExportRecordsAsync(connexion, appSettings),
                JsonExportPages.ExportBuyListAsync(appSettings, globalAppSettings),
                JsonExportPages.ExportScrapListAsync(appSettings, globalAppSettings),
                JsonExportPages.ExportCommandGeneratorDataAsync(appSettings, globalAppSettings),
                JsonExportPages.ExportRankingsAsync(connexion, appSettings, globalAppSettings),
                Task.Run(() => JsonExportCreature.ExportCreaturesList(appSettings, globalAppSettings)),
                Task.Run(() => JsonExportBall.ExportBallsList(appSettings, globalAppSettings)),
                Task.Run(() => JsonExportZone.ExportZonesList(appSettings, globalAppSettings))
            ).Wait();

            appSettings.LastFullExport = DateTime.Now;

            return $"Export done. {users.Count} user JSON files + static JSONs updated.";
        }
    }
}
