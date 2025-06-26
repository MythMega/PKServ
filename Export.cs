using PKServ.Binding;
using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ
{
    internal class ExportSoloDex
    {
        private User User { get; set; }
        private string Source { get; set; }
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }

        public string filename { get; set; }
        public string fileContent { get; set; }

        public ExportSoloDex(AppSettings appSettings, User user, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            this.AppSettings = appSettings;
            this.User = user;
            this.DataConnexion = dataConnexion;
            this.GlobalAppSettings = globalAppSettings;

            Source = dataConnexion.GetFirstNonNullStream();
            BuildRapport();
        }

        public List<string> getLineTables()
        {
            List<string> lineTables = new List<string>();
            string currline = "";
            Pokemon currPoke;
            List<Entrie> entriesByPseudo = DataConnexion.GetEntriesByPseudo(User.Pseudo, User.Platform);
            foreach (Entrie en in entriesByPseudo)
            {
                en.setIDPoke(AppSettings);
            }
            entriesByPseudo = entriesByPseudo.OrderBy(e => e.entryPokeID).ToList();
            foreach (Entrie item in entriesByPseudo)
            {
                string classShiny = string.Empty;
                string classNormal = string.Empty;

                classShiny = item.CountShiny > 0 ? "" : $@"class = ""all-black"" ";
                classNormal = item.CountNormal > 0 ? "" : $@"class = ""all-black"" ";

                currPoke = AppSettings.pokemons.Where(poke => poke.Name_FR == item.PokeName).FirstOrDefault();
                if (currPoke == null)
                {
                    Console.WriteLine($"WARN Le pokémon {item.PokeName} (possédé par {item.Pseudo}) n'a pas été trouvé dans la liste des pokémon activés");
                }
                else
                {
                    string additionals = currPoke.GetAdditionalInfosString(gas: GlobalAppSettings);
                    currline = @$"
<tr>
                <td class=""pokename""><a href=""../Creature/{item.PokeName}.html"">{item.PokeName}</a></td>
                <td><img {classNormal}src=""{currPoke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td class=""count"">{item.CountNormal}</td>
                <td><img {classShiny}src=""{currPoke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td class=""count"">{item.CountShiny}</td>
                <td>{item.dateFirstCatch}</td>
                <td class=""d-none"">{additionals}</td>
            </tr>
";
                    lineTables.Add(currline);
                }
            }
            ;
            return lineTables;
        }

        /// <summary>
        /// SOLO DEX
        /// </summary>
        public void BuildRapport()
        {
            filename = $"Dex_{User.Pseudo}_export_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.html";

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
        <a class=""btn btn-sm btn-outline-secondary"" href=""../commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""../records.html"" style=""color: white;"">Enregistrements</a>
      </form>
    </nav><br><br>
    <h1>Pokédex {User.Pseudo} - chez {Source}</h1>
    <p>Pokédex de {User.Pseudo} [de {User.Platform}] au {DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}</p>

  <div class=""d-flex align-items-center"" style=""max-width: 480px;"">
    <!-- Input recherche avec max-width -->
    <input type=""text"" id=""searchInput"" placeholder=""Rechercher Pokémon ou Statut"" class=""form-control"" style=""margin-bottom: 20px; max-width: 300px;"">
    <!-- Compteur -->
    <span id=""rowCount"" style=""margin-left: 10px; font-size: 16px;"">0 résultat(s)</span>
  </div>
    <table class=""table table-dark table-bordered table-striped"">
        <thead class=""thead-light"">
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Capturé(s)</th>
                <th>Sprite Shiny</th>
                <th>Capturé(s)</th>
                <th>Première capture(s)</th>
                <th class=""d-none"">Tag</th>
            </tr>
        </thead>
        <tbody id=""recordsTable"">";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
<p>Trainer Card :</p><br>
{User.GetUserCardsHTML(AppSettings, DataConnexion, GlobalAppSettings)}
<br><br>
<p>Stats :</p><br>
{User.GetUserStatsHTML(AppSettings, GlobalAppSettings)}
<p>Badges :</p><br>
{User.GetUserBadgeHTML(AppSettings, GlobalAppSettings, DataConnexion)}

   <script>
  // Fonction qui normalise le texte en supprimant les accents et en le convertissant en minuscules
  function normalizeText(text) {{
    return text.normalize(""NFD"").replace(/[\u0300-\u036f]/g, """").toLowerCase();
  }}

  function filterTable() {{
    // Récupère et normalise le texte de recherche
    const searchValue = normalizeText(document.getElementById('searchInput').value);
    // Découpe la recherche en tokens en éliminant les espaces inutiles
    const tokens = searchValue.split(' ').filter(token => token.trim() !== '');
    const tableRows = document.querySelectorAll('#recordsTable tr');
    let visibleCount = 0;

    tableRows.forEach(row => {{
      // Extraction et normalisation des contenus des colonnes recherchées
      const pokemon = normalizeText(row.cells[0].textContent);
      const tags = normalizeText(row.cells[6].textContent);

      // Chaque token doit se retrouver dans au moins l'un des champs (condition AND)
      const isMatch = tokens.every(token =>
        pokemon.includes(token) || tags.includes(token)
      );

      if (isMatch || tokens.length === 0) {{
        row.style.display = '';
        visibleCount++;
      }} else {{
        row.style.display = 'none';
      }}
    }});

    document.getElementById('rowCount').textContent = visibleCount + "" résultat(s)"";
  }}

  // Ajoute l'événement keyup pour lancer le filtrage à la saisie
  document.getElementById('searchInput').addEventListener('keyup', filterTable);

  // Mise à jour du filtrage dès le chargement de la page
  document.addEventListener('DOMContentLoaded', filterTable);
</script>

    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";
        }

        public void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            // Crée le dossier "Exports/<platforme>" s'il n'existe pas
            if (!Directory.Exists(Path.Combine("WebExport", User.Platform)))
                Directory.CreateDirectory(Path.Combine("WebExport", User.Platform));

            string filePath = Path.Combine("WebExport", User.Platform, filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }
    }

    internal class ExportMain
    {
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string filename { get; set; } = "main.html";
        public string fileContent { get; set; }

        public ExportMain(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
            BuildDoc();
        }

        /// <summary>
        /// MAIN.HTML
        /// </summary>
        public void BuildDoc()
        {
            List<User> utilisateurs = DataConnexion.GetAllUserPlatforms();
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
                classementLanceurDeBall = $"Classement ball lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[0].Stats.ballLaunched)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[1].Stats.ballLaunched)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.ballLaunched).ToList()[2].Stats.ballLaunched)})\n";
                classementDepenseur = $"Classement money lancées :\nTOP 1 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[0].Stats.moneySpent)})\n    2 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[1].Stats.moneySpent)})\n    3 - {utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Pseudo} ({Commun.GetStringNumber(utilisateurs.OrderByDescending(u => u.Stats.moneySpent).ToList()[2].Stats.moneySpent)})\n";

                var top1shinydex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[0];
                var top2shinydex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[1];
                var top3shinydex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).Where(w => w.CountShiny > 0).ToList().Count).ToList()[2];

                classementShinyHunter = $" Shinydex TOP 1 : {top1shinydex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top1shinydex.Pseudo, top1shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ; TOP 2 : {top2shinydex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top2shinydex.Pseudo, top2shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ; TOP 3 : {top3shinydex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top3shinydex.Pseudo, top3shinydex.Platform).Where(w => w.CountShiny > 0).ToList().Count} espèces shiny enregistrées ;";

                var top1dex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[0];
                var top2dex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[1];
                var top3dex = utilisateurs.OrderByDescending(u => DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList().Count).ToList()[2];

                classementDexProgress = $"TOP 1 : {top1dex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top1dex.Pseudo, top1dex.Platform).ToList().Count} espèces enregistrées ; TOP 2 : {top2dex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top2dex.Pseudo, top2dex.Platform).ToList().Count} espèces enregistrées ; TOP 3 : {top3dex.Pseudo} avec {DataConnexion.GetEntriesByPseudo(top3dex.Pseudo, top3dex.Platform).ToList().Count} espèces enregistrées ;";
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
        <a class=""btn btn-sm btn-outline-secondary"" href=""commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""records.html"" style=""color: white;"">Enregistrements</a>
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
    .count {{font-size: 20px;
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
      <p>Nombre total pokeball lancées : {Commun.GetStringNumber(NombreTotalPokeball)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Money totale dépensée : {Commun.GetStringNumber(NombreTotalSousouDepense)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de poké capturés : {Commun.GetStringNumber(NombreTotalPokecapture)}, hors giveaway : {Commun.GetStringNumber(NombreTotalPokecapture - nombreNormalDistribue)}</p>
    </div>
    <div class=""col-lg-12 col-md-6"">
      <p>Nombre total de shiny capturés : {Commun.GetStringNumber(NombreTotalShinycapture)}, hors giveaway : {Commun.GetStringNumber(NombreTotalShinycapture - nombreShinyDistribue)}</p>
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
        }

        public async Task ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            await File.WriteAllTextAsync(filePath.ToLower(), fileContent);
        }
    }

    internal class ExportCommandGenerator
    {
        public AppSettings AppSettings { get; set; }
        public DataConnexion DataConnexion { get; set; }
        public GlobalAppSettings GlobalAppSettings { get; set; }
        public string FileName { get; set; } = "commandgenerator.html";
        public string FileContent { get; set; }

        public ExportCommandGenerator(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
            GenerateStatsFile();
        }

        public void GenerateStatsFile()
        {
            FileContent = CommandGeneratorImpl.GenerateFileContent(AppSettings, GlobalAppSettings, DataConnexion);
        }

        internal void ExportFile()
        {
            FileName = Commun.CleanFileName(FileName);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", FileName);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), FileContent);
        }
    }

    internal class ExportStats
    {
        public string FileName { get; set; }
        public string FileContent { get; set; }
        public AppSettings AppSettings { get; set; }
        public DataConnexion DataConnexion { get; set; }
        public GlobalAppSettings GlobalAppSettings { get; set; }

        public ExportStats(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
            GenerateStatsFile();
        }

        public void GenerateStatsFile()
        {
            List<User> users = DataConnexion.GetAllUserPlatforms();

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
        <a class=""btn btn-sm btn-outline-secondary"" href=""commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
      </form>
    </nav><br><br>
<div class=""container""
";

            all_content += "<center>\n\n<br><br><br><h1> Top 10 ball : </h1><br>\n";
            top10ball.ForEach(a => all_content += $"<p>{top10ball.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.ballLaunched)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.ballLaunched); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.ballLaunched); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.ballLaunched);
            value_max = value_youtube + value_twitch + value_tiktok; int percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); int percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); int percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 money : </h1><br>\n";
            top10money.ForEach(a => all_content += $"<p>{top10money.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.moneySpent)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.moneySpent); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.moneySpent); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.moneySpent);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 Dex : </h1><br>\n";
            top10Dex.ForEach(a => all_content += $"<p>{top10Dex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.dexCount)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.dexCount); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.dexCount); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.dexCount);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny Dex : </h1><br>\n";
            top10shinyDex.ForEach(a => all_content += $"<p>{top10shinyDex.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.shinydex)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.shinydex); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.shinydex); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.shinydex);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 caught : </h1><br>\n";
            top10caught.ForEach(a => all_content += $"<p>{top10caught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.pokeCaught)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.pokeCaught); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.pokeCaught); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.pokeCaught);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny caught : </h1><br>\n";
            top10shinycaught.ForEach(a => all_content += $"<p>{top10shinycaught.IndexOf(a) + 1} : <img src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.shinyCaught)} </p>\r");

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

            FileContent = all_content;
            FileName = "pokestats.html";

            ExportFile();
        }

        public void ExportFile()
        {
            FileName = Commun.CleanFileName(FileName);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", FileName);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), FileContent);
        }
    }

    internal class ExportDexAvailablePokemon
    {
        private List<Pokemon> allCodedPokemons;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "availablepokemon.html";

        public ExportDexAvailablePokemon(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            allCodedPokemons = appSettings.allPokemons.Where(p => p.enabled).ToList();
            fileContent = "";
            filename = $"AvailablePokemon.html";
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
        }

        public string GenerateFile()
        {
            BuildRapport();
            ExportFile();
            return filename;
        }

        private void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }

        private string getPokeZoneNameAndLink(List<Zone> zonesList)
        {
            string r = "";
            foreach (Zone zone in zonesList)
            {
                r += $"<a href=\"./Zone/{Commun.CleanFileName(zone.Name)}.html\">{zone.Name}</a>; ";
            }
            return r.TrimEnd(' ', ';').TrimStart(' ', ';'); // Enlève le dernier espace et le point-virgule
        }

        public List<string> getLineTables()
        {
            Pokemon currPoke;
            string currline;
            string classShiny;
            string classNormal;
            List<string> linesTable = new List<string>();
            List<Entrie> entriesByPseudo = new List<Entrie>();
            List<User> users = DataConnexion.GetAllUserPlatforms();
            foreach (User u in users)
            {
                entriesByPseudo.AddRange(DataConnexion.GetEntriesByPseudo(u.Pseudo, u.Platform).ToList());
            }
            foreach (Pokemon item in allCodedPokemons)
            {
                try
                {
                    //Console.WriteLine($"generating line for {item.Name_FR}/{item.Name_EN}");
                    currPoke = AppSettings.pokemons.Where(poke => poke.Name_FR == item.Name_FR).FirstOrDefault();
                    int CountShiny = 0;
                    int CountNormal = 0;
                    entriesByPseudo.Where(x => x.PokeName == item.Name_FR).ToList().ForEach(x => { CountShiny += x.CountShiny; CountNormal += x.CountNormal; });

                    classShiny = CountShiny > 0 ? "" : $@"class = ""all-black"" ";
                    classNormal = CountNormal > 0 ? "" : $@"class = ""all-black"" ";
                    string availability = getPokeAvailability(poke: item);
                    string classAvailability = "";
                    string artistAndTheirLinks = ""; // "<a href=""{item.Artist}"" {item.Artist}
                    if (item.Artist.Count != 0)
                    {
                        foreach (Artist artist in item.Artist)
                        {
                            artistAndTheirLinks += $"<a href=\"{artist.ArtistLink}\">{artist.ArtistName}</a>{(string.IsNullOrEmpty(artist.ArtistCredit) ? "" : $" ({artist.ArtistCredit})")}; ";
                        }
                    }
                    else
                    {
                        artistAndTheirLinks = "/";
                    }
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

                    if (currPoke.isShinyLock && availability == "fully available")
                    {
                        availability += " (shiny locked)";
                    }

                    string additionalInfos = item.GetAdditionalInfosString(gas: GlobalAppSettings);
                    string spawn = !item.enabled ? "????????" : !item.ZonesNames.Any() ?
                    @$"<a href=""./Zone/_void_.html"">void</a>" :
                    getPokeZoneNameAndLink(item.ZonesList);

                    currline = @$"
<tr>
                <td style=""font-size: 1.2em;""><a href=""./Creature/{item.Name_FR}.html"">{item.Name_FR}</a> {CreatureRarity.IconHTML(item.Rarity, IconSize.Medium)}</td>
                <td><img {classNormal}src=""{item.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td class=""count"">{CountNormal}</td>
                <td><img {classShiny}src=""{item.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td class=""count"">{CountShiny}</td>
                <td class=""{classAvailability}"">Dispo : {availability}</td>
                <td>{spawn}</td>
                <td>{artistAndTheirLinks}</td>
                <td class=""d-none"">{additionalInfos}</td>
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

            fileContent = Commun.DefaultHTMLStart(false) + $@"
<style>
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
        a {{
            textDecoration = 'none';
        }}

        /* Texte plus grand dans <td> */
        .large-text td {{font-size: 20px; }}

    </style>
  <div class=""d-flex align-items-center"" style=""max-width: 480px;"">
    <!-- Input recherche avec max-width -->
    <input type=""text"" id=""searchInput"" placeholder=""Rechercher Pokémon ou Statut"" class=""form-control"" style=""margin-bottom: 20px; max-width: 300px;"">
    <!-- Compteur -->
    <span id=""rowCount"" style=""margin-left: 10px; font-size: 16px;"">0 résultat(s)</span>
  </div>
    <table class=""table table-dark table-bordered table-striped"">
        <thead>
            <tr>
                <th>Pokémon</th>
                <th>Sprite Normal</th>
                <th>Capturé(s)</th>
                <th>Sprite Shiny</th>
                <th>Capturé(s)</th>
                <th>Disponibilité</th>
                <th>Localisation</th>
                <th>Artist</th>
                <th class=""d-none"">Tag</th>
            </tr>
        </thead>
        <tbody id=""recordsTable"">";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
    <script>
  // Fonction qui normalise le texte en supprimant les accents et en le convertissant en minuscules
  function normalizeText(text) {{
    return text.normalize(""NFD"").replace(/[\u0300-\u036f]/g, """").toLowerCase();
  }}

  function filterTable() {{
    // Récupère et normalise le texte de recherche
    const searchValue = normalizeText(document.getElementById('searchInput').value);
    // Découpe le texte en tokens et retire les espaces inutiles
    const tokens = searchValue.split(' ').filter(token => token.trim() !== '');
    const tableRows = document.querySelectorAll('#recordsTable tr');
    let visibleCount = 0;

    tableRows.forEach(row => {{
      // Extraction et normalisation des contenus de chaque colonne recherchée
      const pokemon = normalizeText(row.cells[0].textContent); // Colonne Pokémon
      const dispo   = normalizeText(row.cells[5].textContent);   // Colonne dispo (statut ou autre)
      const artist  = normalizeText(row.cells[6].textContent);   // Colonne artist
      const tags    = normalizeText(row.cells[7].textContent);   // Colonne tags

      // Pour chaque token, vérifie qu'il se retrouve dans au moins un des champs
      const isMatch = tokens.every(token =>
        pokemon.includes(token) || dispo.includes(token) || artist.includes(token) || tags.includes(token)
      );

      // Affiche la ligne si tous les tokens sont trouvés (ou s'il n'y a aucun token)
      if (isMatch || tokens.length === 0) {{
        row.style.display = '';
        visibleCount++;
      }} else {{
        row.style.display = 'none';
      }}
    }});

    document.getElementById('rowCount').textContent = visibleCount + "" résultat(s)"";
  }}

  // Ajout de l'événement pour lancer le filtrage à la saisie
  document.getElementById('searchInput').addEventListener('keyup', filterTable);

  // Mise à jour du filtrage dès le chargement de la page
  document.addEventListener('DOMContentLoaded', filterTable);
</script>

    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>";
        }
    }

    internal class ExportIndividualPoke
    {
        private List<Pokemon> allCodedPokemons;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }

        public string fileContent { get; set; }
        public string filename { get; set; } = "";

        public ExportIndividualPoke(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            this.AppSettings = appSettings;
            this.DataConnexion = dataConnexion;
            this.GlobalAppSettings = globalAppSettings;
            allCodedPokemons = appSettings.allPokemons.ToList();
            fileContent = "";
        }

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        { }

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

        public async Task ExportAllFile()
        {
            if (!Directory.Exists(Path.Combine("WebExport", "Creature")))
            {
                Directory.CreateDirectory(Path.Combine("WebExport", "Creature"));
            }
            foreach (Pokemon poke in allCodedPokemons)
            {
                string availability = getPokeAvailability(poke: poke);
                string type = poke.Type1 is not null || poke.Type2 is not null ? "" : "(no infos)";
                string spawn = !poke.enabled ? "????????" : !poke.ZonesNames.Any() ?
                    @$"<a href=""../Zone/_void_.html"">void</a>" :
                    getPokeZoneNameAndLink(poke.ZonesList);
                string infos = "";
                if (poke.isLegendary)
                    infos += "Légendaire; ";
                if (poke.isCustom)
                    infos += "Custom; ";
                if (poke.Serie is not null)
                    infos += $"<Série : {poke.Serie};> ";
                if (poke.Type1 != null)
                {
                    type += $"<img class=\"type\" src=\"{TypeBinding.GetImageUrl(poke.Type1)}\">";
                }
                if (poke.Type2 != null)
                {
                    type += $"<img class=\"type\" src=\"{TypeBinding.GetImageUrl(poke.Type2)}\">";
                }

                int defaultValueNormal = poke.isLegendary ? GlobalAppSettings.ScrapSettings.ValueDefaultNormal * GlobalAppSettings.ScrapSettings.legendaryMultiplier : GlobalAppSettings.ScrapSettings.ValueDefaultNormal;
                int defaultValueShiny = poke.isLegendary ? GlobalAppSettings.ScrapSettings.ValueDefaultShiny * GlobalAppSettings.ScrapSettings.legendaryMultiplier : GlobalAppSettings.ScrapSettings.ValueDefaultShiny;

                string prix = $"<br>Normal <img class=\"icon\" src=\"{ShinyBinding.GetIcon(false)}\"> : {(poke.priceNormal?.ToString() ?? "N/A")} <br> Shiny <img class=\"icon\" src=\"{ShinyBinding.GetIcon(true)}\"> : {(poke.priceShiny?.ToString() ?? "N/A")}";
                string value = $"<br>Normal <img class=\"icon\" src=\"{ShinyBinding.GetIcon(false)}\"> : {(poke.valueNormal?.ToString() ?? defaultValueNormal.ToString())} <br> Shiny <img class=\"icon\" src=\"{ShinyBinding.GetIcon(true)}\"> : {(poke.valueShiny?.ToString() ?? defaultValueShiny.ToString())}";

                filename = $"{poke.Name_FR}.html";
                string artisteInfos = string.Join("; ", poke.Artist.Select(a => $"<a href=\"{a.ArtistLink}\"> {a.ArtistName}</a>")); ;
                try
                {
                    fileContent = $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>{poke.Name_FR}</title>
    <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"" rel=""stylesheet"">
    <style>
        body {{
            background-color: #121212; /* Fond sombre */
            color: white;
        }}
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
</head>
<body>
    <div class=""container mt-5"">
        <div class=""row"">
            <div class=""col-md-4 image-container"">
                <img class=""sprite"" src=""{poke.Sprite_Normal}"" alt=""Image"">
            </div>
            <div class=""col-md-8 info-container"">
                <h2>Informations</h2>
                <ul class=""list-group"">
                    <li class=""list-group-item bg-dark text-white""><strong>Nom :</strong> {poke.Name_FR}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Name :</strong> {poke.Name_EN}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Type :</strong> {type}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Spawn :</strong> {spawn}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Availability :</strong> {availability}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Artist :</strong> {artisteInfos}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Infos :</strong> {infos}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Buy Price / Prix d'achat :</strong> {prix}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Value / Valeur :</strong> {value}</li>
                    <li class=""list-group-item bg-dark text-white""><strong>Rareté :</strong> {CreatureRarity.IconHTML(poke.Rarity, IconSize.Medium)}</li>
                </ul>
            </div>
        </div>
    </div>

    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
</body>
</html>

";

                    fileContent = Commun.DefaultHTMLStart(true) + fileContent + Commun.DefaultHTMLEnd();
                    await File.WriteAllTextAsync(Path.Combine("WebExport", "Creature", filename), fileContent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while exporting individual file for {poke.Name_FR}/{poke.Name_EN}: {ex.Message}");
                }
            }
        }

        private string getPokeZoneNameAndLink(List<Zone> zonesList)
        {
            string r = "";
            foreach (Zone zone in zonesList)
            {
                r += $"<a href=\"../Zone/{Commun.CleanFileName(zone.Name)}.html\">{zone.Name}</a>; ";
            }
            return r.TrimEnd(' ', ';').TrimStart(' ', ';'); // Enlève le dernier espace et le point-virgule
        }
    }

    internal class ExportIndividualZone
    {
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "";

        public ExportIndividualZone(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            fileContent = "";
            filename = $"AvailablePokemon.html";
            GlobalAppSettings = globalAppSettings;
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
        }

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        { }

        public async Task ExportZonePage(Zone zone)
        {
            // Chemin du dossier pour la page zone
            string directoryPath = Path.Combine("WebExport", "Zone");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Filtrer les pokémons activés selon la condition donnée
            var filteredPokemons = AppSettings.allPokemons
                .Where(pokemon => pokemon.enabled && !pokemon.isLock &&
                       (!pokemon.IsZoneExclusive || pokemon.ZonesList.Any(z => z.Name.Equals(zone.Name, StringComparison.OrdinalIgnoreCase))))
                .ToList();

            // Séparer et ordonner les pokémons : exclusifs d'abord
            var exclusivePokemons = filteredPokemons.Where(p => p.IsZoneExclusive && p.ZonesList.Count == 1).ToList();
            var nonExclusivePokemons = filteredPokemons.Where(p => !p.IsZoneExclusive || p.ZonesList.Count != 1).ToList();
            var orderedPokemons = exclusivePokemons.Concat(nonExclusivePokemons).ToList();
            var orderedPokemonsByName = orderedPokemons
                .OrderByDescending(p => p.IsZoneExclusive)
                .ThenByDescending(pokemon => Commun.CompareStrings(pokemon.ZonesList.OrderBy(zone => zone.DexRequirement).FirstOrDefault().Name, zone.Name))
                .ToList();

            // Construction des cards pour chaque pokémon
            StringBuilder cardsBuilder = new StringBuilder();
            foreach (var pokemon in orderedPokemonsByName)
            {
                // Définir la classe de la card et y ajouter un style pour les exclusifs
                string cardClass = "card bg-dark text-white h-100";
                if (pokemon.isLegendary)
                {
                    cardClass += " card-legendary";
                }
                else if (pokemon.IsZoneExclusive && pokemon.ZonesList.Count == 1)
                {
                    cardClass += " card-exclusive";
                }
                else if (pokemon.IsZoneExclusive)
                {
                    cardClass += " card-not-everywhere";
                }
                else
                {
                    cardClass += " card-common";
                }

                // string displayName = Commun.CompareStrings(pokemon.Name_FR, pokemon.Name_EN) ? $"{pokemon.Name_EN}" : $"{pokemon.Name_FR} - {pokemon.Name_EN}";
                string displayName = GlobalAppSettings.LanguageCode.ToUpper() == LanguageBinding.FRENCH ? $"{pokemon.Name_FR}" : $"{pokemon.Name_EN}";

                // icône shiny (ou non)
                string additionalInfos = CreatureRarity.IconHTML(pokemon.Rarity, IconSize.Medium);
                additionalInfos += @$"<img class=""icon"" src=""{ShinyBinding.GetIcon(!pokemon.isShinyLock)}"">";
                if (Commun.CompareStrings(pokemon.ZonesList.OrderBy(zone => zone.DexRequirement).FirstOrDefault().Name, zone.Name))
                {
                    additionalInfos += @$"<img class=""icon"" src=""{IconBinding.GetIconURL("NEW")}"">";
                }

                cardsBuilder.AppendLine($@"
            <div class=""col"">
                <div class=""{cardClass}"">
                    <div class=""sprite-container"">
                        <img src=""{pokemon.Sprite_Normal}"" alt=""{pokemon.Name_FR}"" style=""object-fit:contain;"">
                    </div>
                    <div class=""card-body"">
                        <h5 class=""card-title"">
                            <a href=""../Creature/{pokemon.Name_FR}.html"">{displayName}</a> {additionalInfos}
                        </h5>
                    </div>
                </div>
            </div>
        ");
            }

            // Création du contenu HTML complet de la page
            string content = $@"
<!DOCTYPE html>
<html lang=""fr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <title>{zone.Name} - Détails de la zone</title>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css"">
    <style>
        body {{
            background-color: #121212;
            color: white;
        }}
        /* Bannière zoomée pour l'image de la zone */
        .zone-image {{
            height: 200px;
            width: 100%;
            object-fit: cover;
            margin-bottom: 20px;
        }}
        .sprite-container {{
            height: 256px;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
        }}
        h5 {{
            display: inline;
            margin: 0;
            padding: 0;
            font-weight: bold;
        }}
        .icon {{
            max-height: 16px;
            width: auto;
        }}
        .sprite-container img {{
            width: 100%;
            height: auto;
            image-rendering: pixelated;
            image-rendering: -moz-crisp-edges;
            image-rendering: crisp-edges;
            -ms-interpolation-mode: nearest-neighbor;
        }}
        a {{
            text-decoration: none;
            color: inherit;
            text-shadow: 0 0 5px rgba(255, 255, 255, 0.9);
        }}
        /* Style pour les cartes de pokémon exclusifs : effet doré */
        .card-exclusive {{
            border: 2px solid #d4af37;
            box-shadow: 0 0 10px #ffd700;
            background-color: #212529;
            text-align: center;
        }}
        /* Style pour les cartes de pokémon exclusifs : effet doré */
        .card-common {{
            border: 2px solid #000000;
            box-shadow: 0 0 5px #000000;
            background-color: #212529;
            text-align: center;
        }}
        /* Style pour les cartes de pokémon dispo partout : effet blanc */
        .card-not-everywhere {{
            border: 2px solid #ffffff;
            box-shadow: 0 0 5px #ccc;
            background-color: #212529;
            text-align: center;
        }}
        /* Style pour les cartes de pokémon légendaires : effet inner shadow multicolore */
        .card-legendary {{
            border: 2px solid #e0e0ff;
            background: radial-gradient(circle at center, #121212, #e0e0ff);
            box-shadow: inset 0 0 20px red, inset 0 0 40px blue, inset 0 0 60px green;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class=""container my-5"">
        <!-- Section de la zone -->
        <div class=""text-center"">
            <img src=""{zone.Image}"" alt=""Zone Image"" class=""img-fluid zone-image"">
            <h1>{zone.Name}</h1>
            <p>Level Cap : {zone.LevelRequirement}</p>
            <p>Dex Cap : {zone.DexRequirement}</p>
            <p>Description : {zone.Description}</p>
        </div>

        <!-- Bouton de copie de commande -->
        <div class=""row mb-4"">
            <div class=""col text-center"">
                <button id=""copyZoneButton"" class=""btn btn-secondary"" onclick=""copyZoneCommand()"">Copier commande zone</button>
            </div>
        </div>

        <!-- Affichage du résultat de recherche -->
        <div class=""row mb-4"">
            <div class=""col text-center"">
                <span id=""resultCount""></span>
            </div>
        </div>

        <!-- Barre de recherche -->
        <div class=""row mb-4"">
            <div class=""col"">
                <input type=""text"" id=""searchInput"" class=""form-control"" placeholder=""Rechercher un Pokémon..."">
            </div>
        </div>

        <!-- Quelques exemples de cartes catégories bien identifiées -->
        <div class=""row mb-3"">
            <div class=""col"">
                <div class=""card-legendary"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Légendaires</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-exclusive"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Exclusifs</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-not-everywhere"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Zonaux</h5>
                    </div>
                </div>
            </div>
            <div class=""col"">
                <div class=""card-common"">
                    <div class=""card-body"">
                        <h5 class=""card-title"">Commun</h5>
                    </div>
                </div>
            </div>
        </div>

        <!-- Grille Bootstrap affichant les pokémons ciblés -->
        <div class=""row row-cols-2 row-cols-md-4 row-cols-lg-6 g-4 mt-5"" id=""cardsContainer"">
            {cardsBuilder.ToString()}
        </div>
    </div>
    <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js""></script>
    <script>
        // Définit la commande à copier avec le nom de la zone formaté
        var zoneCommand = '!changeZone {zone.Name.Replace(" ", "_")}';

        function copyZoneCommand() {{
            navigator.clipboard.writeText(zoneCommand).then(function() {{
                alert('Commande copiée : ' + zoneCommand);
            }}, function(err) {{
                console.error('Erreur de copie : ', err);
            }});
        }}

        // Fonction pour mettre à jour le nombre de Pokémon affichés
        function updateResultCount() {{
            var cards = document.querySelectorAll('#cardsContainer .col');
            var visibleCount = 0;
            cards.forEach(function(card) {{
                if (card.style.display !== 'none') {{
                    visibleCount++;
                }}
            }});
            document.getElementById('resultCount').textContent = visibleCount + ' Pokémon(s) affiché(s)';
        }}

        document.getElementById('searchInput').addEventListener('keyup', function() {{
            var query = this.value.toLowerCase();
            var cards = document.querySelectorAll('#cardsContainer .col');
            cards.forEach(function(card) {{
                var title = card.querySelector('.card-title').textContent.toLowerCase();
                card.style.display = (title.indexOf(query) !== -1) ? '' : 'none';
            }});
            updateResultCount();
        }});

        // Mise à jour dès le chargement de la page
        window.addEventListener('load', updateResultCount);
    </script>
</body>
</html>
    ";
            content = Commun.DefaultHTMLStart(true) + content + Commun.DefaultHTMLEnd();
            // Sauvegarder la page
            string fileName = $"{zone.Name}.html";
            string filePath = Path.Combine(directoryPath, Commun.CleanFileName(fileName));
            File.WriteAllText(filePath, content);

            await Task.CompletedTask;
        }

        public async Task ExportAllFile()
        {
            if (!Directory.Exists(Path.Combine("WebExport", "Creature")))
            {
                Directory.CreateDirectory(Path.Combine("WebExport", "Creature"));
            }
            foreach (Zone zone in AppSettings.Zones)
            {
                await ExportZonePage(zone);
            }
        }
    }

    internal class ExportBuyList
    {
        private List<Pokemon> allBuyablePokemon;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "";

        public ExportBuyList(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            allBuyablePokemon = appSettings.pokemons.Where(x => x.priceNormal is not null || x.priceShiny is not null).ToList();
            fileContent = "";
            filename = $"buypokemon.html";
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
        }

        public void BuildDocument()
        {
            fileContent = Commun.DefaultHTMLStart(false);

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

                string displayName = GlobalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;

                // cas ou le altname est celui par défaut
                if (poke.Name_EN == poke.AltName || poke.Name_FR == poke.AltName)
                {
                    pokename = GlobalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
                }
                else
                {
                    pokename = poke.AltName;
                }

                string priceNormal = poke.priceNormal.HasValue ? poke.priceNormal.ToString() : "/";
                string priceShiny = poke.priceShiny.HasValue ? poke.priceShiny.ToString() : "/";

                string commandName = displayName
                    .Replace(" ", "_");

                if (poke.priceNormal is not null && poke.priceShiny is not null)
                {
                    fileContent += $@"
<tr>
    <td>{displayName}</td>
    <td>
        <img style=""height: 96px;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite"">
    </td>
    <td>{priceNormal} </td>
    <td>
        <button
            data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {commandName} normal""
            {(poke.priceNormal is null ? "disabled" : @"onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary""")}>
            Copier commande
        </button>
    </td>
    <td>
        <img style=""height: 96px;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite"">
    </td>
    <td>{priceShiny} </td>
    <td>
        <button
            data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {commandName} shiny""
            {(poke.priceShiny is null ? "disabled" : @"onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning""")}>
            Copier commande
        </button>
    </td>
</tr>";
                }
                else if (poke.priceNormal is null && poke.priceShiny is not null)
                {
                    fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>/</td>
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {pokename} normal"" disabled>Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>{poke.priceShiny}</td>
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {pokename} shiny"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning"">Copier commande</button></td>
            </tr>";
                }
                else if (poke.priceNormal is not null && poke.priceShiny is null)
                {
                    fileContent += @$"
<tr>
                <td>{displayName}</td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Normal}"" alt=""Normal Sprite""></td>
                <td>{poke.priceNormal}</td>
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {pokename.Replace(' ', '_').ToLower()} normal"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary"">Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>/</td>
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdBuy} {pokename.Replace(' ', '_').ToLower()} shiny"" disabled>Copier commande</button></td>
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
            fileContent += Commun.DefaultHTMLEnd();

            ExportFile();
        }

        private void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }
    }

    internal class ExportScrapList
    {
        private List<Pokemon> allScrappablePokemon;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "scrappokemon.html";

        public ExportScrapList(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            allScrappablePokemon = appSettings.pokemons.Where(x => x.enabled).ToList();
            fileContent = "";
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
            GlobalAppSettings = globalAppSettings;
        }

        public void BuildDocument()
        {
            fileContent = Commun.DefaultHTMLStart(false);

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
                string displayName = GlobalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
                int valueNormal = poke.valueNormal.HasValue ? poke.valueNormal.Value : GlobalAppSettings.ScrapSettings.ValueDefaultNormal;
                int valueShiny = poke.valueShiny.HasValue ? poke.valueShiny.Value : GlobalAppSettings.ScrapSettings.ValueDefaultShiny;

                if (poke.isLegendary && !poke.valueNormal.HasValue)
                    valueNormal = valueNormal * GlobalAppSettings.ScrapSettings.legendaryMultiplier;
                if (poke.isLegendary && !poke.valueShiny.HasValue)
                    valueShiny = valueShiny * GlobalAppSettings.ScrapSettings.legendaryMultiplier;

                // cas ou le altname est celui par défaut
                if (poke.Name_EN == poke.AltName || poke.Name_FR == poke.AltName)
                {
                    pokename = GlobalAppSettings.LanguageCode == "fr" ? poke.Name_FR : poke.Name_EN;
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
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdScrap} {pokename.Replace(' ', '_').ToLower()} normal"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-primary"">Copier commande</button></td>
                <td><img style=""height: 96px; width: auto;"" src=""{poke.Sprite_Shiny}"" alt=""Shiny Sprite""></td>
                <td>{valueShiny}</td>
                <td><button data-copy=""{GlobalAppSettings.CommandSettings.CmdScrap} {pokename.Replace(' ', '_').ToLower()} shiny"" onclick=""copyToClipboard(this)"" type=""button"" class=""btn btn-warning"">Copier commande</button></td>
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
            fileContent += Commun.DefaultHTMLEnd();

            ExportFile();
        }

        private void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }
    }
}