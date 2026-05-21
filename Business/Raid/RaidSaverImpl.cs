using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Business.Raid
{
    public class RaidSaverImpl
    {
        public static string SaveRaid(AppSettings settings, JsonSerializerOptions options)
        {
            if (settings.ActiveRaid is null)
            {
                return "Error : No active raid.";
            }
            try
            {
                string fileSaveRaid = Path.Combine("Data", "StreamDex", "ActiveRaid.data");
                if (!File.Exists(fileSaveRaid))
                {
                    File.Create(fileSaveRaid);
                }
            }
            catch
            {
                return "Error during ActiveRaid.data file creation.";
            }
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"name={settings.ActiveRaid.BossName}");
            sb.AppendLine($"maxhp={settings.ActiveRaid.PVMax}");
            sb.AppendLine($"hp={settings.ActiveRaid.PV}");
            sb.AppendLine($"startedtime={settings.ActiveRaid.StartedTime.ToString("yyyy-MM-ddTHH:mm:ss")}");
            sb.AppendLine($"defeatedtime={(settings.ActiveRaid.DefeatedTime is null ? "" : settings.ActiveRaid.DefeatedTime.Value.ToString("yyyy-MM-ddTHH:mm:ss"))}");
            // pour lire ça : DateTime parsedDate = DateTime.ParseExact(dateString, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
            sb.AppendLine($"userdamagebase={DictStringIntToString(settings.ActiveRaid.UserDamageBase)}");
            sb.AppendLine($"usercodecatchstatut={DictStringIntToString(settings.ActiveRaid.UserCodeCatchStatut)}");
            sb.AppendLine($"platformdamage={DictStringIntToString(settings.ActiveRaid.Stats.PlatformDamage)}");
            sb.AppendLine($"userdamagecount={DictStringIntToString(settings.ActiveRaid.Stats.UserDamageCount)}");
            sb.AppendLine($"userdamagetotal={DictStringIntToString(settings.ActiveRaid.Stats.UserDamageTotal)}");
            sb.AppendLine($"displayshiny={(settings.ActiveRaid.DisplayShiny ? "shiny" : "normal")}");

            string resultat = $"raid {settings.ActiveRaid.Boss.Name_FR}/{settings.ActiveRaid.Boss.Name_FR} ({(settings.ActiveRaid.Boss.isShiny ? "shiny" : "normal")}) - {settings.ActiveRaid.PV}/{settings.ActiveRaid.PVMax} HP, {settings.ActiveRaid.UserDamageBase.Count} raiders, saved successfully.";
            settings.ActiveRaid = null;
            return resultat;
        }

        public static string LoadRaid(AppSettings settings, JsonSerializerOptions options, DataConnexion data)
        {
            if (settings.ActiveRaid is not null)
            {
                return "Error : Raid currently in progress, stop it first.";
            }
            try
            {
                string fileLoadRaid = Path.Combine("Data", "StreamDex", "ActiveRaid.json");
                if (!File.Exists(fileLoadRaid))
                {
                    return "Error : Raid File not found.";
                }
                string loadedRaidJson = File.ReadAllText(fileLoadRaid);
                PKServ.Raid loadedRaid = JsonSerializer.Deserialize<PKServ.Raid>(loadedRaidJson, options);
                settings.ActiveRaid = loadedRaid;
                return $"raid {settings.ActiveRaid.Boss.Name_FR}/{settings.ActiveRaid.Boss.Name_FR} ({(settings.ActiveRaid.Boss.isShiny ? "shiny" : "normal")}) - {settings.ActiveRaid.PV}/{settings.ActiveRaid.PVMax} HP, {settings.ActiveRaid.UserDamageBase.Count} raiders, loaded successfully.";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static string DictStringIntToString(Dictionary<string, int> dict)
        {
            if (dict == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var kv in dict)
            {
                // Échapper d'abord les placeholders pour éviter collisions
                string key = kv.Key?.Replace("%DP%", "%25DP%").Replace("%PV%", "%25PV%") ?? string.Empty;
                key = key.Replace(":", "%DP%").Replace(";", "%PV%");
                sb.Append(key).Append(':').Append(kv.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
            }
            return sb.ToString();
        }

        private static Dictionary<string, int> StringToDictStringInt(string str)
        {
            var resultat = new Dictionary<string, int>();
            if (string.IsNullOrEmpty(str)) return resultat;

            string[] pairs = str.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                int colonIndex = pair.IndexOf(':');
                if (colonIndex <= 0) continue;

                string rawKey = pair.Substring(0, colonIndex);
                string rawValue = pair.Substring(colonIndex + 1);

                // Dé-échappement inverse dans le bon ordre
                string key = rawKey.Replace("%DP%", ":").Replace("%PV%", ";").Replace("%25DP%", "%DP%").Replace("%25PV%", "%PV%");
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    // Si la clé existe déjà, on écrase ; ajustez si vous préférez ignorer ou cumuler
                    resultat[key] = value;
                }
                else
                {
                    // Échec de parsing ignoré
                }
            }
            return resultat;
        }
    }
}