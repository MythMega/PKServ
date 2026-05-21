using PKServ.Business.Exports.JsonExporters;
using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PKServ.Controller
{
    /// <summary>
    /// Chargement initial et rechargement à chaud des données de configuration.
    /// Extrait de Program afin d'être réutilisable depuis SystemController.
    /// </summary>
    public static class DataLoader
    {
        // ── Chargement principal ──────────────────────────────────────

        public static void LoadAllData(
            AppSettings       settings,
            GlobalAppSettings globalAppSettings,
            DataConnexion     data,
            List<User>        usersHere)
        {
            JsonSerializerOptions options = Commun.GetJsonSerializerOptions();

            // Réinitialisation des listes
            settings.allPokemons             = [];
            settings.pokeballs               = [];
            settings.triggers                = [];
            settings.badges                  = [];
            settings.TrainerCardsBackgrounds = [];
            settings.customOverlays          = [];
            settings.Zones                   = [];
            settings.SeriesData              = [];
            settings.pokemons                = [];
            settings.giveaways               = [];
            settings.Zones.Add(Commun.GetBaseZone());

            // Données de base
            settings.allPokemons.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./Data/StreamDex/Creatures.json"), options));
            settings.pokeballs.AddRange(JsonSerializer.Deserialize<List<Pokeball>>(File.ReadAllText("./Data/StreamDex/Balls.json"), options));
            settings.triggers.AddRange(JsonSerializer.Deserialize<List<Trigger>>(File.ReadAllText("./Data/StreamDex/Triggers.json"), options));
            settings.TrainerCardsBackgrounds.AddRange(JsonSerializer.Deserialize<List<Background>>(File.ReadAllText("./Data/StreamDex/TrainerCardBackgrounds.json"), options));
            settings.badges.AddRange(JsonSerializer.Deserialize<List<Badge>>(File.ReadAllText("./Data/StreamDex/badges.json"), options).Where(x => !x.Locked).ToList());
            settings.customOverlays.AddRange(JsonSerializer.Deserialize<List<CustomOverlay>>(File.ReadAllText("./Data/StreamDex/Overlays.json"), options));
            settings.giveaways.AddRange(JsonSerializer.Deserialize<List<Giveaway>>(File.ReadAllText("./Data/StreamDex/Giveaways.json"), options));

            // Données custom
            LoadCustomBadges(settings);
            LoadCustomBalls(settings);
            LoadCustomCreature(settings);
            LoadCustomOverlay(settings);
            LoadCustomTrainerCardsBackground(settings);
            LoadCustomGiveaway(settings);
            LoadCustomZone(settings);

            var settingsLocal = settings;
            var globalLocal   = globalAppSettings;

            settings.customOverlays.ForEach(async overlay =>
            {
                overlay.SetEnv(data, settingsLocal, globalLocal, usersHere);
                await overlay.BuildOverlay(true);
            });

            new OverlayGeneration(data, settings, globalAppSettings, usersHere).FirstRun();

            settings.pokemons = settings.allPokemons.Where(p => p.enabled).ToList();
            settings.allPokemons.ForEach(p => p.SetData(settingsLocal.Zones));
            settings.giveaways = GiveawayInitializer.GetGiveaways(settings);

            settings.pokemons.Select(p => p.Serie).Distinct().ToList()
                .ForEach(serie => settingsLocal.SeriesData.Add(
                    (serie, settingsLocal.pokemons.Count(p => p.Serie == serie))));

            // Génération du dex principal
            GenerateDexFull(data, settings, globalAppSettings);

            // Exports JSON statiques en parallèle
            StaticFileCopier.EnsureDataDirectories();
            System.Threading.Tasks.Task.WhenAll(
                System.Threading.Tasks.Task.Run(() => JsonExportCreature.ExportCreaturesList(settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportBall.ExportBallsList(settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportZone.ExportZonesList(settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportBuyList(settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportScrapList(settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportPokeStats(data, settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportRecords(data, settings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportMain(data, settings, globalAppSettings)),
                System.Threading.Tasks.Task.Run(() => JsonExportPages.ExportCommandGeneratorData(settings, globalAppSettings))
            ).Wait();
        }

        public static void LogInitialsDatas(AppSettings settings, GlobalAppSettings globalAppSettings, List<User> usersHere)
        {
            Commun.Logger($"yellow#{globalAppSettings.Texts.serverStarted}");
            Commun.Logger($"white#Nombre de pokémon chargé : |red#{settings.pokemons.Count}");
            Commun.Logger($"white#Nombre de series chargé : |red#{settings.SeriesData.Count}");
            Commun.Logger($"white#Nombre de pokeball chargé : |red#{settings.pokeballs.Count}");
            Commun.Logger($"white#Nombre de triggers chargé : |red#{settings.triggers.Count}");
            Commun.Logger($"white#Nombre de badges chargé : |red#{settings.badges.Count}");
            Commun.Logger($"white#Nombre de custom overlays chargé : |red#{settings.customOverlays.Count}");
            Commun.Logger($"white#Nombre de Background Trainer Card chargé : |red#{settings.TrainerCardsBackgrounds.Count}");
            Commun.Logger($"white#Nombre de Code de Distributions chargé : |red#{settings.giveaways.Count}");
            Commun.Logger($"white#Nombre d'utilisateurs chargés dans le giveaway : |red#{usersHere.Where(uh => uh.Platform != "system").Count()}");
            Commun.Logger($"aqua#Listening on port |yellow#{globalAppSettings.ServerPort}|aqua# , so send your request at |blue#http://localhost:|yellow#{globalAppSettings.ServerPort}");
            if (globalAppSettings.Log.logConsole.console)
            {
                Console.WriteLine("\nServer settings (you can change those settings in _settings.json)");
                Commun.Logger($"aqua#Log infos on console : |yellow#{globalAppSettings.Log.logConsole.console}");
                Commun.Logger($"aqua#Log Json on console (require infos on console) : |yellow#{globalAppSettings.Log.logConsole.logJsonOnConsole}");
                Commun.Logger($"aqua#Log also on File : |yellow#{globalAppSettings.Log.logFile}");
            }
        }

        private static string GenerateDexFull(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                StaticFileCopier.EnsureDataDirectories();
                JsonExportPages.ExportMain(data, settings, globalAppSettings);
                string result = "Génération JSON main.json effectuée avec succès.";
                if (globalAppSettings.Log.logConsole.console)
                    Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return globalAppSettings.Texts.error;
            }
        }

        // ── Chargements custom ────────────────────────────────────────

        private static void LoadCustomTrainerCardsBackground(AppSettings settings)
        {
            const string path = "./Data/Custom/TrainerCardsBackgrounds";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Background>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.TrainerCardsBackgrounds.AddRange(items);
                    Commun.Logger($"white#Custom Trainer Cards Background loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomOverlay(AppSettings settings)
        {
            const string path = "./Data/Custom/Overlays";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<CustomOverlay>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.customOverlays.AddRange(items);
                    Commun.Logger($"white#Custom Overlay loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomGiveaway(AppSettings settings)
        {
            const string path = "./Data/Custom/Giveaways";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Giveaway>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.giveaways.AddRange(items);
                    Commun.Logger($"white#Custom Giveaway loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomBalls(AppSettings settings)
        {
            const string path = "./Data/Custom/Balls";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Pokeball>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.pokeballs.AddRange(items);
                    Commun.Logger($"white#Custom Balls loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomBadges(AppSettings settings)
        {
            const string path = "./Data/Custom/Badges";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Badge>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.badges.AddRange(items);
                    Commun.Logger($"white#Custom Badges loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomCreature(AppSettings settings)
        {
            const string path = "./Data/Custom/Creatures";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    items.ForEach(p => p.isCustom = true);
                    settings.allPokemons.AddRange(items);
                    Commun.Logger($"white#Custom Pokémon loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }

        private static void LoadCustomZone(AppSettings settings)
        {
            const string path = "./Data/Custom/Zones";
            Directory.CreateDirectory(path);
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    var items = JsonSerializer.Deserialize<List<Zone>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.Zones.AddRange(items);
                    Commun.Logger($"white#Zones loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{items.Count}|white#.");
                }
                catch (Exception e) { Console.WriteLine($"Error reading {file}: {e.Message}"); }
            }
        }
    }
}
