using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Commun
    {
        /// <summary>
        /// File Uploader
        /// retourne l'url
        /// </summary>
        /// <param name="filepath"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static async Task<string> UploadFileAsync(string filepath, GlobalAppSettings globalAppSettings, string targetFolder)
        {
            string token = globalAppSettings.GitHubTokenUpload;
            string owner = "MythMega";
            string repos = "PKServExports";

            string content = Convert.ToBase64String(File.ReadAllBytes(filepath));
            string message = $"Upload {Path.GetFileName(filepath)}";

            var fileContent = new
            {
                message = message,
                content = content
            };

            string json = JsonSerializer.Serialize(fileContent);

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", token);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; GitHubUploader/1.0)");
                string url = $"https://api.github.com/repos/{owner}/{repos}/contents/{globalAppSettings.Namespace}/{targetFolder}/{Path.GetFileName(filepath)}";

                var requestContent = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await client.PutAsync(url, requestContent);
                string responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("File uploaded successfully.");

                    try
                    {
                        var jsonResponse = JsonDocument.Parse(responseContent);
                        string fileUrl = jsonResponse.RootElement.GetProperty("content").GetProperty("download_url").GetString();
                        return fileUrl;
                    }
                    catch (JsonException)
                    {
                        throw new Exception("La réponse de l'API n'est pas du JSON valide ou ne contient pas les propriétés attendues.");
                    }
                }
                else
                {
                    throw new Exception($"File upload failed. Status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }

        /// <summary>
        /// Retourne les options nécessaires ç une serialization/deserialization impeccable
        /// </summary>
        /// <returns></returns>
        public static JsonSerializerOptions GetJsonSerializerOptions()
        {
            return new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        /// <summary>
        /// Give a creature to a user, from anywhere
        /// </summary>
        /// <param name="user"></param>
        /// <param name="poke"></param>
        /// <param name="connexion"></param>
        /// <param name="ChannelSource"></param>
        public static void ObtainPoke(User user, Pokemon poke, DataConnexion connexion, string ChannelSource)
        {
            user.Code_user = connexion.GetCodeUserByPlatformPseudo(user);
            List<Entrie> entriesByPseudo = connexion.GetEntriesByPseudo(user.Pseudo, user.Platform);
            int count = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Count();
            if (entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Any())
            {
                Entrie entrie = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).FirstOrDefault();
                if (poke.isShiny)
                    entrie.CountShiny++;
                else
                    entrie.CountNormal++;
                // a virer a terme
                if ((entrie.code == null || entrie.code == "" || entrie.code == "unset" || entrie.code == "unset in UserRequest" || entrie.code == "unset by code")
                    && user.Code_user != null && user.Code_user != "" && user.Code_user != "unset" && user.Code_user != "unset in UserRequest" && user.Code_user != "unset by code")
                {
                    entrie.code = user.Code_user;
                    foreach (var item in entriesByPseudo)
                    {
                        item.code = user.Code_user;
                        item.Validate(false);
                    }
                }
                entrie.Validate(false);
            }
            else
            {
                (!poke.isShiny ? new Entrie(-1, user.Pseudo, ChannelSource, user.Platform, poke.Name_FR, 1, 0, DateTime.Now, DateTime.Now, user.Code_user) : new Entrie(-1, user.Pseudo, ChannelSource, user.Platform, poke.Name_FR, 0, 1, DateTime.Now, DateTime.Now, user.Code_user)).Validate(true);
            }
        }

        /// <summary>
        /// Make a string withtout '_', accentless, lower
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string StringifyChange(string input)
        {
            return input.ToLower()
                .Replace("_", " ")
                .Replace("é", "e")
                .Replace("è", "e")
                .Replace("ê", "e")
                .Replace("ë", "e")
                .Replace("à", "a")
                .Replace("â", "a")
                .Replace("ä", "a")
                .Replace("î", "i")
                .Replace("ï", "i")
                .Replace("ö", "o")
                .Replace("ô", "o")
                .Replace("ü", "u")
                .Replace("û", "u")
                .Replace("ù", "u")
                .Replace("ç", "c");
        }

        public static bool isSamePoke(Pokemon pokemonSearched, string name)
        {
            name = StringifyChange(name);
            return (StringifyChange(pokemonSearched.AltName) == name || StringifyChange(pokemonSearched.Name_EN) == name || StringifyChange(pokemonSearched.Name_FR) == name);
        }

        public static string CapitalizePhrase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            string[] words = input.Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = CapitalizeString(words[i]);
            }
            return string.Join(' ', words);
        }

        public static string CapitalizeString(string input)
        {
            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        public static string FullInfoShinyNormal(string v)
        {
            if (v.Split('#').Length == 2)
            {
                return v.Split('#')[0];
            }
            return v;
        }

        public static void AddRecords(string mode, Pokemon boss, bool shiny, DataConnexion dataConnexion)
        {
            Records record = new Records(boss.Name_FR, shiny ? "shiny" : "normal", mode, DateTime.Now);
            dataConnexion.AddRecord(record);
        }

        internal static string GetTranslatedType(GlobalAppSettings gas, string type2)
        {
            string resultat = String.Empty;
            try
            {
                switch (type2.ToLower())
                {
                    case "fire":
                        resultat = gas.Texts.Types.fire ?? type2.ToLower();
                        break;

                    case "water":
                        resultat = gas.Texts.Types.water;
                        break;

                    case "grass":
                        resultat = gas.Texts.Types.grass;
                        break;

                    case "electric":
                        resultat = gas.Texts.Types.electric;
                        break;

                    case "ground":
                        resultat = gas.Texts.Types.ground;
                        break;

                    case "rock":
                        resultat = gas.Texts.Types.rock;
                        break;

                    case "flying":
                        resultat = gas.Texts.Types.flying;
                        break;

                    case "bug":
                        resultat = gas.Texts.Types.bug;
                        break;

                    case "poison":
                        resultat = gas.Texts.Types.poison;
                        break;

                    case "ice":
                        resultat = gas.Texts.Types.ice;
                        break;

                    case "psychic":
                        resultat = gas.Texts.Types.psychic;
                        break;

                    case "ghost":
                        resultat = gas.Texts.Types.ghost;
                        break;

                    case "dragon":
                        resultat = gas.Texts.Types.dragon;
                        break;

                    case "dark":
                        resultat = gas.Texts.Types.dark;
                        break;

                    case "steel":
                        resultat = gas.Texts.Types.steel;
                        break;

                    case "fairy":
                        resultat = gas.Texts.Types.fairy;
                        break;

                    case "fighting":
                        resultat = gas.Texts.Types.fighting;
                        break;

                    case "normal":
                        resultat = gas.Texts.Types.normal;
                        break;
                }
            }
            catch { }

            return resultat;
        }

        private static string Normalize(string input) =>
          input.Trim()
         .Replace(' ', '_')
         .Replace('’', '\''); // Remplace l’apostrophe typographique par l’apostrophe standard

        public static bool CompareStrings(string first, string second)
        {
            if (first == null && second == null)
            {
                return true;
            }
            if (first == null || second == null)
            {
                return false;
            }
            return Normalize(first).Trim().Replace(' ', '_').Equals(Normalize(second).Trim().Replace(' ', '_'), StringComparison.OrdinalIgnoreCase);
        }

        public static void Logger(string message)
        {
            try
            {
                Console.WriteLine("\r");
                List<string> parts = message.Split('|').ToList();
                foreach (string part in parts)
                {
                    string color = part.Split('#')[0];
                    string msg = part.Split('#')[1];

                    switch (color.ToLower())
                    {
                        case "blue":
                            Console.ForegroundColor = ConsoleColor.DarkBlue;
                            break;

                        case "red":
                            Console.ForegroundColor = ConsoleColor.Red;
                            break;

                        case "yellow":
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            break;

                        case "aqua":
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            break;

                        case "green":
                            Console.ForegroundColor = ConsoleColor.Green;
                            break;

                        case "orange":
                            Console.ForegroundColor = ConsoleColor.DarkYellow;
                            break;

                        case "pink":
                            Console.ForegroundColor = ConsoleColor.Magenta;
                            break;

                        default:
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }
                    Console.Write(msg);
                    Console.ForegroundColor = ConsoleColor.White;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while logging :  " + e.Message + "\n" + e.Data);
            }
        }

        /// <summary>
        /// Retourne la zone de base
        /// </summary>
        /// <returns></returns>
        public static Zone GetBaseZone()
        {
            return new Zone
            {
                Name = "<void>",
                Description = "Zone de base, sans description. Aucune condition requise pour y accéder.",
                DexRequirement = 0,
                LevelRequirement = 0,
                Image = "https://archives.bulbagarden.net/media/upload/thumb/b/bc/Vermilion_Forest.png/1200px-Vermilion_Forest.png"
            };
        }

        public static string GetStringNumber(int element)
        {
            string result = string.Empty;
            float rounded = 0;
            if (element < 1000)
            {
                rounded = element;
                result = $"{rounded}";
            }
            else if (element < 1000000)
            {
                rounded = element;
                result = $"{Math.Round(rounded / 1000, 2)}K";
            }
            else
            {
                rounded = element;
                result = $"{Math.Round(rounded / 1000000, 2)}M";
            }
            return result;
        }

        public static string CleanFileName(string fileName)
        {
            string invalidChars = new string(Path.GetInvalidFileNameChars());
            string cleanedFileName = Regex.Replace(fileName, "[" + Regex.Escape(invalidChars) + "]", "_");
            return cleanedFileName;
        }

        public static string DefaultHTMLStart(bool needGetParent)
        {
            string init = needGetParent ? "../" : "./";
            return @$"
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
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}main.html"" style=""color: white;"">Accueil Pokédex</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}commandgenerator.html"" style=""color: white;"">Command Generator</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}raid.html"" style=""color: white;"">Raid Result</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}availablepokemon.html"" style=""color: white;"">Pokédex Infos</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}pokestats.html"" style=""color: white;"">Classements</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}buypokemon.html"" style=""color: white;"">Acheter Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}scrappokemon.html"" style=""color: white;"">Scrap Pokémon</a>
        <a class=""btn btn-sm btn-outline-secondary"" href=""{init}records.html"" style=""color: white;"">Enregistrements</a>
      </form>
    </nav><br><br>";
        }

        public static string DefaultHTMLEnd()
        {
            return @"
<br><br>
    <!-- Bootstrap JS, Popper.js, and jQuery -->
    <script src=""https://code.jquery.com/jquery-3.5.1.slim.min.js""></script>
    <script src=""https://cdn.jsdelivr.net/npm/@popperjs/core@2.9.2/dist/umd/popper.min.js""></script>
    <script src=""https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js""></script>
</body>
</html>
";
        }
    }
}