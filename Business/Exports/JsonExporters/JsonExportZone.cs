using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PKServ.Business.Exports.JsonExporters
{
    /// <summary>
    /// Génère les JSON pour les zones : zones_list.json
    /// </summary>
    public static class JsonExportZone
    {
        public static void ExportZonesList(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var list = settings.Zones.Select(z => BuildZoneJson(z, settings)).ToList();
            string path = Path.Combine("WebExport", "Data", "json", "zones_list.json");
            File.WriteAllText(path, JsonSerializer.Serialize(list, StaticFileCopier.GetOptions()));
        }

        private static object BuildZoneJson(Zone z, AppSettings settings)
        {
            var creaturesInZone = settings.allPokemons
                .Where(p => p.enabled && !p.isLock &&
                    (!p.IsZoneExclusive || p.ZonesList.Any(pz => string.Equals(pz.Name, z.Name, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            int exclusiveCount = creaturesInZone.Count(p => p.IsZoneExclusive && p.ZonesList.Count == 1 &&
                p.ZonesList.Any(pz => string.Equals(pz.Name, z.Name, StringComparison.OrdinalIgnoreCase)));

            return new
            {
                z.Name,
                z.Description,
                z.Region,
                z.Image,
                z.DexRequirement,
                z.LevelRequirement,
                CreatureCount = creaturesInZone.Count,
                ExclusiveCount = exclusiveCount
            };
        }
    }
}
