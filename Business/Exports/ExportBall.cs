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
    public static class ExportBall
    {
        public static async Task ExportIndividualBall(Pokeball ball, GlobalAppSettings globalAppSettings)
        {
            string fileContent = "";
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
                <img class=""sprite"" src=""{ball.sprite}"" alt=""Image"">
            </div>
            <div class=""col-md-8 info-container"">
                <h2>Informations</h2>
                <ul class=""list-group"">
                    <li class=""list-group-item bg-dark text-white""><strong>Nom :</strong> {ball.Name}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Taux de capture de base :</strong> {ball.catchrate}%</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Taux de shiny de base :</strong> {ball.shinyrate}%</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Taux de capture bonus / 100 poke dans le dex :</strong> +{ball.dexRelativeBonusCatchrate}%</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Taux de shiny bonus / 100 poke dans le dex :</strong> +{ball.dexRelativeBonusShinyrate}%</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Reroll pour le dex :</strong> +{ball.rerollItemForUncaught}%</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Série de pokémon exclusif :</strong> {(ball.exclusiveSerie is not null ? $"Oui : {ball.exclusiveSerie}" : "Non.")}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Type exclusif :</strong> {(ball.exclusiveType is not null ? $"Oui : <img src={TypeBinding.GetImageUrl(ball.exclusiveType)} style='width:128px; height:24px;'>" : "Non.")}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Zone forcée :</strong> {(ball.exclusiveZone is not null ? $"Oui : {ball.exclusiveZone}" : "Non.")}</li>
                </ul>
            </div>
        </div>
    </div>
";
                string filename = $"{ball.Name}.html";
                fileContent = Commun.DefaultHTMLStart(true, $"StreamDex > {ball.Name}") + fileContent + Commun.DefaultHTMLEnd();
                await File.WriteAllTextAsync(Path.Combine("WebExport", "Ball", filename), fileContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while exporting individual file for {ball.Name}: {ex.Message}");
            }
        }
    }
}