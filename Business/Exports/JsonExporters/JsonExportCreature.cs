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
    /// Génère les JSON pour les créatures : creatures_list.json
    /// </summary>
    public static class JsonExportCreature
    {
        public static void ExportCreaturesList(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var list = settings.allPokemons.Select(p => BuildCreatureJson(p, settings, globalAppSettings)).ToList();

            string path = Path.Combine("WebExport", "Data", "json", "creatures_list.json");
            File.WriteAllText(path, JsonSerializer.Serialize(list, StaticFileCopier.GetOptions()));
        }

        private static object BuildCreatureJson(Pokemon p, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            int defaultValueNormal = p.isLegendary
                ? globalAppSettings.ScrapSettings.ValueDefaultNormal * globalAppSettings.ScrapSettings.legendaryMultiplier
                : globalAppSettings.ScrapSettings.ValueDefaultNormal;
            int defaultValueShiny = p.isLegendary
                ? globalAppSettings.ScrapSettings.ValueDefaultShiny * globalAppSettings.ScrapSettings.legendaryMultiplier
                : globalAppSettings.ScrapSettings.ValueDefaultShiny;

            return new
            {
                p.Name_FR,
                p.Name_EN,
                p.AltName,
                p.Sprite_Normal,
                p.Sprite_Shiny,
                p.Serie,
                p.Rarity,
                p.enabled,
                p.isLegendary,
                p.isCustom,
                p.isShinyLock,
                p.isLock,
                p.IsZoneExclusive,
                Type1Url = p.Type1 != null ? TypeBinding.GetImageUrl(p.Type1) : null,
                Type2Url = p.Type2 != null ? TypeBinding.GetImageUrl(p.Type2) : null,
                CatchRate = (object)null,
                ShinyRate = (object)null,
                PriceNormal = p.priceNormal,
                PriceShiny = p.priceShiny,
                ValueNormal = p.valueNormal ?? defaultValueNormal,
                ValueShiny = p.valueShiny ?? defaultValueShiny,
                Zones = p.ZonesList?.Select(z => new
                {
                    name = z.Name,
                    dexRequired = z.DexRequirement,
                    levelRequired = z.LevelRequirement
                }).ToList(),
                Artists = p.Artist?.Select(a => new { name = a.ArtistName, link = a.ArtistLink }).ToList()
            };
        }
    }
}
