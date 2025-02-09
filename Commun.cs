using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
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
        public static async Task<string> UploadFileAsync(string filepath, GlobalAppSettings globalAppSettings)
        {
            string token = globalAppSettings.GitHubTokenUpload;
            string owner = "MythMega";
            string repos = "PKServExports";
            string path = "exports";

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
                string url = $"https://api.github.com/repos/{owner}/{repos}/contents/{path}/{Path.GetFileName(filepath)}";

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
                .Replace("à", "a")
                .Replace("â", "a");
        }

        public static bool isSamePoke(Pokemon pokemonSearched, string name)
        {
            name = StringifyChange(name);
            return (StringifyChange(pokemonSearched.AltName) == name || StringifyChange(pokemonSearched.Name_EN) == name || StringifyChange(pokemonSearched.Name_FR) == name);
        }
    }
}