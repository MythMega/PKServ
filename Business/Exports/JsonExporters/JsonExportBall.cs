using PKServ.Binding;
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
    /// Génère les JSON pour les balls : balls_list.json
    /// </summary>
    public static class JsonExportBall
    {
        public static void ExportBallsList(AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            var list = settings.pokeballs.Select(b => new
            {
                b.Name,
                Sprite = b.sprite,
                CatchRate = b.catchrate,
                ShinyRate = b.shinyrate,
                DexBonusCatch = b.dexRelativeBonusCatchrate,
                DexBonusShiny = b.dexRelativeBonusShinyrate,
                RerollUncaught = b.rerollItemForUncaught,
                ExclusiveSerie = b.exclusiveSerie,
                ExclusiveZone = b.exclusiveZone,
                ExclusiveTypeUrl = b.exclusiveType != null ? TypeBinding.GetImageUrl(b.exclusiveType) : null
            }).ToList();

            string path = Path.Combine("WebExport", "Data", "json", "balls_list.json");
            File.WriteAllText(path, JsonSerializer.Serialize(list, StaticFileCopier.GetOptions()));
        }
    }
}
