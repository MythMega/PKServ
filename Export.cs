using PKServ.Binding;
using PKServ.Business;
using PKServ.Business.Exports;
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

            fileContent = Commun.DefaultHTMLStart(true, $"DEX {User.Pseudo}") + $@"
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
            fileContent = ExportMainImpl.GetFileContent(dataConnexion: DataConnexion, appSettings: AppSettings);
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

            string all_content = Commun.DefaultHTMLStart(false, "StreamDex > Stats") + @"
<style>
	p {font-size: 32px;}
    </style>
<div class=""container""
";

            all_content += "<center>\n\n<br><br><br><h1> Top 10 ball : </h1><br>\n";
            top10ball.ForEach(a => all_content += $"<p>{top10ball.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.ballLaunched)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.ballLaunched); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.ballLaunched); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.ballLaunched);
            value_max = value_youtube + value_twitch + value_tiktok; int percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); int percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); int percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 money : </h1><br>\n";
            top10money.ForEach(a => all_content += $"<p>{top10money.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.moneySpent)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.moneySpent); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.moneySpent); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.moneySpent);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 Dex : </h1><br>\n";
            top10Dex.ForEach(a => all_content += $"<p>{top10Dex.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.dexCount)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.dexCount); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.dexCount); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.dexCount);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny Dex : </h1><br>\n";
            top10shinyDex.ForEach(a => all_content += $"<p>{top10shinyDex.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.shinydex)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.shinydex); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.shinydex); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.shinydex);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 caught : </h1><br>\n";
            top10caught.ForEach(a => all_content += $"<p>{top10caught.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.pokeCaught)} </p>\r");

            value_youtube = users.Where(u => u.Platform == "youtube").Sum(x => x.Stats.pokeCaught); value_twitch = users.Where(u => u.Platform == "twitch").Sum(x => x.Stats.pokeCaught); value_tiktok = users.Where(u => u.Platform == "tiktok").Sum(x => x.Stats.pokeCaught);
            value_max = value_youtube + value_twitch + value_tiktok; percent_twitch = (int)Math.Round((double)(100 * value_twitch) / value_max); percent_youtube = (int)Math.Round((double)(100 * value_youtube) / value_max); percent_tiktok = (int)Math.Round((double)(100 * value_tiktok) / value_max);

            all_content += $@"
<div class=""progress"" style=""height: {barHeight}px;"">
  <div class=""progress-bar progress-bar-twitch"" role=""progressbar"" style=""width: {percent_twitch}%; height: {barHeight}px;"" aria-valuenow=""{percent_twitch}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_twitch}% Twitch ({value_twitch})</span></div>
  <div class=""progress-bar progress-bar-youtube"" role=""progressbar"" style=""width: {percent_youtube}%; height: {barHeight}px;"" aria-valuenow=""{percent_youtube}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_youtube}% YouTube ({value_youtube})</span></div>
  <div class=""progress-bar progress-bar-tiktok"" role=""progressbar"" style=""width: {percent_tiktok}%; height: {barHeight}px;"" aria-valuenow=""{percent_tiktok}"" aria-valuemin=""0"" aria-valuemax=""100""><span>{percent_tiktok}% TikTok ({value_tiktok})</span></div>
</div><br><br><br>";

            all_content += "\n\n\n<br><br><br><h1> Top 10 shiny caught : </h1><br>\n";
            top10shinycaught.ForEach(a => all_content += $"<p>{top10shinycaught.IndexOf(a) + 1} : <img style=\"width: 32px; height: 32px; display: inline;\" src='https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/platform/{a.Platform}.png'> {a.Pseudo} => {Commun.GetStringNumber(a.Stats.shinyCaught)} </p>\r");

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

    internal class ExportBallDex
    {
        private List<Pokemon> allCodedPokemons;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "BallDex.html";

        public ExportBallDex(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            allCodedPokemons = appSettings.allPokemons.Where(p => p.enabled).ToList();
            fileContent = "";
            filename = "BallDex.html";
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

        public void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        {
            List<string> lineTables = getLineTables();

            fileContent = Commun.DefaultHTMLStart(false, "StreamDex > BallDex") + $@"
<style>
        .table tbody td img {{
            height: 64px;
            width: auto;
        }}
        .NotAvailable {{
            color: #2fa432;
        }}
        .count {{font-size: 30px;
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
                <th>Sprite</th>
                <th>Ball</th>
                <th>Infos</th>
                <th>Plus d'info</th>
            </tr>
        </thead>
        <tbody id=""recordsTable"">";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
" + Commun.DefaultHTMLEnd();
        }

        public List<string> getLineTables()
        {
            List<string> linesTable = new List<string>();
            string currline;
            string spriteClass;

            foreach (Pokeball item in AppSettings.pokeballs)
            {
                try
                {
                    spriteClass = item.enabledOnWeb ? "" : $@"class = ""all-black"" ";
                    string classAvailability = item.enabledOnWeb ? "" : "NotAvailable";
                    string availabilityDisplayed = item.enabledOnWeb ? "Available" : "Not Available";
                    currline = @$"
<tr>
                <td><img {spriteClass} src=""{item.sprite}"" style=""height:64px; width:auto;""alt=""Normal Sprite""></td>
                <td style=""font-size: 1.2em;""><a href=""./Ball/{item.Name}.html"">{item.Name}</a></td>
                <td class=""count"">Catchrate : {item.catchrate} | Shinyrate : {item.shinyrate}</td>
                <td><a href=""./Ball/{item.Name}.html""><button>
            Plus d'infos
        </button></a></td>
</tr>
";
                    linesTable.Add(currline);
                }
                catch
                {
                    Console.WriteLine($"Error while generating line for {item.Name} in Export > ExportBallDex > getLineTables");
                }
            }
            return linesTable;
        }
    }

    internal class ExportZoneDex
    {
        private List<Pokemon> allCodedPokemons;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }
        public string fileContent { get; set; }
        public string filename { get; set; } = "ZoneDex.html";

        public ExportZoneDex(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            allCodedPokemons = appSettings.allPokemons.Where(p => p.enabled).ToList();
            fileContent = "";
            filename = "ZoneDex.html";
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

        public void ExportFile()
        {
            filename = Commun.CleanFileName(filename);

            // Crée le dossier "Exports" s'il n'existe pas
            if (!Directory.Exists("WebExport"))
                Directory.CreateDirectory("WebExport");

            string filePath = Path.Combine("WebExport", filename);

            // Écrit le contenu dans le fichier
            File.WriteAllText(filePath.ToLower(), fileContent);
        }

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        {
            List<string> lineTables = getLineTables();

            fileContent = Commun.DefaultHTMLStart(false, "StreamDex > ZoneDex") + $@"
<style>
        .table tbody td img {{
            height: 64px;
            width: auto;
        }}
        .NotAvailable {{
            color: #2fa432;
        }}
        .count {{font-size: 30px;
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
                <th>Image</th>
                <th>Zone</th>
                <th>Description</th>
                <th>Dex Requis</th>
                <th>Lvl Requis</th>
                <th>Plus d'info</th>
            </tr>
        </thead>
        <tbody id=""recordsTable"">";

            lineTables.ForEach(line => fileContent += line);

            fileContent += @$"</tbody>
    </table>
<br><br>
" + Commun.DefaultHTMLEnd();
        }

        public List<string> getLineTables()
        {
            List<string> linesTable = new List<string>();
            string currline;
            string spriteClass;

            foreach (Zone item in AppSettings.Zones)
            {
                try
                {
                    spriteClass = item.Enabled ? "" : $@"class = ""all-black"" ";
                    string classAvailability = item.Enabled ? "" : "NotAvailable";
                    string availabilityDisplayed = item.Enabled ? "Available" : "Not Available";
                    currline = @$"
<tr>
                <td><img {spriteClass}src=""{item.Image}"" alt=""Normal Sprite""></td>
                <td style=""font-size: 1.2em;""><a href=""./Zone/{Commun.CleanFileName(item.Name)}.html"">{item.Name}</a></td>
                <td style=""font-size: 1.2em;"">{item.Description}</td>
                <td class=""count"">Dex : {item.DexRequirement}</td>
                <td class=""count"">Lvl : {item.LevelRequirement}</td>
                <td><a href=""./Zone/{Commun.CleanFileName(item.Name)}.html""><button>
            Plus d'infos
        </button></a></td>
            </tr>
";
                    linesTable.Add(currline);
                }
                catch
                {
                    Console.WriteLine($"Error while generating line for {item.Name} in Export > ExportBallDex > getLineTables");
                }
            }
            return linesTable;
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

        public void ExportFile()
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
                    string spawn = item.isLock ? "????????" : !item.ZonesNames.Any() ?
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

            fileContent = Commun.DefaultHTMLStart(false, "StreamDex > GlobalDex") + $@"
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

        public async Task ExportAllFile()
        {
            if (!Directory.Exists(Path.Combine("WebExport", "Creature")))
            {
                Directory.CreateDirectory(Path.Combine("WebExport", "Creature"));
            }
            foreach (Pokemon poke in allCodedPokemons)
            {
                await ExportCreature.ExportIndividualCreature(creature: poke, globalAppSettings: GlobalAppSettings);
            }
        }
    }

    internal class ExportIndividualBall
    {
        private List<Pokeball> allBall;
        private AppSettings AppSettings { get; set; }
        private DataConnexion DataConnexion { get; set; }
        private GlobalAppSettings GlobalAppSettings { get; set; }

        public string fileContent { get; set; }
        public string filename { get; set; } = "";

        public ExportIndividualBall(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            this.AppSettings = appSettings;
            this.DataConnexion = dataConnexion;
            this.GlobalAppSettings = globalAppSettings;
            allBall = appSettings.pokeballs.ToList();
            fileContent = "";
        }

        /// <summary>
        /// Rapport de dex
        /// </summary>
        public void BuildRapport()
        { }

        public async Task ExportAllFile()
        {
            if (!Directory.Exists(Path.Combine("WebExport", "Ball")))
            {
                Directory.CreateDirectory(Path.Combine("WebExport", "Ball"));
            }
            foreach (Pokeball ball in allBall)
            {
                await ExportBall.ExportIndividualBall(ball: ball, globalAppSettings: GlobalAppSettings);
            }
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
            filename = $"defaultZoneName";
            GlobalAppSettings = globalAppSettings;
            AppSettings = appSettings;
            DataConnexion = dataConnexion;
        }

        public async Task ExportZonePage(Zone zone)
        {
            await ExportZone.ExportIndividualZone(zone: zone, appSettings: AppSettings, globalAppSettings: GlobalAppSettings);
        }

        /// <summary>
        /// Crée le dossier et lance l'export de toutes les zones de AppSettings
        /// </summary>
        /// <returns></returns>
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
            fileContent = Commun.DefaultHTMLStart(false, $"StreamDex > Shop");

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
            fileContent = Commun.DefaultHTMLStart(false, $"StreamDex > Scrapping");

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