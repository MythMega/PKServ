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
                            var jsonResponse = JsonDocument.Parse(responseContent);
                            return jsonResponse.RootElement.GetProperty("link").GetString();
                        }
                        else
                        {
                            throw new Exception("File upload failed.");
                        }
                    }
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