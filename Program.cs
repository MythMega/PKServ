using PKServ.Admin;
using PKServ.Business;
using PKServ.Business._Tool;
using PKServ.Business.Admin;
using PKServ.Business.Exports.JsonExporters;
using PKServ.Business.Overlay;
using PKServ.Business.Raid;
using PKServ.Configuration;
using PKServ.Controller;
using PKServ.Entity;
using PKServ.Entity.Raid;
using PKServ.Entity.Raid.ManualRandomRaid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            #region Initiatlization

            DataConnexion data = new();
            data.Initialize();
            // raid auto
            DateTime LastRaidCheck = DateTime.Now;

            var options = Commun.GetJsonSerializerOptions();
            if (!File.Exists("./_settings.json"))
            {
                File.WriteAllText("./_settings.json", File.ReadAllText("Admin\\DefaultSettings.json"));
            }
            GlobalAppSettings globalAppSettings = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("./_settings.json"), options);
            GlobalAppSettings globalAppSettingsDefaults = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("Admin\\DefaultSettings.json"), options);
            SettingsHelper.MergeWithDefaults(globalAppSettings, globalAppSettingsDefaults);
            globalAppSettings.FixType(globalAppSettingsDefaults);
            File.WriteAllText("./_settings.json", JsonSerializer.Serialize(globalAppSettings, options));
            bool AutoRaid = globalAppSettings.RaidSettings.AutoRaidSettings.Enabled;
            int autoRaidCount = 0;
            AppSettings settings = new();
            List<User> usersHere = [];

            globalAppSettings.LanguageCode = globalAppSettings.LanguageCode.ToLower();
            if (globalAppSettings.LanguageCode == "fr")
            {
                settings.allPokemons.ForEach(p => { if (p.AltNameForced) { p.AltName = p.Name_FR; } });
            }
            else if (globalAppSettings.LanguageCode == "en")
            {
                settings.allPokemons.ForEach(p => { if (p.AltNameForced) { p.AltName = p.Name_EN; } });
            }

            if (globalAppSettings.KeepUserInGiveAwayAfterShutdown)
            {
                try
                {
                    usersHere.AddRange(JsonSerializer.Deserialize<List<User>>(File.ReadAllText("./user.data"), options));
                }
                catch { }
            }

            LoadAllData(ref settings, ref globalAppSettings, data, usersHere);

            LogInitialsDatas(settings, globalAppSettings, usersHere);

            // ── Prompt d'export au démarrage (5 secondes) ────────────────────
            PromptStartupExport(data, settings, globalAppSettings);
            // ─────────────────────────────────────────────────────────────────

            DateTime lastExportTime = DateTime.Now;
            CheckScheduledTasks(globalAppSettings, true);

            await DataFixerImpl.FixEntries(globalAppSettings, data);
            await DataFixerImpl.FixDuplicateUsers(data);
            await DataFixerImpl.FixChangedUsernameInEntries(globalAppSettings, data);

            #endregion Initiatlization

            // ── Contrôleurs ───────────────────────────────────────────────────
            var controllerCtx = new ControllerContext
            {
                Settings = settings,
                GlobalSettings = globalAppSettings,
                Data = data,
                UsersHere = usersHere,
                JsonOptions = options,
                AutoRaid = globalAppSettings.RaidSettings.AutoRaidSettings.Enabled,
                LastRaidCheck = DateTime.Now,
                AutoRaidCount = 0,
            };

            var routedControllers = new System.Collections.Generic.Dictionary<string, BaseController>(StringComparer.OrdinalIgnoreCase)
            {
                ["Zone"] = new ZoneController(controllerCtx),
                ["Raid"] = new RaidController(controllerCtx),
                ["Trade"] = new TradeController(controllerCtx),
                ["Giveaway"] = new GiveawayController(controllerCtx),
                ["Interface"] = new InterfaceController(controllerCtx),
                ["Debug"] = new DebugController(controllerCtx),
                ["System"] = new SystemController(controllerCtx),
            };
            var generalController = new GeneralController(controllerCtx);
            // ─────────────────────────────────────────────────────────────────

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{globalAppSettings.ServerPort}/");
            listener.Start();

            // ── Démarrage du logger de requêtes ───────────────────────────────
            RequestLogger.Start("logs");
            RequestLogger.Info($"PKServ démarré sur le port {globalAppSettings.ServerPort}");

            // ── Génération des overlays de raid ───────────────────────────────
            // raidOverlay.html    : overlay existant (polling toutes les 10s)
            // current_raid.html   : overlay SSE temps réel (généré une seule fois)
            await RaidOverlayImpl.WriteOverlay(globalAppSettings);
            await RaidOverlayImpl.WriteRealtimeOverlay(globalAppSettings);
            // Génération unique de tous les overlays SSE génériques (si absents)
            await OverlaySseHtmlFactory.GenerateAllAsync(globalAppSettings);
            // ─────────────────────────────────────────────────────────────────

            // ── Boucle de fond pour les tâches planifiées ─────────────────────
            // CheckScheduledTasks était appelé uniquement sur les requêtes GET,
            // ce qui l'empêchait de s'exécuter quand le trafic est majoritairement
            // SSE ou POST. On le déplace dans un Task.Run indépendant.
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    try   { CheckScheduledTasks(globalAppSettings); }
                    catch (Exception e) { Console.WriteLine($"Error in scheduled tasks loop: {e.Message}"); }
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
            });
            // ─────────────────────────────────────────────────────────────────

            await Task.Run(async () =>
            {
                while (true)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    //Console.WriteLine(request.);
                    // Ajouter les en-têtes CORS
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

                    // Répondre immédiatement aux preflight OPTIONS (navigateur CORS)
                    if (request.HttpMethod == "OPTIONS")
                    {
                        response.StatusCode = 200;
                        response.ContentLength64 = 0;
                        response.Close();
                        continue;
                    }

                    string urlPath = request.Url.LocalPath.Trim('/');
                    string firstSegment = urlPath.Contains('/') ? urlPath[..urlPath.IndexOf('/')] : urlPath;
                    BaseController ctrl = routedControllers.TryGetValue(firstSegment, out var c) ? c : generalController;
                    string responseString = "";

                    // ── Endpoint SSE pour l'overlay raid temps réel ──────────────────
                    // La route GET /current_raid_stream est interceptée ICI, avant le routage normal,
                    // car elle nécessite une connexion longue durée (la réponse ne se ferme pas).
                    //
                    // Détection de déconnexion : on utilise un CancellationTokenSource lié à
                    // l'ApplicationStopping, mais la déconnexion réelle est détectée dans
                    // RegisterClientAsync quand l'écriture sur le stream échoue (IOException).
                    // On NE lit PAS request.InputStream : sur une requête GET sans body,
                    // Read() retourne 0 immédiatement et annulerait le token tout de suite.
                    if (request.HttpMethod == "GET" && urlPath == "current_raid_stream")
                    {
                        var cts = new CancellationTokenSource();
                        _ = RaidSseManager.RegisterClientAsync(response, settings, globalAppSettings, cts.Token);
                        continue;
                    }
                    // ────────────────────────────────────────────────────────────────

                    // ── Endpoints /overlay/<name> et /overlay/<name>/stream ──────────
                    // /overlay/<name>/stream : connexion SSE longue durée (même principe que current_raid_stream)
                    // /overlay/<name>        : sert le fichier HTML statique depuis StreamOverlays/<name>.html
                    // /overlay/raid          : alias vers current_raid.html (overlay SSE du raid)
                    if (request.HttpMethod == "GET" && urlPath.StartsWith("overlay/", StringComparison.OrdinalIgnoreCase))
                    {
                        string overlaySegment = urlPath["overlay/".Length..]; // ex: "raid/stream" ou "raid"

                        if (overlaySegment.EndsWith("/stream", StringComparison.OrdinalIgnoreCase))
                        {
                            // Connexion SSE longue durée sur un canal générique
                            string channel = overlaySegment[..^"/stream".Length]; // ex: "raid"
                            _ = PKServ.Business.Overlay.OverlaySseManager.RegisterClientAsync(
                                channel,
                                response,
                                () => PKServ.Business.Overlay.OverlaySseInitialPayload.Build(channel, settings, globalAppSettings, usersHere, data),
                                new CancellationToken());
                            continue;
                        }
                        else
                        {
                            // Sert le fichier HTML statique correspondant
                            string channel  = overlaySegment; // ex: "raid"
                            string htmlPath = Path.Combine(AppContext.BaseDirectory, "StreamOverlays", channel + ".html");
                            if (File.Exists(htmlPath))
                            {
                                string html = await File.ReadAllTextAsync(htmlPath);
                                byte[] htmlBytes = System.Text.Encoding.UTF8.GetBytes(html);
                                response.ContentType = "text/html; charset=utf-8";
                                response.ContentLength64 = htmlBytes.Length;
                                response.AddHeader("Cache-Control", "no-cache");
                                await response.OutputStream.WriteAsync(htmlBytes);
                                response.Close();
                            }
                            else
                            {
                                response.StatusCode = 404;
                                response.Close();
                            }
                            continue;
                        }
                    }
                    // ────────────────────────────────────────────────────────────────
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream))
                        {
                            string requestBody = reader.ReadToEnd();

                            if (globalAppSettings.Log.logConsole.console)
                            {
                                Console.WriteLine($"{DateTime.Now:yyyy MM dd - HH:mm:ss} - entrée : {urlPath}");
                                if (globalAppSettings.Log.logConsole.logJsonOnConsole)
                                    Console.WriteLine(requestBody);
                            }

                            var (sw, reqId) = RequestLogger.Enter("POST", urlPath, requestBody);
                            try
                            {
                                responseString = await ctrl.HandlePostAsync(urlPath, requestBody);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine($"CRITICAL POST ERROR : {e.Message}\n{e.Data}\n{e.Source}\n{e.StackTrace}{e.InnerException}\n");
                                responseString = "";
                            }
                            finally
                            {
                                RequestLogger.Done("POST", urlPath, sw, reqId, responseString);
                            }
                        }
                    }
                    if (request.HttpMethod == "GET")
                    {
                        var (sw, reqId) = RequestLogger.Enter("GET", urlPath, request.QueryString.ToString());
                        try
                        {
                            responseString = await ctrl.HandleGetAsync(urlPath, request.QueryString);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"CRITICAL GET ERROR :  {e.Message}\n{e.Data}");
                            responseString = "";
                        }
                        finally
                        {
                            RequestLogger.Done("GET", urlPath, sw, reqId, responseString);
                        }

                        _ = Task.Run(() =>
                        {
                            var bgSw = System.Diagnostics.Stopwatch.StartNew();
                            try
                            {
                                RequestLogger.BackgroundDone("checkScheduledTasks", bgSw);
                            }
                            catch (Exception e)
                            {
                                RequestLogger.BackgroundError("checkScheduledTasks", e);
                                Console.WriteLine($"Error while executing scheduled task : {e.Message}\n{e.Data}");
                            }
                        });
                    }

                    responseString = (responseString ?? "")
                        .Replace("<", "\uFF1C")
                        .Replace(">", "\uFF1E");

                    if (globalAppSettings.Log.logConsole.console)
                        //Console.WriteLine($"Réponse envoyée à twitchat : {responseString}");

                    if (responseString is null)
                    {
                        responseString = "";
                        Console.WriteLine("ERROR WHILE EXECUTING REQUEST : RESPONSESTRING IS NULL");
                    }
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    using (Stream output = response.OutputStream)
                    {
                        output.Write(buffer, 0, buffer.Length);
                    }

                    // Auto Export — fire-and-forget : l'export ne bloque plus la boucle de traitement des requêtes.
                    // lastExportTime est mis à jour immédiatement pour éviter un double-déclenchement
                    // si plusieurs requêtes arrivent pendant la fenêtre d'export.
                    if (globalAppSettings.MustAutoFullExport && lastExportTime.AddMinutes(globalAppSettings.DelayBeforeFullWebUpdate) < DateTime.Now)
                    {
                        lastExportTime = DateTime.Now;
                        var exportController = (InterfaceController)routedControllers["Interface"];
                        var exportSettings   = settings;
                        var exportGlobal     = globalAppSettings;
                        var exportData       = data;
                        var exportUsers      = usersHere;
                        RequestLogger.Info("Auto-export déclenché (fire-and-forget)");
                        _ = Task.Run(() =>
                        {
                            // ── FullExport ───────────────────────────────────
                            var swExport = System.Diagnostics.Stopwatch.StartNew();
                            try
                            {
                                string result = exportController.FullExport(forced: false, assets: false);
                                RequestLogger.BackgroundDone("FullExport", swExport, result);
                                if (exportGlobal.Log.logConsole.console)
                                    Console.WriteLine($"Export done : {result}");
                            }
                            catch (Exception e)
                            {
                                RequestLogger.BackgroundError("FullExport", e);
                                if (exportGlobal.Log.logConsole.console)
                                {
                                    Console.WriteLine("Export not possible.");
                                    Console.WriteLine(e.Message);
                                    Console.WriteLine(e.StackTrace);
                                    Console.WriteLine(e.Source);
                                }
                            }

                            // ── Custom overlays ──────────────────────────────
                            var swOverlay = System.Diagnostics.Stopwatch.StartNew();
                            try
                            {
                                List<CustomOverlay> overlayResets = []; //JsonSerializer.Deserialize<List<CustomOverlay>>(File.ReadAllText("./customOverlays.json"), options);
                                foreach (CustomOverlay overlayReset in overlayResets)
                                    exportSettings.customOverlays
                                        .Find(o => o.Filename == overlayReset.Filename)!.Content = overlayReset.Content;

                                Task.WhenAll(exportSettings.customOverlays
                                    .Select(o => o.BuildOverlay(false))).Wait();
                                RequestLogger.BackgroundDone("BuildOverlays", swOverlay);
                            }
                            catch (Exception e)
                            {
                                RequestLogger.BackgroundError("BuildOverlays", e);
                                Console.WriteLine("Error while Building custom Overlays : " + e.Message);
                            }

                            // ── TextsUpdate ──────────────────────────────────
                            var swTexts = System.Diagnostics.Stopwatch.StartNew();
                            try
                            {
                                new OverlayGeneration(exportData, exportSettings, exportGlobal, exportUsers).TextsUpdate();
                                RequestLogger.BackgroundDone("TextsUpdate", swTexts);
                            }
                            catch (Exception e)
                            {
                                RequestLogger.BackgroundError("TextsUpdate", e);
                                Console.WriteLine("Error while generating PKServ Overlay : " + e.Message + "\n" + e.Data);
                            }
                        });
                    }

                    if (controllerCtx.AutoRaid && controllerCtx.AutoRaidCount < globalAppSettings.RaidSettings.AutoRaidSettings.MaxRaidCountPerSession &&
                    globalAppSettings.RaidSettings.AutoRaidSettings is not null &&
                    controllerCtx.LastRaidCheck.AddMinutes(globalAppSettings.RaidSettings.AutoRaidSettings.DelayBetweenRaids) < DateTime.Now &&
                    settings.ActiveRaid is null
                    )
                    {
                        try
                        {
                            SettingsCheckerImpl.CheckAutoraid(globalAppSettings.RaidSettings.AutoRaidSettings, settings);
                            settings.ActiveRaid = new Raid(globalAppSettings.RaidSettings.AutoRaidSettings, settings, usersHere.Count, data);
                            // Raid auto lancé → notifie immédiatement les clients SSE connectés
                            RaidSseManager.BroadcastRaidState(settings, globalAppSettings);
                            OverlaySseBroadcaster.BroadcastRaid(settings, globalAppSettings);
                            autoRaidCount++;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error while checking AutoRaid : " + e.Message + "\n" + e.Data);
                        }
                    }
                }
            });

            Console.ReadLine();
            listener.Stop();
        }

        private static void LoadAllData(ref AppSettings settings, ref GlobalAppSettings globalAppSettings, DataConnexion data, List<User> usersHere)
        {
            DataLoader.LoadAllData(settings, globalAppSettings, data, usersHere);
        }

        private static void LogInitialsDatas(AppSettings settings, GlobalAppSettings globalAppSettings, List<User> usersHere)
        {
            DataLoader.LogInitialsDatas(settings, globalAppSettings, usersHere);
        }

        private static void CheckScheduledTasks(GlobalAppSettings settings, bool start = false)
        {
            foreach (ScheduledTask task in settings.ScheduledTasks)
            {
                bool condition = false;

                switch (task.DelayType.ToString())
                {
                    case "seconds":
                        condition = DateTime.Now > task.LastExecution.AddSeconds(task.Delay);
                        break;

                    case "minutes":
                        condition = DateTime.Now > task.LastExecution.AddMinutes(task.Delay);
                        break;

                    case "hours":
                        condition = DateTime.Now > task.LastExecution.AddHours(task.Delay);
                        break;
                }

                if ((task.ExecuteAtStart && start) || condition)
                {
                    task.LastExecution = DateTime.Now;
                    if (File.Exists(task.ProcessFilePath))
                    {
                        Process process = new Process();
                        process.StartInfo.FileName = task.ProcessFilePath;
                        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(task.ProcessFilePath);
                        process.StartInfo.RedirectStandardOutput = true;
                        process.StartInfo.UseShellExecute = false;
                        process.StartInfo.CreateNoWindow = true;

                        process.Start();

                        Commun.Logger($"white#Task |red#{Path.GetFileName(task.ProcessFilePath)}|white# success.");
                    }
                    else
                    {
                        Console.WriteLine("\n" + task.ProcessFilePath + " not found.\n");
                    }
                }
            }
        }

        private static string SYS_GenerateAvailableDex(AppSettings settings, DataConnexion data, GlobalAppSettings _params)
        {
            // délégué au SystemController — gardé ici pour l'appel depuis checkScheduledTasks si besoin
            return "";
        }

        public static UserRequest tempFixUserRequest(UserRequest ur, DataConnexion db)
        {
            // délégué à ControllerContext.TempFixUserRequest
            if (ur.UserCode == "unset")
            {
                var found = db.GetAllUserPlatforms().Find(u => u.Pseudo == ur.UserName && u.Platform == ur.Platform);
                if (found is not null)
                    ur.UserCode = db.GetCodeUserByPlatformPseudo(new User(ur.UserName, ur.Platform));
            }
            return ur;
        }

        /// Affiche un prompt au démarrage : si l'utilisateur appuie sur Entrée dans les 5 secondes,
        /// un export complet de tous les utilisateurs, zones, balls, etc. est déclenché.
        /// Passé ce délai, le prompt est ignoré silencieusement.
        /// </summary>
        private static void PromptStartupExport(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            const int timeoutSeconds = 5;
            Commun.Logger($"white#⏱  Appuyez sur |yellow#Entrée|white# dans les |yellow#{timeoutSeconds} secondes|white# pour déclencher un export complet au démarrage...");

            using var cts = new System.Threading.CancellationTokenSource();
            var keyTask = Task.Run(() => { try { Console.ReadLine(); } catch { } }, cts.Token);
            bool pressed = keyTask.Wait(TimeSpan.FromSeconds(timeoutSeconds));
            cts.Cancel();

            if (pressed)
            {
                Commun.Logger("white#▶  Export de démarrage lancé...");
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    var users = data.GetAllUserPlatforms();
                    Task.WhenAll(users.Select(u =>
                        JsonExportUser.ExportUserAsync(u, data, settings, globalAppSettings))).Wait();
                    JsonExportUser.ExportUsersByPlatform(users);

                    StaticFileCopier.EnsureDataDirectories();
                    Task.WhenAll(
                        JsonExportPages.ExportMainAsync(data, settings, globalAppSettings),
                        JsonExportPages.ExportPokeStatsAsync(data, settings, globalAppSettings),
                        JsonExportPages.ExportRecordsAsync(data, settings),
                        JsonExportPages.ExportBuyListAsync(settings, globalAppSettings),
                        JsonExportPages.ExportScrapListAsync(settings, globalAppSettings),
                        JsonExportPages.ExportCommandGeneratorDataAsync(settings, globalAppSettings),
                        JsonExportPages.ExportRankingsAsync(data, settings, globalAppSettings),
                        Task.Run(() => JsonExportCreature.ExportCreaturesList(settings, globalAppSettings)),
                        Task.Run(() => JsonExportBall.ExportBallsList(settings, globalAppSettings)),
                        Task.Run(() => JsonExportZone.ExportZonesList(settings, globalAppSettings))
                    ).Wait();

                    sw.Stop();
                    Commun.Logger($"white#✅  Export terminé en |yellow#{sw.Elapsed.TotalSeconds:F1}s|white# ({users.Count} utilisateurs).");
                }
                catch (Exception ex)
                {
                    Commun.Logger($"red#❌  Erreur lors de l'export de démarrage : {ex.Message}");
                }
            }
            else
            {
                Commun.Logger("white#⏭  Délai écoulé, export de démarrage ignoré.");
            }
        }
    }
}