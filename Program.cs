﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            DataConnexion data = new DataConnexion();
            data.Initialize();

            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };

            GlobalAppSettings globalAppSettings = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("./_settings.json"), options);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{globalAppSettings.ServerPort}/");
            listener.Start();

            // définition des données
            AppSettings settings = new();

            List<User> usersHere = [new User("_history", "system")];

            if (globalAppSettings.KeepUserInGiveAwayAfterShutdown)
            {
                try
                {
                    usersHere.AddRange(JsonSerializer.Deserialize<List<User>>(File.ReadAllText("./user.data"), options));
                }
                catch { }
            }

            settings.allPokemons.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options));
            //dumpSprite(settings.allPokemons);
            List<Pokemon> custom = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./customPokemons.json"), options);
            custom.ForEach(p => { p.isCustom = true; });
            settings.allPokemons.AddRange(custom);
            settings.pokemons = settings.allPokemons.Where(p => p.enabled || p == null).ToList();
            settings.pokeballs.AddRange(JsonSerializer.Deserialize<List<Pokeball>>(File.ReadAllText("./balls.json"), options));
            settings.triggers.AddRange(JsonSerializer.Deserialize<List<Trigger>>(File.ReadAllText("./Triggers.json"), options));
            settings.badges.AddRange(JsonSerializer.Deserialize<List<Badge>>(File.ReadAllText("./badges.json"), options).Where(x => !x.Locked).ToList());
            OverlayGeneration overlays = new OverlayGeneration(data, settings, globalAppSettings, usersHere);
            Logger($"yellow#{globalAppSettings.Texts.serverStarted}");
            Logger($"white#Nombre de pokémon chargé : |red#{settings.pokemons.Count}");
            Logger($"white#Nombre de pokeball chargé : |red#{settings.pokeballs.Count}");
            Logger($"white#Nombre de triggers chargé : |red#{settings.triggers.Count}");
            Logger($"white#Nombre de badges chargé : |red#{settings.badges.Count}");
            Logger($"white#Nombre d'utilisateurs chargés dans le giveaway : |red#{usersHere.Where(uh => uh.Platform != "system").Count()}");
            Logger($"aqua#Listening on port |yellow#{globalAppSettings.ServerPort}|aqua# , so send your request at |blue#http://localhost:|yellow#{globalAppSettings.ServerPort}");
            if (globalAppSettings.Log.logConsole.console)
            {
                string settingsServer = "Server settings (you can change those settings in _settings.json)";
                Console.WriteLine("\n" + settingsServer);

                Logger($"aqua#Log infos on console : |yellow#{globalAppSettings.Log.logConsole.console}");
                Logger($"aqua#Log Json on console (require infos on console) : |yellow#{globalAppSettings.Log.logConsole.logJsonOnConsole}");
                Logger($"aqua#Log also on File : |yellow#{globalAppSettings.Log.logFile}");
            }
            DateTime lastExportTime = DateTime.Now;
            generateOverlays(overlays, First:true );
            checkScheduledTasks(globalAppSettings, true);

            Task.Run(() =>
            {
                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;

                    // Ajouter les en-têtes CORS
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Authorization");

                    if (request.HttpMethod == "POST")
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream))
                        {
                            string requestBody = reader.ReadToEnd();

                            string urlPath = request.Url.LocalPath.Trim('/');
                            string responseString = "";
                            UserRequest ctx = null;

                            if (globalAppSettings.Log.logConsole.console)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("yyyy MM dd - HH:mm:ss")} - entrée : {urlPath}");
                                if (globalAppSettings.Log.logConsole.logJsonOnConsole)
                                {
                                    Console.WriteLine(requestBody);
                                }
                            }

                            switch (urlPath)
                            {
                                case "CatchPoke":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    if (globalAppSettings.AutoSignInGiveAway)
                                    {
                                        AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                    }
                                    responseString = CatchPoke(ctx, data, settings, globalAppSettings);
                                    break;

                                case "SignIn":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    User a = JsonSerializer.Deserialize<User>(requestBody, options);
                                    User user = new User
                                    {
                                        Code_user = ctx.UserCode,
                                        Pseudo = a.Pseudo,
                                        Platform = a.Platform
                                    };
                                    responseString = SignIn(user, data, ref usersHere);
                                    break;

                                case "GenerateDexSolo":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    if (globalAppSettings.AutoSignInGiveAway)
                                    {
                                        AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                    }
                                    responseString = GenerateDexSolo(ctx, data, settings, globalAppSettings);
                                    break;

                                case "GenerateDexFull":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    responseString = GenerateDexFull(ctx, data, settings, globalAppSettings);
                                    break;

                                case "GetUserStats":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    if (globalAppSettings.AutoSignInGiveAway)
                                    {
                                        AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                    }
                                    responseString = GetUserStats(ctx, data, settings, globalAppSettings);
                                    break;

                                case "GetUserLevels":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    if (globalAppSettings.AutoSignInGiveAway)
                                    {
                                        AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                    }
                                    responseString = GetUserLevels(ctx, data, settings, globalAppSettings);
                                    break;

                                case "ScrapElement":
                                    Scrapping scrapping = JsonSerializer.Deserialize<Scrapping>(requestBody, options);
                                    scrapping.SetEnv(data, settings, globalAppSettings);
                                    responseString = scrapping.DoResult();
                                    break;

                                case "BuyElement":
                                    Buying buying = JsonSerializer.Deserialize<Buying>(requestBody, options);
                                    buying.SetEnv(data, settings, globalAppSettings);
                                    responseString = buying.DoResult();
                                    break;

                                case "GetOneValue":
                                    SearchValue sv = JsonSerializer.Deserialize<SearchValue>(requestBody, options);
                                    sv.SetEnv(data, settings, globalAppSettings, usersHere);
                                    responseString = sv.searchResult();
                                    break;

                                case "Interface/LaunchBall":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    ctx = tempFixUserRequest(ctx, data);
                                    responseString = API_SendBall(ctx, data, settings, globalAppSettings);
                                    break;

                                case "Interface/GiveAway":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    responseString = API_GiveAway(ctx, data, settings, globalAppSettings, usersHere);
                                    break;

                                case "Interface/FullExport":
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    bool forced = ctx.TriggerName == "API_FWE_Force";
                                    responseString = API_FullExport(ctx, data, settings, globalAppSettings, forced: forced);
                                    break;

                                case "Interface/Trade":
                                    Trade trade = JsonSerializer.Deserialize<Trade>(requestBody, options);
                                    responseString = API_Trade(trade, data, settings, globalAppSettings);
                                    break;

                                case "Interface/SignList":
                                    responseString = API_SignedUserHere(usersHere);
                                    break;

                                case "Interface/GenerateAvailableDex":
                                    responseString = API_GenerateAvailableDex(settings, data, globalAppSettings, ctx);
                                    break;

                                case "Interface/ExecuteTask":
                                    ScheduledTask taskSelected = JsonSerializer.Deserialize<ScheduledTask>(requestBody, options);
                                    taskSelected = globalAppSettings.ScheduledTasks.FirstOrDefault(t => t.ProcessFilePath == taskSelected.ProcessFilePath);
                                    API_ExecuteTasks(taskSelected);
                                    break;

                                case "System/FixCodeUser":
                                    System_FixCodeUser(data);
                                    break;

                                case "System/ClearEmptyAccounts":
                                    responseString = SYS_GenerateAvailableDex(settings, data, globalAppSettings);
                                    break;

                                case "System/ReloadData":
                                    try
                                    {
                                        settings.allPokemons = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options);
                                        List<Pokemon> custom = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./customPokemons.json"), options);
                                        custom.ForEach(p => { p.isCustom = true; });
                                        settings.allPokemons.AddRange(custom);
                                        settings.pokemons = settings.allPokemons.Where(p => p.enabled || p == null).ToList();
                                        settings.pokeballs = JsonSerializer.Deserialize<List<Pokeball>>(File.ReadAllText("./balls.json"), options);
                                        settings.triggers = JsonSerializer.Deserialize<List<Trigger>>(File.ReadAllText("./Triggers.json"), options);
                                        globalAppSettings = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("./_settings.json"), options);

                                        Logger($"yellow#{globalAppSettings.Texts.serverReloaded}");
                                        Logger($"white#Nombre de pokémon chargé : |red#{settings.pokemons.Count}");
                                        Logger($"white#Nombre de pokeball chargé : |red#{settings.pokeballs.Count}");
                                        Logger($"white#Nombre de triggers chargé : |red#{settings.triggers.Count}");
                                        Logger($"white#Tous les settings ont été rechargés |red#sauf le port du serveur, cet élement nécessite un redémarre si vous le changez !\n");

                                        responseString = "system data reloaded";
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                        responseString = "ERROR" + ex.ToString();
                                    }
                                    break;

                                case "System/ClearPeopleHere":
                                    try
                                    {
                                        usersHere = usersHere.Where(x => x.Platform == "system").ToList();
                                        System.IO.File.WriteAllText("./user.data", "[]");
                                        responseString = "success";
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.ToString());
                                        responseString = "ERROR" + ex.ToString();
                                    }
                                    break;

                                default:
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    responseString = $"Route non reconnue. \nDEBUG : {requestBody}";
                                    break;
                            }

                            if (globalAppSettings.MustAutoFullExport && lastExportTime.AddMinutes(globalAppSettings.DelayBeforeFullWebUpdate) < DateTime.Now)
                            {
                                try {
                                    ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                    string a = API_FullExport(ctx, data, settings, globalAppSettings);
                                    lastExportTime = DateTime.Now;
                                    if (globalAppSettings.Log.logConsole.console)
                                        Console.WriteLine("Export done.");
                                } catch {
                                    if(globalAppSettings.Log.logConsole.console)
                                        Console.WriteLine("Export not possible.");
                                }
                            }

                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(buffer, 0, buffer.Length);
                            }
                            checkScheduledTasks(globalAppSettings);

                            generateOverlays(overlays, First: false);
                        }
                    }
                    if (request.HttpMethod == "GET")
                    {
                        using (StreamReader reader = new StreamReader(request.InputStream))
                        {
                            string requestBody = reader.ReadToEnd();

                            // Récupération des variables de requête depuis l'URL
                            var queryParameters = request.QueryString;

                            string urlPath = request.Url.LocalPath.Trim('/');
                            string responseString = "";
                            switch(urlPath)
                            {
                                case "Get":
                                    switch (queryParameters.AllKeys[0])
                                    {
                                        case "Value":
                                            SearchValue searchValue = new SearchValue();

                                            string info = queryParameters["Value"];

                                            searchValue.SetEnv(data, settings, globalAppSettings, usersHere);

                                            responseString = searchValue.searchValue(info);
                                            break;

                                        default:
                                            responseString = $"Route non reconnue. \nDEBUG : {requestBody}";
                                            break;

                                    }
                                    break;

                                case "Interface/GetUserHere":
                                    responseString = API_GetUserHere(usersHere);
                                    break;

                                default:
                                    responseString = $"Route non reconnue. \nDEBUG : {requestBody}";
                                    break;
                            }

                            

                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(buffer, 0, buffer.Length);
                            }
                            checkScheduledTasks(globalAppSettings);
                        }
                    }

                }
            });

            Console.ReadLine();
            listener.Stop();
        }

        private static string API_GetUserHere(List<User> usersHere)
        {
            return JsonSerializer.Serialize(usersHere.Where(x => x.Platform != "system").ToList());
        }

        private static void generateOverlays(OverlayGeneration overlay, bool First)
        {
            if (First)
            {
                overlay.FirstRun();
            }
            else
            {
                overlay.TextsUpdate();
            }
        }

        private static void System_FixCodeUser(DataConnexion data)
        {
            List<User> users = data.GetAllUserPlatforms();
            users.ForEach(x =>
            {
                data.GetEntriesByPseudo(pseudoTriggered: x.Pseudo, platformTriggered: x.Platform).ForEach(a => { a.code = x.Code_user; a.Validate(false); });
            });
        }

        private static void API_ExecuteTasks(ScheduledTask task)
        {
            if (File.Exists(task.ProcessFilePath))
            {
                Process process = new Process();
                process.StartInfo.FileName = task.ProcessFilePath;
                process.StartInfo.WorkingDirectory = Path.GetDirectoryName(task.ProcessFilePath);
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();

                Console.WriteLine($"\ntask {Path.GetFileName(task.ProcessFilePath)} success.");
            }
            else
            {
                Console.WriteLine("\n" + task.ProcessFilePath + " not found.");
            }
        }

        private static void checkScheduledTasks(GlobalAppSettings settings, bool start = false)
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

                        Logger($"white#Task |red#{Path.GetFileName(task.ProcessFilePath)}|white# success.");
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
            int counter = 0;
            var users = data.GetAllUserPlatforms();
            foreach (var user in users.Where(x => x.Platform == "twitch" || x.Platform == "youtube" || x.Platform == "tiktok").ToList())
            {
                var entries = data.GetEntriesByPseudo(user.Pseudo, user.Platform);
                user.generateStats();
                if (entries == null || user.Stats.ballLaunched == 0)
                {
                    counter++;
                    if (_params.Log.logConsole.console)
                    {
                        string lastAppear = "No last appear recorded.";
                        if (entries is not null && entries.Count > 0)
                        {
                            Entrie value = entries.OrderByDescending(w => w.dateLastCatch).FirstOrDefault();
                            DateTime a = value!.dateLastCatch;
                            TimeSpan diff = DateTime.Now - a;
                            lastAppear = $" Last seen : {a.Day} ago.";
                        }
                        Console.WriteLine($"{user.Pseudo} on {user.Platform} [{user.Code_user}] Deleted ({entries.Count} entries, {user.Stats.ballLaunched} ball launched.)");
                    }

                    user.DeleteUser();
                    user.DeleteAllEntries();
                }
            }
            return $"{counter} users deleted.";
        }

        public static UserRequest tempFixUserRequest(UserRequest ur, DataConnexion db)
        {
            UserRequest requete = ur;
            if (ur.UserCode == "unset")
            {
                if (db.GetAllUserPlatforms().Where(u => u.Pseudo == requete.UserName && u.Platform == requete.Platform).Any())
                {
                    ur.UserCode = db.GetCodeUserByPlatformPseudo(new User(requete.UserName, requete.Platform));
                }
            }
            return ur;
        }

        private static void AddToHere(User user, ref List<User> usersHere, GlobalAppSettings globalAppSettings)
        {
            if (!usersHere.Where(x => x.Pseudo == user.Pseudo && x.Platform == user.Platform).ToList().Any())
            {
                usersHere.Add(user);
                if (globalAppSettings.Log.logConsole.console)
                {
                    Logger($"red#{user.Pseudo}|white# (on |red#{user.Platform}|white#) a ajouté à la liste du giveaway.");
                    Console.WriteLine("\r");
                }
            }
        }

        private static void dumpSprite(List<Pokemon> allPokemons)
        {
            WebClient clientweb = new WebClient();
            foreach (Pokemon pokemon in allPokemons)
            {
                try
                {
                    string imageUrl = pokemon.Sprite_Normal.ToLower();  // L'URL de l'image
                    string fileName = pokemon.Name_EN + "_normal.gif";  // Le nom de fichier cible
                    string filepath = Path.Combine("c:\\", "sprite", fileName);

                    clientweb.DownloadFile(new Uri(imageUrl), filepath);

                    imageUrl = pokemon.Sprite_Shiny.ToLower();  // L'URL de l'image
                    fileName = pokemon.Name_EN + "_shiny.gif";  // Le nom de fichier cible
                    filepath = Path.Combine("c:\\", "sprite", fileName);

                    clientweb.DownloadFile(new Uri(imageUrl), filepath);

                    //Console.WriteLine(pokemon.Name_FR + " : téléchargé en shiny & en normal.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(pokemon.Name_FR + " : Une erreur lors du téléchargement" + ex.Message);
                }
            }
        }

        private static string API_GenerateAvailableDex(AppSettings settings, DataConnexion data, GlobalAppSettings globalAppSettings, UserRequest ur)
        {
            string r = string.Empty;
            try
            {
                ExportDexAvailablePokemon a = new ExportDexAvailablePokemon(settings, ur, data, globalAppSettings);
                a.GenerateFile();
                return a.filename;
            }
            catch (Exception ex)
            {
                r = ex.Message;
            }
            return r;
        }

        private static string API_SignedUserHere(List<User> usersHere)
        {
            string r = string.Empty;
            r = $"{usersHere.Where(w => w.Platform != "system").ToList().Count} personnes.\n\n";
            usersHere.OrderBy(o => o.Platform).ToList().ForEach(user => r += $"[{user.Platform}] {user.Pseudo}\n");
            return r;
        }

        private static string SignIn(User user, DataConnexion data, ref List<User> usersHere)
        {
            var a = data.GetAllUserPlatforms();
            bool estCeQueLeMecAParticipe2Fois = usersHere.Where(b => b.Pseudo == user.Pseudo && b.Platform == user.Platform).Any();
            if (estCeQueLeMecAParticipe2Fois)
            {
                return $"@{user.Pseudo} tu fais dejà partie de la liste des participants :)";
            }
            else
            {
                data.SetCodeUserByPlatformPseudo(item: user);

                var entries = data.GetEntriesByPseudo(pseudoTriggered: user.Pseudo, platformTriggered: user.Platform);
                entries.ForEach(entry =>
                {
                    entry.code = user.Code_user;
                    entry.Validate(false);
                });

                data.SetCodeUserByPlatformPseudo(item: user);

                usersHere.Add(user);
                // Écrit le contenu dans le fichier
                string ab = JsonSerializer.Serialize(usersHere.Where(x => x.Platform != "system").ToList(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                System.IO.File.WriteAllText("./user.data", ab);
                return $"@{user.Pseudo} tu as bien été ajouté a la liste des participants !";
            }
        }

        private static string GetUserStats(UserRequest ctx, DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            tempFixUserCodeInBDD(ctx, data);

            try
            {
                User user = new User(ctx.UserName, ctx.Platform, ctx.UserCode, data);
                int nombreDePoke = settings.pokemons.Count;
                string sentence = $"@{user.Pseudo} => tu as eu {user.Stats.pokeCaught} poké dont {user.Stats.shinyCaught} shiny en tout, tes dex sont {globalAppSettings.Texts.emotes.dex}[{user.Stats.dexCount}/{nombreDePoke} ({(user.Stats.dexCount * 100) / settings.pokemons.Count}%)]{globalAppSettings.Texts.emotes.dex} - {globalAppSettings.Texts.emotes.shiny}[{user.Stats.shinydex}/{nombreDePoke} ({(user.Stats.shinydex * 100) / settings.pokemons.Count}%)]{globalAppSettings.Texts.emotes.shiny} ! {user.Stats.moneySpent}{globalAppSettings.Texts.emotes.money} dépensés et {user.Stats.ballLaunched} {globalAppSettings.Texts.emotes.ball} lancées. Money : {user.Stats.CustomMoney}.";
                if (globalAppSettings.Log.logConsole.console)
                    Console.WriteLine($"---\nresult : {sentence}\n---\n");
                return sentence;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return globalAppSettings.Texts.error;
            }
        }

        private static string GetUserLevels(UserRequest ctx, DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            tempFixUserCodeInBDD(ctx, data);

            try
            {
                User user = new User(ctx.UserName, ctx.Platform, ctx.UserCode, data);
                int nombreDeBadge = settings.badges.Count;
                user.generateStatsAchievement(settings, globalAppSettings);
                string sentence = $"@{user.Pseudo} => Level {user.Stats.level} ({user.Stats.currentXP}/{globalAppSettings.BadgeSettings.XPPerLevel}) {user.Stats.badges.Where(x => x.Obtained).Count()}/{settings.badges.Count} badges. Génère ton Pokédex pour plus d'infos.";
                if (globalAppSettings.Log.logConsole.console)
                    Console.WriteLine($"---\nresult : {sentence}\n---\n");
                return sentence;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return globalAppSettings.Texts.error;
            }
        }

        /// <summary>
        /// TEMP METHOD
        /// </summary>
        /// <param name="ctx"></param>
        /// <exception cref="NotImplementedException"></exception>
        private static void tempFixUserCodeInBDD(UserRequest ctx, DataConnexion data)
        {
            data.SetCodeUserByPlatformPseudo(new User { Data = data, Code_user = ctx.UserCode, Platform = ctx.Platform, Pseudo = ctx.UserName });
            var entries = data.GetEntriesByPseudo(ctx.UserCode, ctx.Platform);
            entries.ForEach(entry => { entry.code = ctx.UserCode; entry.Validate(false); });
        }

        private static string CatchPoke(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                string result = new Work(json, cnx, appSettings, globalAppSettings).DoCatchRandomPoke();
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

        private static string GenerateDexSolo(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            tempFixUserCodeInBDD(json, cnx);
            try
            {
                var data = new ExportSoloDex(appSettings, json, cnx, globalAppSettings);
                data.ExportFile().Wait();
                data.UploadFileAsync().Wait();
                if (globalAppSettings.Log.logConsole.console)
                    Console.WriteLine($"---\nresult : {data.url}\n---\n");
                return data.url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return globalAppSettings.Texts.error;
            }
        }

        private static string GenerateDexFull(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                var data = new ExportRapport(appSettings, json, cnx, globalAppSettings);
                data.ExportFile().Wait();
                string result = $"Génération exécutée avec succès sous le nom de {data.filename}!";

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

        private static string API_SendBall(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                string result = string.Empty;
                if (appSettings.pokeballs.Where(p => p.Name == json.TriggerName).Any())
                {
                    result = new Work(json, cnx, appSettings, globalAppSettings).DoCatchRandomPoke(true, appSettings.pokeballs.Where(p => p.Name == json.TriggerName).FirstOrDefault());
                }
                else
                {
                    result = "No pokeball with that name exist. ";
                }
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

        private static string API_GiveAway(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings, List<User> userList)
        {
            try
            {
                string result = new Work(json, cnx, appSettings, globalAppSettings).DistributePoke(userList);
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

        private static string API_FullExport(UserRequest json, DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings, bool forced = false)
        {
            try
            {
                // export main file  + individuals
                string result = new Work(json, cnx, appSettings, globalAppSettings).DoFullExport(forced: forced);

                //  export availablespokemon.html
                ExportDexAvailablePokemon a = new ExportDexAvailablePokemon(appSettings, json, cnx, globalAppSettings);
                a.GenerateFile();

                //  export pokestats.html
                ExportStats exportStats = new ExportStats(appSettings, json, cnx, globalAppSettings);
                exportStats.ExportFile().Wait();
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

        private static string API_Trade(Trade trade, DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                string result = trade.DoWork();
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

        private static void Logger(string message)
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
    }
}