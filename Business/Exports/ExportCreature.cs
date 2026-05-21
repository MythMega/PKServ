using PKServ.Binding;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Exports
{
    public static class ExportCreature
    {
        public static async Task ExportIndividualCreature(Pokemon creature, GlobalAppSettings globalAppSettings)
        {
            string fileContent = "";
            string availability = getPokeAvailability(poke: creature);
            string type = creature.Type1 is not null || creature.Type2 is not null ? "" : "(no infos)";
            string spawn = !creature.enabled ? "????????" : !creature.ZonesNames.Any() ?
                @$"<a href=""../Zone/_void_.html"">void</a>" :
                getPokeZoneNameAndLink(creature.ZonesList);
            string infos = "";
            if (creature.isLegendary)
                infos += "Légendaire; ";
            if (creature.isCustom)
                infos += "Custom; ";
            if (creature.Serie is not null)
                infos += $"<Série : {creature.Serie};> ";
            if (creature.Type1 != null)
            {
                type += $"<img class=\"type\" src=\"{TypeBinding.GetImageUrl(creature.Type1)}\">";
            }
            if (creature.Type2 != null)
            {
                type += $"<img class=\"type\" src=\"{TypeBinding.GetImageUrl(creature.Type2)}\">";
            }

            int defaultValueNormal = creature.isLegendary ? globalAppSettings.ScrapSettings.ValueDefaultNormal * globalAppSettings.ScrapSettings.legendaryMultiplier : globalAppSettings.ScrapSettings.ValueDefaultNormal;
            int defaultValueShiny = creature.isLegendary ? globalAppSettings.ScrapSettings.ValueDefaultShiny * globalAppSettings.ScrapSettings.legendaryMultiplier : globalAppSettings.ScrapSettings.ValueDefaultShiny;

            string prix = $"<br>Normal <img class=\"icon\" src=\"{ShinyBinding.GetIcon(false)}\"> : {(creature.priceNormal?.ToString() ?? "N/A")} <br> Shiny <img class=\"icon\" src=\"{ShinyBinding.GetIcon(true)}\"> : {(creature.priceShiny?.ToString() ?? "N/A")}";
            string value = $"<br>Normal <img class=\"icon\" src=\"{ShinyBinding.GetIcon(false)}\"> : {(creature.valueNormal?.ToString() ?? defaultValueNormal.ToString())} <br> Shiny <img class=\"icon\" src=\"{ShinyBinding.GetIcon(true)}\"> : {(creature.valueShiny?.ToString() ?? defaultValueShiny.ToString())}";

            string filename = $"{creature.Name_FR}.html";
            string artisteInfos = string.Join("; ", creature.Artist.Select(a => $"<a href=\"{a.ArtistLink}\"> {a.ArtistName}</a>")); ;
            try
            {
                fileContent = $@"
    <style>
        .image-container {{
            flex: 1;
            display: flex;
            justify-content: center;
            align-items: center;
        }}
        .info-container {{
            flex: 1;
            padding: 20px;
        }}
        .sprite {{
            width: 100%; /* Prend toute la largeur du conteneur */
            height: auto;
            image-rendering: pixelated;          /* Pour Chrome, Edge, etc. */
            image-rendering: -moz-crisp-edges;     /* Pour Firefox */
            image-rendering: crisp-edges;          /* Alternative pour certains navigateurs */
            -ms-interpolation-mode: nearest-neighbor; /* Pour IE */
        }}
        .type {{
            max-height: 20px;
            width: auto;
        }}
        a {{
            textDecoration = 'none';
        }}
        .icon {{
            max-height: 16px;
            width: auto;
        }}
    </style>
    <div class=""container mt-5"">
        <div class=""row"">
            <div class=""col-md-4 image-container"">
                <img class=""sprite"" src=""{creature.Sprite_Normal}"" alt=""Image"">
            </div>
            <div class=""col-md-8 info-container"">
                <h2>Informations</h2>
                <ul class=""list-group"">
                    <li class=""list-group-item bg-dark text-white""><strong>Nom :</strong> {creature.Name_FR}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Name :</strong> {creature.Name_EN}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Type :</strong> {type}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Spawn :</strong> {spawn}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Availability :</strong> {availability}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Artist :</strong> {artisteInfos}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Infos :</strong> {infos}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Buy Price / Prix d'achat :</strong> {prix}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Value / Valeur :</strong> {value}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Rareté :</strong> {CreatureRarity.IconHTML(creature.Rarity, IconSize.Medium)}</li>
                </ul>
            </div>
        </div>
    </div>
";

                fileContent = Commun.DefaultHTMLStart(true, $"StreamDex > {creature.AltName}") + fileContent + Commun.DefaultHTMLEnd();
                await File.WriteAllTextAsync(Path.Combine("WebExport", "Creature", filename), fileContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while exporting individual file for {creature.Name_FR}/{creature.Name_EN}: {ex.Message}");
            }
        }

        private static string getPokeAvailability(Pokemon poke)
        {
            if (!poke.enabled)
            {
                return "Not available at all.";
            }
            else if (poke.isLock)
            {
                return "only under distribution / events.";
            }
            else
            {
                return "fully available";
            }
        }

        private static string getPokeZoneNameAndLink(List<Zone> zonesList)
        {
            string r = "";
            foreach (Zone zone in zonesList)
            {
                r += $"<a href=\"../Zone/{Commun.CleanFileName(zone.Name)}.html\">{zone.Name}</a>; ";
            }
            return r.TrimEnd(' ', ';').TrimStart(' ', ';'); // Enlève le dernier espace et le point-virgule
        }
    }
}