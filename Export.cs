﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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

        public async Task<string> UploadFileAsync()
        {
            string filepath = Path.Combine("ExportsSimple", this.filename);
            using (var client = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        var fileContent = new StreamContent(fileStream);
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
                        content.Add(fileContent, "file", Path.GetFileName(filepath));

                        HttpResponseMessage response = await client.PostAsync("https://file.io", content);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseContent = await response.Content.ReadAsStringAsync();
                            var jsonResponse = System.Text.Json.JsonDocument.Parse(responseContent);
                            this.url = jsonResponse.RootElement.GetProperty("link").GetString();
                            return this.url;
                        }
                        else
                        {
                            throw new Exception("File upload failed.");
                        }
                    }
                }
            }
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

        public void BuildRapport()
        {
            this.filename = $"Dex_{userRequest.UserName}_export_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.html";

            List<string> lineTables = getLineTables();

            this.fileContent = $@"
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


        th, td {{ text-align: center; border: 1px solid black; padding: 10px; /* Ajuste la marge interne des cellules selon tes besoins */ }}

    </style>
</head>
<body>
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

            lineTables.ForEach(line => this.fileContent += line);

            this.fileContent += @$"</tbody>
    </table>
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

        public void BuildDoc()
        {
            List<User> utilisateurs = dataConnexion.GetAllUserPlatforms();
            List<string> lines = new List<string>();
            List<string> table = new List<string>();
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
                classementLanceurDeBall = $"Classement ball lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Stats.ballLaunched)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Stats.ballLaunched)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Stats.ballLaunched)})\n";
                classementDepenseur = $"Classement money lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Stats.moneySpent)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Stats.moneySpent)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Pseudo} ({this.getStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Stats.moneySpent)})\n";

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
</head>
<body>
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
    <input type=""text"" id=""pseudo"" name=""pseudo"">
<br>
    <input type=""submit"" value=""Submit"">
</form>

<script> document.getElementById('redirectForm').onsubmit = function(event) {{ event.preventDefault(); var platform = document.getElementById('platform').value; var pseudo = document.getElementById('pseudo').value.toLowerCase(); var currentUrl = window.location.href; var newUrl = currentUrl.substring(0, currentUrl.lastIndexOf(""/"")) + '/' + platform + '/' + pseudo + '.html'; window.location.href = newUrl; }} </script>
    <p>{DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}</p>
    <h1 class=""mt-5"">Stats globales de la chaîne</h1>
  <div class=""row"">
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total pokeball lancées : {this.getStringNumber(NombreTotalPokeball)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Money totale dépensée : {this.getStringNumber(NombreTotalSousouDepense)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de poké capturés : {this.getStringNumber(NombreTotalPokecapture)}, hors giveaway : {this.getStringNumber(NombreTotalPokecapture - nombreNormalDistribue)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de shiny capturés : {this.getStringNumber(NombreTotalShinycapture)}, hors giveaway : {this.getStringNumber(NombreTotalShinycapture - nombreShinyDistribue)}</p>
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
    <h1 class=""mt-5"">Selectionnez une personne</h1>
    <select id=""tableSelector"" class=""form-control mb-3""> ";

            //utilisateurs.ForEach(item => lines.Add($"<option value=\"{item.Platform}-{item.Pseudo}\">{item.Pseudo} ({item.Platform})</option>"));

            string middle = @"</select>";

            string wip = "";
            List<Entrie> info = new List<Entrie>();
            Pokemon pkm = null;
            foreach (User item in utilisateurs)
            {
                wip = @$"
    <div id=""{item.Platform}-{item.Pseudo}"" class=""table-container"">
      <h2>{item.Pseudo} ({item.Platform})</h2>
      <table class=""table table-dark table-bordered table-striped"">
        <thead>
          <tr>
            <th></th>
            <th>Pokémon</th>
            <th>Capturés normaux</th>
            <th>Capturés shiny</th>
          </tr>
        </thead>
        <tbody>";

                info = dataConnexion.GetEntriesByPseudo(item.Pseudo, item.Platform);
                foreach (Entrie en in info)
                {
                    en.setIDPoke(appSettings);
                }
                info = info.OrderBy(e => e.entryPokeID).ToList();

                foreach (Entrie entry in info)
                {
                    pkm = appSettings.pokemons.FirstOrDefault(p => p.Name_FR == entry.PokeName);
                    if (pkm == null)
                    {
                        Console.WriteLine($"WARN Le pokémon {entry.Pseudo} (possédé par {entry.Pseudo}) n'a pas été trouvé dans la liste des pokémon activés");
                    }
                    else
                    {
                        wip += @$"<tr>
            <td><img src=""{pkm.Sprite_Normal}"" alt=""Normal Sprite"" style=""height: 64px; width: auto;""></td>
            <td>{entry.PokeName}</td>
            <td>{entry.CountNormal}</td>
            <td>{entry.CountShiny}</td>
          </tr>";
                    }
                }
                wip += @"
        </tbody>
      </table>
    </div>";
                table.Add(wip);
                wip = "";
            }

            string end = @"<script>
    $(document).ready(function() {{
      $('#tableSelector').change(function() {{
        $('.table-container').hide();
        $('#' + $(this).val()).show();
      }});
    }});
  </script>
</html>
";
            fileContent = start;
            lines.ForEach(line => fileContent += line);
            fileContent += middle;
            table.ForEach(line => fileContent += line);
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
<div class=""container""
";

            all_content += "<center>\n\n<br><br><br><h1> Top 10 ball : </h1><br>\n";
            top10ball.ForEach(a => all_content += $"<p>{top10ball.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.ballLaunched)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.ballLaunched); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.ballLaunched); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.ballLaunched);
            value_max = value_youtube + value_twitch + value_tiktok; int percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); int percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); int percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 money : </h1><br>\n";
            top10money.ForEach(a => all_content += $"<p>{top10money.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.moneySpent)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.moneySpent); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.moneySpent); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.moneySpent);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 Dex : </h1><br>\n";
            top10Dex.ForEach(a => all_content += $"<p>{top10Dex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.dexCount)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.dexCount); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.dexCount); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.dexCount);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny Dex : </h1><br>\n";
            top10shinyDex.ForEach(a => all_content += $"<p>{top10shinyDex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.shinydex)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.shinydex); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.shinydex); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.shinydex);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 caught : </h1><br>\n";
            top10caught.ForEach(a => all_content += $"<p>{top10caught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.pokeCaught)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.pokeCaught); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.pokeCaught); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.pokeCaught);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny caught : </h1><br>\n";
            top10shinycaught.ForEach(a => all_content += $"<p>{top10shinycaught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {this.getStringNumber(a.Stats.shinyCaught)} </p>\r");

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

            this.ExportFile(true, true).Wait();
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
            this.BuildRapport();
            this.ExportFile(true, true).Wait();
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

                        case "only under distribution.":
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
                return "only under distribution.";
            }
            else
            {
                return "fully available";
            }
        }

        public void BuildRapport()
        {
            List<string> lineTables = getLineTables();

            this.fileContent = $@"
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

            lineTables.ForEach(line => this.fileContent += line);

            this.fileContent += @$"</tbody>
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
            this.filename = "";
            this.url = "";
            this.fileContent = "trick";
            dateExport = DateTime.Now;
        }

        public string CleanFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string cleanedFileName = Regex.Replace(fileName, "[" + Regex.Escape(invalidChars) + "]", "_");
            return cleanedFileName;
        }

        public async Task ExportFile(bool fullExport = false, bool main = false)
        {

            this.filename = CleanFileName(this.filename);


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
            string filePath = Path.Combine("ExportsSimple", this.filename);

            if (fullExport && !main)
            {
                filePath = Path.Combine("WebExport", userRequest.Platform, this.filename);
            }
            if (fullExport && main)
            {
                filePath = Path.Combine("WebExport", this.filename);
            }

            // Écrit le contenu dans le fichier
            await System.IO.File.WriteAllTextAsync(filePath.ToLower(), this.fileContent);
        }

        public string GetUserStats()
        {
            string data = "";

            User utilisateur = new User(userRequest.UserName, userRequest.Platform, userRequest.UserCode, dataConnexion);

            data += $"<p>Nombre d'espèce enregistrée : {utilisateur.Stats.dexCount} / {appSettings.pokemons.Count}</p>";
            float dexProgressPourcent = (utilisateur.Stats.dexCount * 100) / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexProgressPourcent}%;\" aria-valuenow=\"{dexProgressPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexProgressPourcent}%</div>\r\n</div>";
            data += $"<p>Nombre d'espèce shiny enregistrée : {utilisateur.Stats.shinydex}</p>";
            float dexShinyPourcent = (utilisateur.Stats.shinydex * 100) / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexShinyPourcent}%;\" aria-valuenow=\"{dexShinyPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexShinyPourcent}%</div>\r\n</div>";
            data += "<br>";

            data += $"<p>Total argent dépensé : {utilisateur.Stats.moneySpent}</p>";
            data += $"<p>Total de ball lancées : {utilisateur.Stats.ballLaunched}</p>";
            data += "<br>";

            data += $"<p>Nombre de pokémon non shiny capturé : {utilisateur.Stats.normalCaught-utilisateur.Stats.giveawayNormal}</p>";
            data += $"<p>Nombre de pokémon shiny capturé : {utilisateur.Stats.shinyCaught - utilisateur.Stats.giveawayShiny}</p>";
            data += $"<p>Total de pokémon attrapé : {utilisateur.Stats.pokeCaught - (utilisateur.Stats.giveawayNormal+utilisateur.Stats.giveawayShiny)}</p>";
            data += $"<p>Pokémon le plus attrapé : {utilisateur.Stats.favoritePoke}</p>";
            TimeSpan diff = DateTime.Now - utilisateur.Stats.firstCatch;
            data += $"<p>Dresseur depuis : {utilisateur.Stats.firstCatch} (depuis {diff.Days} jours.)</p>";

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
                data += $"<p>Level {utilisateur.Stats.level}</p><br><p>{utilisateur.Stats.currentXP} XP/{globalAppSettings.BadgeSettings.XPPerLevel} XP</p><br><p>{utilisateur.Stats.totalXP} XP Totale</p><br>";

                var group = utilisateur.Stats.badges.GroupBy(x => x.Type);
                foreach (var groupBadges in group)
                {
                    data += "<div class=\"row\">";
                    foreach (var badge in groupBadges)
                     {
                        wip = badge.Obtained ? badge.Description : "????";
                        wip += $" [+{badge.XP}XP]";
                        data += $@"
<div class=""col-4"">
    <div class=""card {badge.Rarity}"" style=""background-color: #222222;"">
      <center><br><img src=""{badge.IconUrl}"" class=""card-img-top trophy-{badge.Obtained}"" alt=""...""></center>
      <div class=""card-body"">
        <h5 class=""card-title"">{badge.Title}</h5>
        <p class=""card-text"">{wip}</p>
      </div>
    </div>
</div>";
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
                result = $"{Math.Round((rounded / 1000), 2)}K";
            }
            else
            {
                rounded = ballLaunched;
                result = $"{Math.Round((rounded / 1000000), 2)}M";
            }
            return result;
        }
    }
}