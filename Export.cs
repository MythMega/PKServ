using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKServ
{
    internal class ExportSoloDex : Export
    {
        public ExportSoloDex(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings)
        {
            BuildRapport();
        }

        public async Task<string> UploadFileAsync(GlobalAppSettings gas)
        {
            string filepath = Path.Combine("ExportsSimple", this.filename);
            return await Commun.UploadFileAsync(filepath, gas, "ExportSimples");
        }

        public List<string> getLineTables()
        {
            List<string> lineTables = new List<string>();
            string currline = "";
            Pokemon currPoke;
            List<Entrie> entriesByPseudo = dataConnexion.GetEntriesByPseudo(userRequest.UserName, userRequest.Platform);
            foreach (Entrie en in entriesByPseudo)
            {
                en.setIDPoke(appSettings);
            }
            entriesByPseudo = entriesByPseudo.OrderBy(e => e.entryPokeID).ToList();
            foreach (Entrie item in entriesByPseudo)
            {
                string classShiny = string.Empty;
                string classNormal = string.Empty;

                classShiny = item.CountShiny > 0 ? "" : $@"class = ""all-black"" ";
                classNormal = item.CountNormal > 0 ? "" : $@"class = ""all-black"" ";

                currPoke = appSettings.pokemons.Where(poke => poke.Name_FR == item.PokeName).FirstOrDefault();
                if (currPoke == null)
                {
                    Console.WriteLine($"WARN Le pokémon {item.PokeName} (possédé par {item.Pseudo}) n'a pas été trouvé dans la liste des pokémon activés");
                }
                else
                {
                    currline = @$"
<tr>
                <td class=""pokename"">{item.PokeName}</td>
                <td><img {classNormal}src=""{currPoke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td class=""count"">{item.CountNormal}</td>
                <td><img {classShiny}src=""{currPoke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td class=""count"">{item.CountShiny}</td>
                <td>{item.dateFirstCatch}</td>
            </tr>
";
                    lineTables.Add(currline);
                }
            };
            return lineTables;
        }

        /// <summary>
        /// SOLO DEX
        /// </summary>
        public void BuildRapport()
        {
            filename = $"Dex_{userRequest.UserName}_export_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.html";

            List<string> lineTables = getLineTables();

            fileContent = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Pokémon Capture Tracker</title>
    <!-- Bootstrap CSS -->
    <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"" rel=""stylesheet"">
    <link href=""https://unpkg.com/aos@2.3.1/dist/aos.css"" rel=""stylesheet"">

    <script>
  AOS.init();
    </script>
    <style>
        body {{
            background-color: #2a2a2a;
            color: #ffffff;
            padding: 20px;
        }}
        .pokename {{
        font-size: 30px;
        }}
        .table tbody td img {{
            height: 64px;
            width: auto;
        }}
        .count {{font-size: 40px;
        }}
        /* Noir et blanc */
        .black-and-white {{filter: grayscale(100%);
          -webkit-filter: grayscale(100%);
        }}

        /* Tout noir (seulement la forme) */
        .all-black {{filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }}
        /* Texte plus grand dans <td> */
        .large-text td {{font - size: 20px; }}

        .container-badge {{max - width: 500px;
            }}

        .trophy-True {{
            height: 64px;
            width: 64px;
        }}

        .trophy-False {{
            height: 64px;
            width: 64px;
            filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }}

        .uncommon {{box-shadow: inset 0 0 4px lime;
        }}

        .rare {{box-shadow: inset 0 0 8px aqua;
        }}

        .epic {{box-shadow: inset 0 0 12px #7D0DC3;
        }}

        .legendary {{box-shadow: inset 0 0 16px gold;
        }}

        .exotic {{box-shadow: inset 0 0 20px pink;
        }}

        th, td {{ text-align: center; border: 1px solid black; padding: 10px; }}

    </style>
</head>
<body>
    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""../main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../pokestats.html"" style=""color: white;"">Classements</a>
      </form>
    </nav><br><br>
    <h1>Pokédex {userRequest.UserName} - chez {userRequest.ChannelSource}</h1>
    <p>Pokédex de {userRequest.UserName} [de {userRequest.Platform}] au {DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}, sur le stream de {userRequest.ChannelSource}.</p>
    <table class=""table table-dark table-bordered table-striped"">
        <thead class=""thead-light"">
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Capturé(s)</th>
                <th>Sprite Shiny</th>
                <th>Capturé(s)</th>
                <th>Première capture(s)</th>
            </tr>
        </thead>
        <tbody>";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
<p>Trainer Card :</p><br>
{GetUserCards()}
<br><br>
<p>Stats :</p><br>
{GetUserStats()}
<p>Badges :</p><br>
{GetUserBadge(appSettings)}
    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
    <script src=""https://unpkg.com/aos@2.3.1/dist/aos.js""></script>
</body>
</html>";
        }
    }

    internal class ExportRapport : Export
    {
        public ExportRapport(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings)
        {
            BuildDoc();
        }

        /// <summary>
        /// MAIN.HTML
        /// </summary>
        public void BuildDoc()
        {
            List<User> utilisateurs = dataConnexion.GetAllUserPlatforms();
            string dataPseudoList = string.Join(", ", utilisateurs.Select(x => x.Pseudo));
            int NombreTotalPokeball = 0;
            int NombreTotalSousouDepense = 0;
            int NombreTotalPokecapture = 0;
            int NombreTotalShinycapture = 0;
            int nombreShinyDistribue = 0;
            int nombreNormalDistribue = 0;

            utilisateurs.ForEach(u => NombreTotalPokeball += u.Stats.ballLaunched);
            utilisateurs.ForEach(u => NombreTotalSousouDepense += u.Stats.moneySpent);
            utilisateurs.ForEach(u => NombreTotalPokecapture += u.Stats.pokeCaught);
            utilisateurs.ForEach(u => NombreTotalShinycapture += u.Stats.shinyCaught);
            utilisateurs.ForEach(u => nombreShinyDistribue += u.Stats.giveawayShiny);
            utilisateurs.ForEach(u => nombreNormalDistribue += u.Stats.giveawayNormal);

            // -ceux qui lancent le plus de ball
            string classementLanceurDeBall = "stat non initialisée";

            // -ceux qui ont le plus dépensé
            string classementDepenseur = "stat non initialisée";

            // -ceux qui ont le plus haut shinydex
            string classementShinyHunter = "stat non initialisée";

            // -ceux qui ont le plus gros pokédex
            string classementDexProgress = "stat non initialisée";

            //// ceux qu'on le meilleur ratio pokéballs lancées / thunes dépensées
            //string classementRadin = "stat non initialisée";

            //// ceux qui ont attrapé le plus de pokémon totaux
            //string classementMassCatcher = "stat non initialisé";

            // celui qu'à le plus haut taux de capture
            string luckyCatcher = "stat non initialisée";

            // celui qu'a le plus bas taux de capture
            string unluckyCatcher = "stat non initialisée";

            if (utilisateurs.Count > 2)
            {
                classementLanceurDeBall = $"Classement ball lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Stats.ballLaunched)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Stats.ballLaunched)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Stats.ballLaunched)})\n";
                classementDepenseur = $"Classement money lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Stats.moneySpent)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Stats.moneySpent)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Pseudo} ({getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Stats.moneySpent)})\n";

                var top1shinydex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[0];
                var top2shinydex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[1];
                var top3shinydex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[2];

                classementShinyHunter = $" Shinydex TOP 1 : {top1shinydex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top1shinydex.Pseudo, top1shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ; TOP 2 : {top2shinydex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top2shinydex.Pseudo, top2shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ; TOP 3 : {top3shinydex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top3shinydex.Pseudo, top3shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ;";

                var top1dex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[0];
                var top2dex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[1];
                var top3dex = utilisateurs.OrderByDescending(u => dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[2];

                classementDexProgress = $"TOP 1 : {top1dex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top1dex.Pseudo, top1dex.Platform).ToList().Count} espèces enregistrées ; TOP 2 : {top2dex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top2dex.Pseudo, top2dex.Platform).ToList().Count} espèces enregistrées ; TOP 3 : {top3dex.Pseudo} avec {dataConnexion.GetEntriesByPseudo(top3dex.Pseudo, top3dex.Platform).ToList().Count} espèces enregistrées ;";
            }

            if (utilisateurs.Count > 1)
            {
                //var luckiest = utilisateurs.Where(u => dataConnexion.GetEntriesByPseudo(u.pseudo, u.platform).Count > 10).OrderByDescending(x => (x.stats.pokeCaught / x.stats.ballLaunched)).FirstOrDefault();
                //var unluckiest = utilisateurs.Where(u => dataConnexion.GetEntriesByPseudo(u.pseudo, u.platform).Count > 10).OrderBy(x => (x.stats.pokeCaught / x.stats.ballLaunched)).FirstOrDefault();
                //luckyCatcher = $"Le plus chanceux : {luckiest.pseudo} ({luckiest.stats.pokeCaught} pokémon attrapés pour {luckiest.stats.ballLaunched} ball lancées, soit un taux de capture de {Math.Round((double)(luckiest.stats.pokeCaught * 100) / luckiest.stats.ballLaunched)}%)";
                //unluckyCatcher = $"Le moins chanceux : {unluckiest.pseudo} ({unluckiest.stats.pokeCaught} pokémon attrapés pour {unluckiest.stats.ballLaunched} ball lancées, soit un taux de capture de {Math.Round((double)(unluckiest.stats.pokeCaught * 100) / unluckiest.stats.ballLaunched)}%)";
            }

            string start = @$"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>Rapport de Dexs</title>
  <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"" rel=""stylesheet"">
  <script src=""https://code.jquery.com/jquery-3.5.1.min.js""></script>
  <script src=""https://cdnjs.cloudflare.com/ajax/libs/awesomplete/1.1.5/awesomplete.min.js""></script>
</head>
<body>
    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
      </form>
    </nav><br><br>
<style>
    body {{
        background-color: #2a2a2a;
        color: #ffffff;
        padding: 20px;
    }}
    .table tbody td img {{height: 64px;
        width: auto;
    }}
    .count {{font - size: 20px;
    }}
    /* Noir et blanc */
    .black-and-white {{filter: grayscale(100%);
        -webkit-filter: grayscale(100%);
    }}
    /* Tout noir (seulement la forme) */
    .all-black {{filter: brightness(0%);
        -webkit-filter: brightness(0%);
    }}
    /* Texte plus grand dans <td> */
    .large-text td {{font-size: 20px;
    }}
</style>

  <div class=""container"">

<h1>Voir son propre dex</h1>

  <form id=""redirectForm"">
    <label for=""platform"">Platform:</label>
    <select id=""platform"" name=""platform"">
        <option value=""twitch"">Twitch</option>
        <option value=""youtube"">YouTube</option>
        <option value=""tiktok"">TikTok</option>
    </select>
    <br>
    <label for=""pseudo"">Pseudo:</label>
    <input type=""text"" id=""pseudo"" name=""pseudo"" class=""awesomplete"" data-list=""{dataPseudoList}"">
    <br>
    <input type=""submit"" value=""Submit"">
</form>

<script>
document.getElementById('redirectForm').onsubmit = function(event) {{
    event.preventDefault();
    var platform = document.getElementById('platform').value;
    var pseudo = document.getElementById('pseudo').value.toLowerCase();
    var currentUrl = window.location.href;
    var newUrl = currentUrl.substring(0, currentUrl.lastIndexOf(""/"")) + '/' + platform + '/' + pseudo + '.html';
    window.location.href = newUrl;
}};
</script>
    <p>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</p>
    <h1 class=""mt-5"">Stats globales de la chaîne</h1>
  <div class=""row"">
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total pokeball lancées : {getStringNumber(NombreTotalPokeball)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Money totale dépensée : {getStringNumber(NombreTotalSousouDepense)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de poké capturés : {getStringNumber(NombreTotalPokecapture)}, hors giveaway : {getStringNumber(NombreTotalPokecapture - nombreNormalDistribue)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de shiny capturés : {getStringNumber(NombreTotalShinycapture)}, hors giveaway : {getStringNumber(NombreTotalShinycapture - nombreShinyDistribue)}</p>
    </div>
  </div>
    <h1 class=""mt-5"">Stats classement</h1>
    <div class=""row"">
        <div class=""col-12 col-md-6"">
          <p>{classementLanceurDeBall}</p>
          <p>{classementDepenseur}</p>
          <p>{luckyCatcher}</p>
          <p>{unluckyCatcher}</p>
          <p>{classementShinyHunter}</p>
          <p>{classementDexProgress}</p>
        </div>
    </div>
";

            string end = @"
</html>
";
            fileContent = start;
            fileContent += end;

            filename = $"FullExport_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.html";
        }
    }

    internal class ExportStats : Export
    {
        public ExportStats(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings) // Initialiser la classe de base ici
        {
            GenerateStatsFile();
        }

        public void GenerateStatsFile()
        {
            List<User> users = dataConnexion.GetAllUserPlatforms();

            List<User> top10ball = users.OrderByDescending(w => w.Stats.ballLaunched).Take(10).ToList();
            List<User> top10money = users.OrderByDescending(w => w.Stats.moneySpent).Take(10).ToList();
            List<User> top10Dex = users.OrderByDescending(w => w.Stats.dexCount).Take(10).ToList();
            List<User> top10shinyDex = users.OrderByDescending(w => w.Stats.shinydex).Take(10).ToList();
            List<User> top10caught = users.OrderByDescending(w => w.Stats.pokeCaught).Take(10).ToList();
            List<User> top10shinycaught = users.OrderByDescending(w => w.Stats.shinyCaught).Take(10).ToList();

            int value_twitch = 0; int value_youtube = 0; int value_tiktok = 0; ; int value_max = 0;

            int barHeight = 32;

            string all_content = @"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Progress Bar avec Couleurs Personnalisées</title>
    <link rel=""stylesheet"" href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"">
    <style>
	img {width: 32px; height: 32px; display: inline;}
	p {font-size: 32px;}
        body {
            background-color: #2a2a2a;
            color: #ffffff;
            padding: 20px;
        }
        .progress-bar-twitch {
            background-color: #6441A5;
        }
        .progress-bar-youtube {
            background-color: #FF0000;
        }
        .progress-bar-tiktok {
            background-color: #E4E4E4;
        }
        .progress-bar span {
            text-align: center;
            color: white;
        }
    </style>
</head>
<body>

    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
      </form>
    </nav><br><br>
<div class=""container""
";

            all_content += "<center>\n\n<br><br><br><h1> Top 10 ball : </h1><br>\n";
            top10ball.ForEach(a => all_content += $"<p>{top10ball.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.ballLaunched)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.ballLaunched); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.ballLaunched); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.ballLaunched);
            value_max = value_youtube + value_twitch + value_tiktok; int percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); int percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); int percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 money : </h1><br>\n";
            top10money.ForEach(a => all_content += $"<p>{top10money.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.moneySpent)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.moneySpent); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.moneySpent); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.moneySpent);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 Dex : </h1><br>\n";
            top10Dex.ForEach(a => all_content += $"<p>{top10Dex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.dexCount)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.dexCount); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.dexCount); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.dexCount);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny Dex : </h1><br>\n";
            top10shinyDex.ForEach(a => all_content += $"<p>{top10shinyDex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.shinydex)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.shinydex); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.shinydex); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.shinydex);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 caught : </h1><br>\n";
            top10caught.ForEach(a => all_content += $"<p>{top10caught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.pokeCaught)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.pokeCaught); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.pokeCaught); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.pokeCaught);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny caught : </h1><br>\n";
            top10shinycaught.ForEach(a => all_content += $"<p>{top10shinycaught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {getStringNumber(a.Stats.shinyCaught)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.shinyCaught); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.shinyCaught); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.shinyCaught);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += @"
</div>
</center>
<script src=""https://code.jquery.com/jquery-3.5.1.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.0.7/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";

            fileContent = all_content;
            filename = "pokestats.html";

            ExportFile(true, true).Wait();
        }
    }

    internal class ExportDexAvailablePokemon : Export
    {
        private List<Pokemon> allCodedPokemons;

        public ExportDexAvailablePokemon(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings) // Initialiser la classe de base ici
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            allCodedPokemons = appSettings.allPokemons;
            fileContent = "";
            filename = $"AvailablePokemon.html";
        }

        public string GenerateFile()
        {
            BuildRapport();
            ExportFile(true, true).Wait();
            return filename;
        }

        public List<string> getLineTables()
        {
            Pokemon currPoke;
            string currline;
            string classShiny;
            string classNormal;
            List<string> linesTable = new List<string>();
            List<Entrie> entriesByPseudo = new List<Entrie>();
            List<User> users = dataConnexion.GetAllUserPlatforms();
            foreach (User u in users)
            {
                entriesByPseudo.AddRange(dataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList());
            }
            foreach (Pokemon item in allCodedPokemons)
            {
                try
                {
                    //Console.WriteLine($"generating line for {item.Name_FR}/{item.Name_EN}");
                    currPoke = appSettings.pokemons.Where(poke => poke.Name_FR == item.Name_FR).FirstOrDefault();
                    int CountShiny = 0;
                    int CountNormal = 0;
                    entriesByPseudo.Where(x => x.PokeName == item.Name_FR).ToList().ForEach(x => { CountShiny += x.CountShiny; CountNormal += x.CountNormal; });

                    classShiny = CountShiny > 0 ? "" : $@"class = ""all-black"" ";
                    classNormal = CountNormal > 0 ? "" : $@"class = ""all-black"" ";
                    string availability = getPokeAvailability(poke: item);
                    string classAvailability = "";
                    switch (availability)
                    {
                        case "Not available at all.":
                            classAvailability = "NotAvailable";
                            break;

                        case "only under distribution / events.":
                            classAvailability = "AvailableForGiveaway";
                            break;

                        case "fully available":
                            classAvailability = "FullAvailable";
                            break;
                    }
                    currline = @$"
<tr>
                <td>{item.Name_FR}</td>
                <td><img {classNormal}src=""{item.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td class=""count"">{CountNormal}</td>
                <td><img {classShiny}src=""{item.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td class=""count"">{CountShiny}</td>
                <td class=""{classAvailability}"">Dispo : {availability}</td>
            </tr>
";
                    linesTable.Add(currline);
                }
                catch
                {
                    Console.WriteLine($"Error while generating line for {item.Name_FR}/{item.Name_EN}");
                }
            }
            return linesTable;
        }

        private string getPokeAvailability(Pokemon poke)
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

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        {
            List<string> lineTables = getLineTables();

            fileContent = $@"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Pokémon Capture Tracker</title>
    <!-- Bootstrap CSS -->
    <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"" rel=""stylesheet"">
    <style>
        body {{
            background-color: #2a2a2a;
            color: #ffffff;
            padding: 20px;
        }}
        .table tbody td img {{
            height: 64px;
            width: auto;
        }}
        .NotAvailable {{
            color: #2fa432;
        }}
        .AvailableForGiveaway {{
            color: #c1a518;
        }}
        .NotAvailable {{
            color: #ad1e1e;
        }}
        .count {{font-size: 30px;
        }}
        /* Noir et blanc */
        .black-and-white {{filter: grayscale(100%);
          -webkit-filter: grayscale(100%);
        }}

        /* Tout noir (seulement la forme) */
        .all-black {{filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }}

        /* Texte plus grand dans <td> */
        .large-text td {{font - size: 20px; }}

    </style>
</head>
<body>

    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
      </form>
    </nav><br><br>

    <table class=""table table-dark table-bordered table-striped"">
        <thead>
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Capturé(s)</th>
                <th>Sprite Shiny</th>
                <th>Capturé(s)</th>
                <th>Disponibilités</th>
            </tr>
        </thead>
        <tbody>";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";
        }
    }

    internal class ExportBuyList : Export
    {
        private List<Pokemon> allBuyablePokemon;

        public ExportBuyList(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings) // Initialiser la classe de base ici
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            allBuyablePokemon = appSettings.pokemons.Where(x => x.priceNormal is not null || x.priceShiny is not null).ToList();
            fileContent = "";
            filename = $"buypokemon.html";
        }

        public void BuildDocument()
        {
            fileContent = DefaultStart();

            fileContent += @"<table class=""table table-dark table-bordered table-striped"">
        <thead>
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Prix Normal</th>
                <th>Acheter normal</th>
                <th>Sprite Shiny</th>
                <th>Prix Shiny</th>
                <th>Acheter Shiny</th>
            </tr>
        </thead>
        <tbody>";

            foreach (Pokemon poke in allBuyablePokemon)
            {
                string pokename = String.Empty;

                string displayName = globalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;

                // cas ou le altname est celui par défaut
                if (poke.Name_EN == poke.AltName || poke.Name_FR == poke.AltName)
                {
                    pokename = globalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
                }
                else
                {
                    pokename = poke.AltName;
                }

                if (poke.priceNormal is not null && poke.priceShiny is not null)
                {
                    fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>{poke.priceNormal}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename} normal"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary"">Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>{poke.priceShiny}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename} shiny"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning"">Copier commande</button></td>
            </tr>";
                }
                else if (poke.priceNormal is null && poke.priceShiny is not null)
                {
                    fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>/</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename} normal"" disabled>Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>{poke.priceShiny}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename} shiny"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning"">Copier commande</button></td>
            </tr>";
                }
                else if (poke.priceNormal is not null && poke.priceShiny is null)
                {
                    fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>{poke.priceNormal}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename.Replace(' ', '_').ToLower()} normal"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary"">Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>/</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdBuy} {pokename.Replace(' ', '_').ToLower()} shiny"" disabled>Copier commande</button></td>
            </tr>";
                }
            }

            fileContent += "</table><br>";

            fileContent += @"

<script>
        function copyToClipboard(button) {
    const textToCopy = button.getAttribute(""data-copy"");

    if (!navigator.clipboard) {
        // Fallback for browsers that don't support the Clipboard API
        const textArea = document.createElement(""textarea"");
        textArea.value = textToCopy;
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand(""copy"");
            alert(""Texte copié : "" + textToCopy);
        } catch (err) {
            alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
        }
        document.body.removeChild(textArea);
        return;
    }

    navigator.clipboard.writeText(textToCopy).then(() => {
        alert(""Texte copié : "" + textToCopy);
    }).catch(err => {
        alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
    });
}
    </script>";
            fileContent += DefaultEnd();

            ExportFile(true, true).Wait();
        }
    }

    internal class ExportScrapList : Export
    {
        private List<Pokemon> allScrappablePokemon;

        public ExportScrapList(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings) : base(appSettings, userRequest, dataConnexion, globalAppSettings) // Initialiser la classe de base ici
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            allScrappablePokemon = appSettings.pokemons.Where(x => x.enabled).ToList();
            fileContent = "";
            filename = $"scrappokemon.html";
        }

        public void BuildDocument()
        {
            fileContent = DefaultStart();

            fileContent += @"<table class=""table table-dark table-bordered table-striped"">
        <thead>
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Valeur Normal</th>
                <th>Scrap Normal</th>
                <th>Sprite Shiny</th>
                <th>Valeur Shiny</th>
                <th>Scrap Shiny</th>
            </tr>
        </thead>
        <tbody>";

            foreach (Pokemon poke in allScrappablePokemon)
            {
                string pokename = String.Empty;
                string displayName = globalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
                int valueNormal = poke.valueNormal.HasValue ? poke.valueNormal.Value : globalAppSettings.ScrapSettings.ValueDefaultNormal;
                int valueShiny = poke.valueShiny.HasValue ? poke.valueShiny.Value : globalAppSettings.ScrapSettings.ValueDefaultShiny;

                if (poke.isLegendary && !poke.valueNormal.HasValue)
                    valueNormal = valueNormal * globalAppSettings.ScrapSettings.legendaryMultiplier;
                if (poke.isLegendary && !poke.valueShiny.HasValue)
                    valueShiny = valueShiny * globalAppSettings.ScrapSettings.legendaryMultiplier;

                // cas ou le altname est celui par défaut
                if (poke.Name_EN == poke.AltName || poke.Name_FR == poke.AltName)
                {
                    pokename = globalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
                }
                else
                {
                    pokename = poke.AltName;
                }

                fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>{valueNormal}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdScrap} {pokename.Replace(' ', '_').ToLower()} normal"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary"">Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>{valueShiny}</td>
                <td><button data-copy=""{globalAppSettings.CommandSettings.CmdScrap} {pokename.Replace(' ', '_').ToLower()} shiny"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning"">Copier commande</button></td>
</tr>";
            }

            fileContent += "</table><br>";

            fileContent += @"

<script>
        function copyToClipboard(button) {
    const textToCopy = button.getAttribute(""data-copy"");

    if (!navigator.clipboard) {
        // Fallback for browsers that don't support the Clipboard API
        const textArea = document.createElement(""textarea"");
        textArea.value = textToCopy;
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        try {
            document.execCommand(""copy"");
            alert(""Texte copié : "" + textToCopy);
        } catch (err) {
            alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
        }
        document.body.removeChild(textArea);
        return;
    }

    navigator.clipboard.writeText(textToCopy).then(() => {
        alert(""Texte copié : "" + textToCopy);
    }).catch(err => {
        alert(""Erreur lors de la copie dans le presse-papiers : "" + err);
    });
}

    </script>";
            fileContent += DefaultEnd();

            ExportFile(true, true).Wait();
        }
    }

    internal class Export
    {
        public AppSettings appSettings;
        public UserRequest userRequest;
        public DataConnexion dataConnexion;
        public GlobalAppSettings globalAppSettings;
        public string filename;
        public string url;
        public string fileContent;
        public DateTime dateExport;

        public Export(AppSettings appSettings, UserRequest userRequest, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            this.appSettings = appSettings;
            this.userRequest = userRequest;
            this.dataConnexion = dataConnexion;
            this.globalAppSettings = globalAppSettings;
            filename = "";
            url = "";
            fileContent = "trick";
            dateExport = DateTime.Now;
        }

        public string DefaultStart()
        {
            return @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Pokémon Capture Tracker</title>
    <!-- Bootstrap CSS -->
    <link href=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css"" rel=""stylesheet"">
    <style>
        body {
            background-color: #2a2a2a;
            color: #ffffff;
            padding: 20px;
        }
        .table tbody td img {
            height: 64px;
            width: auto;
        }
        .NotAvailable {
            color: #2fa432;
        }
        .AvailableForGiveaway {
            color: #c1a518;
        }
        .NotAvailable {
            color: #ad1e1e;
        }
        .count {font-size: 30px;
        }
        /* Noir et blanc */
        .black-and-white {filter: grayscale(100%);
          -webkit-filter: grayscale(100%);
        }

        /* Tout noir (seulement la forme) */
        .all-black {filter: brightness(0%);
          -webkit-filter: brightness(0%);
        }

        /* Texte plus grand dans <td> */
        .large-text td {font - size: 20px; }

    </style>
</head>
<body>

    <nav class=""navbar navbar-dark bg-dark"" style=""justify-content: center; background-color: #2a2a2a;"">
      <form class=""form-inline"">
        <a class=""btn btn-sm btn-outline-secondary"" href=""main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
      </form>
    </nav><br><br>";
        }

        public string DefaultEnd()
        {
            return @"

<br><br>
    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";
        }

        public string CleanFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string cleanedFileName = Regex.Replace(fileName, "[" + Regex.Escape(invalidChars) + "]", "_");
            return cleanedFileName;
        }

        public async Task ExportFile(bool fullExport = false, bool main = false)
        {
            filename = CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("ExportsSimple"))
                Directory.CreateDirectory("ExportsSimple");

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            if (!main)
            {
                // Crée le dossier "youtube" s'il n'existe pas
                if (!Directory.Exists(Path.Combine("WebExport", "youtube")))
                    Directory.CreateDirectory(Path.Combine("WebExport", "youtube"));

                // Crée le dossier "twitch" s'il n'existe pas
                if (!Directory.Exists(Path.Combine("WebExport", "twitch")))
                    Directory.CreateDirectory(Path.Combine("WebExport", "twitch"));

                // Crée le dossier "twitch" s'il n'existe pas
                if (!Directory.Exists(Path.Combine("WebExport", "system")))
                    Directory.CreateDirectory(Path.Combine("WebExport", "system"));

                // Crée le dossier "twitch" s'il n'existe pas
                if (!Directory.Exists(Path.Combine("WebExport", "tiktok")))
                    Directory.CreateDirectory(Path.Combine("WebExport", "tiktok"));
            }

            // Chemin complet du fichier
            string filePath = Path.Combine("ExportsSimple", filename);

            if (fullExport && !main)
            {
                filePath = Path.Combine("WebExport", userRequest.Platform, filename);
            }
            if (fullExport && main)
            {
                filePath = Path.Combine("WebExport", filename);
            }

            // Écrit le contenu dans le fichier
            await File.WriteAllTextAsync(filePath.ToLower(), fileContent);
        }

        public string GetUserStats()
        {
            string data = "";

            User utilisateur = new User(userRequest.UserName, userRequest.Platform, userRequest.UserCode, dataConnexion);

            data += $"<p>Nombre d'espèce enregistrée : {utilisateur.Stats.dexCount} / {appSettings.pokemons.Count}</p>";
            float dexProgressPourcent = utilisateur.Stats.dexCount * 100 / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexProgressPourcent}%;\" aria-valuenow=\"{dexProgressPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexProgressPourcent}%</div>\r\n</div>";
            data += $"<p>Nombre d'espèce shiny enregistrée : {utilisateur.Stats.shinydex}</p>";
            float dexShinyPourcent = utilisateur.Stats.shinydex * 100 / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexShinyPourcent}%;\" aria-valuenow=\"{dexShinyPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexShinyPourcent}%</div>\r\n</div>";
            data += "<br>";

            data += $"<p>Total argent dépensé : {utilisateur.Stats.moneySpent}</p>";
            data += $"<p>Total de ball lancées : {utilisateur.Stats.ballLaunched}</p>";
            data += "<br>";

            data += $"<p>Nombre de pokémon non shiny capturé : {utilisateur.Stats.normalCaught - utilisateur.Stats.giveawayNormal}</p>";
            data += $"<p>Nombre de pokémon shiny capturé : {utilisateur.Stats.shinyCaught - utilisateur.Stats.giveawayShiny}</p>";
            data += $"<p>Total de pokémon attrapé : {utilisateur.Stats.pokeCaught - (utilisateur.Stats.giveawayNormal + utilisateur.Stats.giveawayShiny)}</p>";
            data += $"<p>Pokémon le plus attrapé : {utilisateur.Stats.favoritePoke}</p>";
            TimeSpan diff = DateTime.Now - utilisateur.Stats.firstCatch;
            data += $"<p>Dresseur depuis : {utilisateur.Stats.firstCatch} (depuis {diff.Days} jours.)</p>";

            return data;
        }

        public string GetUserCards()
        {
            string data = "";
            User utilisateur = new User(userRequest.UserName, userRequest.Platform, userRequest.UserCode, dataConnexion);
            utilisateur.Code_user = dataConnexion.GetCodeUserByPlatformPseudo(utilisateur);

            string urlAvatar = dataConnexion.GetAvatarUrl(utilisateur);
            string urlSpritePokeFav = dataConnexion.GetSpriteFavoriteCreature(utilisateur, appSettings);

            utilisateur.generateStats();
            utilisateur.generateStatsAchievement(appSettings, globalAppSettings);

            List<Badge> badgeToShow = utilisateur.Stats.badges.Where(x => x.Rarity == "exotic").ToList();
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "legendary" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "epic" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "rare" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "uncommon" && x.Obtained).ToList());
            badgeToShow.AddRange(utilisateur.Stats.badges.Where(x => x.Rarity == "common" && x.Obtained).ToList());

            badgeToShow = badgeToShow.Take(8).ToList();

            string badgePart = "";
            int count = 1;
            foreach (Badge badge in badgeToShow)
            {
                badgePart += $@"
<div class=""col"">
          <img src=""{badge.IconUrl}"" alt=""Badge {count}"" style=""height: 48px; width: 48px;"">
            <p style=""font-size: 12px; margin-top: 4px"">{badge.Title}</p>
        </div>";
                count++;
            }

            data = @$"
<style>
    .generatedCard {{
      color: white;
      border: 1px solid #ccc;
      padding: 20px;
      width: 856px;
      height: 540px;
      border-radius: 10px;
      background: linear-gradient(to bottom, #0b0b2b, #1b2735 70%, #090a0f);
    }}
    #downloadBtn {{
      margin-top: 35px;
      font-size: large;
      border-radius: 3px;
      border: 5px;
      padding: 15px;
      box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
-webkit-box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
-moz-box-shadow: 0px 0px 38px 0px rgba(0,0,0,0.5);
    }}
    .stars {{
  width: 1px;
  height: 1px;
  position: absolute;
  background: white;
  box-shadow: 2vw 5vh 2px white, 10vw 8vh 2px white, 15vw 15vh 1px white,
    22vw 22vh 1px white, 28vw 12vh 2px white, 32vw 32vh 1px white,
    38vw 18vh 2px white, 42vw 35vh 1px white, 48vw 25vh 2px white,
    53vw 42vh 1px white, 58vw 15vh 2px white, 63vw 38vh 1px white,
    68vw 28vh 2px white, 73vw 45vh 1px white, 78vw 32vh 2px white,
    83vw 48vh 1px white, 88vw 20vh 2px white, 93vw 52vh 1px white,
    98vw 35vh 2px white, 5vw 60vh 1px white, 12vw 65vh 2px white,
    18vw 72vh 1px white, 25vw 78vh 2px white, 30vw 85vh 1px white,
    35vw 68vh 2px white, 40vw 82vh 1px white, 45vw 92vh 2px white,
    50vw 75vh 1px white, 55vw 88vh 2px white, 60vw 95vh 1px white,
    65vw 72vh 2px white, 70vw 85vh 1px white, 75vw 78vh 2px white,
    80vw 92vh 1px white, 85vw 82vh 2px white, 90vw 88vh 1px white,
    95vw 75vh 2px white;
  animation: twinkle 8s infinite linear;
}}
@keyframes twinkle {{
  0%,
  100% {{
    opacity: 0.8;
  }}
  50% {{
    opacity: 0.4;
  }}
}}
  </style>
</head>
<body>
  <center>
  <div class=""generatedCard"">
  <!-- Partie titre -->
    <div class=""stars""></div>
    <h1>Dresseur : {utilisateur.Pseudo}</h1>
    <h3 style=""margin-bottom: 50px;"">ID : {utilisateur.Code_user}</h3>
    <!-- Partie Corp -->
    <div class=""container"">
      <!-- Photo -->
      <div class=""row"">
        <div class=""col"">
          <img src=""{urlAvatar}"" class=""img-thumbnail"" alt=""userprofile picture"" style=""width: 192px;"" crossorigin=""anonymous"">
        </div>
        <!-- Stats -->
        <div class=""col"">
          <p>Global dex : {utilisateur.Stats.dexCount}</p>
          <p>Shiny dex : {utilisateur.Stats.shinydex}</p>
          <p>Trainer since : {utilisateur.Stats.firstCatch.ToString("d MMM. yy", CultureInfo.InvariantCulture)}</p>
          <p>Platform : {userRequest.Platform} <img src=""https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{userRequest.Platform.ToLower()}.png"" style=""height: 16px; width: 16px;""></p>
          <p>Level : {utilisateur.Stats.level}</p>
          <p>Captures : {utilisateur.Stats.pokeCaught}</p>
        </div>
        <!-- Favorite creature -->
        <div class=""col"">
          <h6>Creature Favorite :</h6>
          <p>{Commun.FullInfoShinyNormal(Commun.CapitalizePhrase(utilisateur.Stats.favoritePoke))}</p>
          <img src=""{urlSpritePokeFav}"" class=""img-thumbnail"" alt=""userprofile fav creature"" style=""width: 128px;"">
        </div>
      </div>
    </div>
    <!-- Badges -->
    <div class=""container"">
      <div class=""row"">
        {badgePart}
      </div>
    </div>
  </div>

  <button id=""downloadBtn"">Télécharger ma carte</button>
</center>
<script src=""https://html2canvas.hertzen.com/dist/html2canvas.min.js""></script>
  <script>
    document.getElementById(""downloadBtn"").addEventListener(""click"", function(){{
      var cardElement = document.querySelector("".generatedCard"");

      html2canvas(cardElement, {{ useCORS: true }}).then(canvas => {{
        var imageData = canvas.toDataURL(""image/png"");
        var downloadLink = document.createElement(""a"");
        downloadLink.href = imageData;
        downloadLink.download = ""ma-carte.png"";
        document.body.appendChild(downloadLink);
        downloadLink.click();
        document.body.removeChild(downloadLink);
      }}).catch(err => {{
        console.error(""Erreur lors de la génération de l'image :"", err);
      }});
    }});
  </script>
";

            return data;
        }

        public string GetUserBadge(AppSettings settings)
        {
            string data = "";
            string wip = "";
            try
            {
                User utilisateur = new User(userRequest.UserName, userRequest.Platform, userRequest.UserCode, dataConnexion);
                utilisateur.generateStatsAchievement(settings, globalAppSettings);
                data += $"<p>Level {utilisateur.Stats.level}</p><br><p>{utilisateur.Stats.currentXP} XP/{utilisateur.Stats.MaxXPLevel} XP</p><br><p>{utilisateur.Stats.totalXP} XP Totale</p><br>";

                List<string> GroupsBadges = utilisateur.Stats.badges.Select(element => element.Group).Distinct().ToList();
                foreach (string group in GroupsBadges)
                {
                    List<Badge> badgesOfThisGroup = utilisateur.Stats.badges.Where(g => g.Group == group).ToList();
                    List<string> SubGroupsBadges = badgesOfThisGroup.Select(element => element.SubGroup).Distinct().ToList();
                    data += $"<br><br><br><h2 class=\"col-12\" style=\"margin-top:25px\"><b>{group.ToString()} [{badgesOfThisGroup.Where(x => x.Obtained).Count().ToString()}/{badgesOfThisGroup.Count}]</b></h2>";
                    data += "<div class=\"row\">";
                    foreach (string subgroup in SubGroupsBadges)
                    {
                        List<Badge> badgeOfThisSubgroup = badgesOfThisGroup.Where(element => element.SubGroup == subgroup).ToList();
                        data += $"  <br><br><h4 class=\"col-12\" style=\"margin-top:15px\"><b>{subgroup.ToString()} [{badgeOfThisSubgroup.Where(x => x.Obtained).Count().ToString()}/{badgeOfThisSubgroup.Count}]</b></h4>";
                        data += "   <div class=\"row\">";

                        foreach (Badge badge in badgeOfThisSubgroup)
                        {
                            wip = badge.Obtained ? badge.Description : "????";
                            wip += $" [+{badge.XP}XP]";
                            data += $@"
                            <div style=""width: 29vw;  margin-left: 1vw; margin-bottom: 15px;"">
                                <div class=""card {badge.Rarity}"" style=""background-color: #222222;  height: 220px;"">
                                  <center><br><img src=""{badge.IconUrl}"" class=""card-img-top trophy-{badge.Obtained}"" alt=""..."" style=""height: 96px; width: auto;""></center>
                                  <div class=""card-body"">
                                    <h5 class=""card-title"">{badge.Title}</h5>
                                    <p class=""card-text"">{wip}</p>
                                  </div>
                                </div>
                            </div>";
                        }
                        data += "   </div>";
                    }
                    data += "</div>";
                }
            }
            catch (Exception)
            {
            }

            return data;
        }

        public string getStringNumber(int ballLaunched)
        {
            string result = string.Empty;
            float rounded = 0;
            if (ballLaunched < 1000)
            {
                rounded = ballLaunched;
                result = $"{rounded}";
            }
            else if (ballLaunched < 1000000)
            {
                rounded = ballLaunched;
                result = $"{Math.Round(rounded / 1000, 2)}K";
            }
            else
            {
                rounded = ballLaunched;
                result = $"{Math.Round(rounded / 1000000, 2)}M";
            }
            return result;
        }
    }
}