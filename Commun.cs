using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text.Encodings.Web;

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
        public static async Task<string> UploadFileAsync(string filepath)
        {
            string token = "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";
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
    }
}