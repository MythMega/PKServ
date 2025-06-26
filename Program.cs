using PKServ.Admin;
using PKServ.Business;
using PKServ.Business.Admin;
using PKServ.Business.Raid;
using PKServ.Configuration;
using PKServ.Entity;
using PKServ.Entity.Raid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Text.Json;
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

            var options = new JsonSerializerOptions
            {
                IncludeFields = true,
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            if (!File.Exists("./_settings.json"))
            {
                File.WriteAllText("./_settings.json", File.ReadAllText("Admin\\DefaultSettings.json"));
            }
            GlobalAppSettings globalAppSettings = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("./_settings.json"), options);
            GlobalAppSettings globalAppSettingsDefaults = JsonSerializer.Deserialize<GlobalAppSettings>(File.ReadAllText("Admin\\DefaultSettings.json"), options);
            SettingsHelper.MergeWithDefaults(globalAppSettings, globalAppSettingsDefaults);
            globalAppSettings.FixType(globalAppSettingsDefaults);
            File.WriteAllText("./_settings.json", JsonSerializer.Serialize(globalAppSettings, options));

            // définition des données
            AppSettings settings = new();
            List<User> usersHere = [new User("_history", "system")];

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

            DateTime lastExportTime = DateTime.Now;
            checkScheduledTasks(globalAppSettings, true);

            await DataFixerImpl.FixEntries(globalAppSettings, data);
            await DataFixerImpl.FixUsers(data);

            #endregion Initiatlization

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{globalAppSettings.ServerPort}/");
            listener.Start();

            await Task.Run(async () =>
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
                            bool actionToExport = false;

                            if (globalAppSettings.Log.logConsole.console)
                            {
                                Console.WriteLine($"{DateTime.Now.ToString("yyyy MM dd - HH:mm:ss")} - entrée : {urlPath}");
                                if (globalAppSettings.Log.logConsole.logJsonOnConsole)
                                {
                                    Console.WriteLine(requestBody);
                                }
                            }
                            try
                            {
                                switch (urlPath)
                                {
                                    case "CatchPoke":
                                        ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                        }
                                        responseString = CatchPoke(ctx, data, settings, globalAppSettings);
                                        try
                                        {
                                            if (ctx.avatarUrl != null)
                                            {
                                                data.UpdateAvatar(ctx);
                                            }
                                        }
                                        catch { }
                                        break;

                                    case "CatchPokeNew":
                                        BallThrowRequest ballThrowRequest = JsonSerializer.Deserialize<BallThrowRequest>(requestBody, options);

                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(ballThrowRequest.UserName, ballThrowRequest.Platform, ballThrowRequest.UserCode, data), ref usersHere, globalAppSettings);
                                        }

                                        BallThrowTreatement traitement = new BallThrowTreatement();
                                        await traitement.InitializeAsync(ballThrowRequest, settings, data, globalAppSettings);
                                        responseString = await traitement.ProcessAsync(settings, globalAppSettings, data);
                                        actionToExport = true;

                                        break;

                                    case "Evolve":
                                        CreatureEvolutionRequest creatureEvolutionRequest = JsonSerializer.Deserialize<CreatureEvolutionRequest>(requestBody, options);
                                        try
                                        {
                                            creatureEvolutionRequest.SetCreatures(settings.pokemons, globalAppSettings);
                                            responseString = creatureEvolutionRequest.DoEvolve(data, globalAppSettings);
                                            if (!settings.UsersToExport.Where(u => u.Code_user == creatureEvolutionRequest.User.Code_user || (u.Pseudo == creatureEvolutionRequest.User.Pseudo && u.Platform == creatureEvolutionRequest.User.Platform)).Any())
                                                settings.UsersToExport.Add(creatureEvolutionRequest.User);
                                        }
                                        catch (Exception e)
                                        {
                                            responseString = e.Message;
                                        }
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

                                    case "ChoseFavoriteCreature":
                                        FavoriteCreatureRequest favoriteCreatureRequest = JsonSerializer.Deserialize<FavoriteCreatureRequest>(requestBody, options);
                                        if (favoriteCreatureRequest.IsValide(data, settings))
                                        {
                                            responseString = favoriteCreatureRequest.Set(favoriteCreatureRequest.User, favoriteCreatureRequest.Name, favoriteCreatureRequest.Mode, data);

                                            if (!settings.UsersToExport.Where(u => u.Code_user == favoriteCreatureRequest.User.Code_user || (u.Pseudo == favoriteCreatureRequest.User.Pseudo && u.Platform == favoriteCreatureRequest.User.Platform)).Any())
                                                settings.UsersToExport.Add(favoriteCreatureRequest.User);
                                        }
                                        else
                                        {
                                            responseString = "invalide";
                                        }
                                        break;

                                    case "GenerateDexFull":
                                        responseString = GenerateDexFull(data, settings, globalAppSettings);
                                        break;

                                    case "GetUserStats":
                                        ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, data), ref usersHere, globalAppSettings);
                                        }
                                        responseString = GetUserStats(ctx, data, settings, globalAppSettings);
                                        break;

                                    case "GetPokeStats":
                                        GetPokeStats requestGetPokeStats = JsonSerializer.Deserialize<GetPokeStats>(requestBody, options);
                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(requestGetPokeStats.User.Pseudo, requestGetPokeStats.User.Platform, requestGetPokeStats.User.Code_user, data), ref usersHere, globalAppSettings);
                                        }
                                        responseString = GetOneCreatureStat(requestGetPokeStats, data, settings, globalAppSettings);
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
                                        try
                                        {
                                            Scrapping scrapping = JsonSerializer.Deserialize<Scrapping>(requestBody, options);
                                            scrapping.SetEnv(data, settings, globalAppSettings);
                                            responseString = scrapping.DoResult(settings);
                                        }
                                        catch
                                        {
                                            Console.WriteLine("erreur scrap");
                                        }
                                        break;

                                    case "BuyElement":
                                        Buying buying = JsonSerializer.Deserialize<Buying>(requestBody, options);
                                        buying.SetEnv(data, settings, globalAppSettings);
                                        responseString = buying.DoResult();
                                        break;

                                    case "ChangeBackground":
                                        BackgroundChange tcbackgroundChange = JsonSerializer.Deserialize<BackgroundChange>(requestBody, options);
                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(tcbackgroundChange.User.Pseudo, tcbackgroundChange.User.Platform, tcbackgroundChange.User.Code_user, data), ref usersHere, globalAppSettings);
                                        }
                                        if (tcbackgroundChange.IsValide(data, settings))
                                        {
                                            responseString = tcbackgroundChange.DoResult(settings);

                                            if (!settings.UsersToExport.Where(u => u.Code_user == tcbackgroundChange.User.Code_user || (u.Pseudo == tcbackgroundChange.User.Pseudo && u.Platform == tcbackgroundChange.User.Platform)).Any())
                                                settings.UsersToExport.Add(tcbackgroundChange.User);
                                        }
                                        else
                                            responseString = "T'as pas le droit frere";
                                        break;

                                    case "Zone/Move/Normal":
                                        ZoneChange rqZoneChange = JsonSerializer.Deserialize<ZoneChange>(requestBody, options);
                                        if (globalAppSettings.AutoSignInGiveAway)
                                        {
                                            AddToHere(new User(rqZoneChange.User.Pseudo, rqZoneChange.User.Platform, rqZoneChange.User.Code_user, data), ref usersHere, globalAppSettings);
                                        }
                                        if (rqZoneChange.IsValide(settings))
                                        {
                                            List<string> errors = rqZoneChange.ListErrors(settings, globalAppSettings);
                                            if (errors.Count == 0)
                                            {
                                                responseString = await rqZoneChange.DoResult(settings, data);

                                                if (!settings.UsersToExport.Where(u => u.Code_user == rqZoneChange.User.Code_user || (u.Pseudo == rqZoneChange.User.Pseudo && u.Platform == rqZoneChange.User.Platform)).Any())
                                                    settings.UsersToExport.Add(rqZoneChange.User);
                                            }
                                            else
                                            {
                                                responseString = "Failed : " + string.Join(" ; ", errors);
                                            }
                                        }
                                        else
                                            responseString = "Erreur, soit ça existe po, soit y a un bug";
                                        break;

                                    case "Zone/GetCurrentZone":
                                        User userGetCurrentZone = JsonSerializer.Deserialize<User>(requestBody, options);
                                        responseString = data.GetZoneUser(userGetCurrentZone.Code_user, settings.Zones)?.Name;
                                        break;

                                    case "Zone/Move/Random":
                                        ZoneChangeAuto rqZoneChangeRandom = JsonSerializer.Deserialize<ZoneChangeAuto>(requestBody, options);
                                        rqZoneChangeRandom.SmartMode = false;
                                        rqZoneChangeRandom.SetPokesZones(settings.pokemons, settings.Zones);
                                        responseString = await rqZoneChangeRandom.DoResult(settings, data, globalAppSettings);
                                        break;

                                    case "Zone/Move/Smart":
                                        ZoneChangeAuto rqZoneChangeSmart = JsonSerializer.Deserialize<ZoneChangeAuto>(requestBody, options);
                                        rqZoneChangeSmart.SmartMode = true;
                                        responseString = await rqZoneChangeSmart.DoResult(settings, data, globalAppSettings);
                                        break;

                                    case "Giveaway/Claim":
                                        GiveawayClaim giveawayClaim = JsonSerializer.Deserialize<GiveawayClaim>(requestBody, options);
                                        if (settings.giveaways.Any(a => a.Code == giveawayClaim.Code))
                                        {
                                            Giveaway giveaway = settings.giveaways.First(a => a.Code == giveawayClaim.Code);

                                            // on verifie les dates
                                            if (DateTime.Now < giveaway.Start)
                                            {
                                                responseString = globalAppSettings.Texts.TranslationGiveaway.CodeNotYetAvailable;
                                            }
                                            else if (DateTime.Now > giveaway.End)
                                            {
                                                responseString = globalAppSettings.Texts.TranslationGiveaway.CodeExpired;
                                            }

                                            // si les dates sont bonnes, on peut faire le giveaway
                                            else
                                            {
                                                responseString = GiveawayImpl.DoGiveaway(giveawayClaim, giveaway, globalAppSettings, settings, data);

                                                if (!settings.UsersToExport.Where(u => u.Code_user == giveawayClaim.User.Code_user || (u.Pseudo == giveawayClaim.User.Pseudo && u.Platform == giveawayClaim.User.Platform)).Any())
                                                    settings.UsersToExport.Add(giveawayClaim.User);
                                            }
                                        }
                                        else
                                            responseString = globalAppSettings.Texts.TranslationGiveaway.CodeDoesNotExist;
                                        break;

                                    case "GetOneValue":
                                        SearchValue sv = JsonSerializer.Deserialize<SearchValue>(requestBody, options);
                                        sv.SetEnv(data, settings, globalAppSettings, usersHere);
                                        responseString = sv.searchResult();
                                        break;

                                    case "Trade/Request":
                                        try
                                        {
                                            TradeRequest tradeRequest = JsonSerializer.Deserialize<TradeRequest>(requestBody, options);

                                            TradeRequest oldTrade = settings.TradeRequests.Where(trade => trade.UserWhoMadeRequest.Code_user == tradeRequest.UserWhoMadeRequest.Code_user && !trade.Completed).FirstOrDefault();
                                            if (oldTrade is not null)
                                            {
                                                responseString = $"{globalAppSettings.Texts.TranslationTrading.atLeastOneTradeInitialized} [{globalAppSettings.CommandSettings.CmdTradeCancel} {oldTrade.ID}]";
                                                break;
                                            }

                                            #region verification

                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableShinyInTrade && (tradeRequest.CreatureRequested.isShiny || tradeRequest.CreatureSent.isShiny))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeShiny;
                                                break;
                                            }
                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableLockedPokemonInTrade && (tradeRequest.CreatureRequested.isLock || tradeRequest.CreatureSent.isLock))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeLocked;
                                                break;
                                            }
                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableLegendariesInTrade && (tradeRequest.CreatureRequested.isLegendary || tradeRequest.CreatureSent.isLegendary))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeShiny;
                                                break;
                                            }
                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableShinyAgainstNormal && (tradeRequest.CreatureRequested.isShiny != tradeRequest.CreatureSent.isShiny))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeShinyAndNormal;
                                                break;
                                            }
                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableTradeBetweenClassicAndCustom && (tradeRequest.CreatureRequested.isCustom != tradeRequest.CreatureSent.isCustom))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeClassicAndCustom;
                                                break;
                                            }
                                            if (!globalAppSettings.TradeSettings.TradeConditions.EnableTradeBetweenDifferentSeries && (tradeRequest.CreatureRequested.Serie.ToLower() != tradeRequest.CreatureSent.Serie.ToLower()))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotTradeShiny;
                                                break;
                                            }

                                            #endregion verification

                                            if (globalAppSettings.TradeSettings.PaidTrade)
                                            {
                                                tradeRequest.CalculatePrice(globalAppSettings);
                                                tradeRequest.UserWhoMadeRequest.generateStats();
                                                if (tradeRequest.Price > tradeRequest.UserWhoMadeRequest.Stats.CustomMoney)
                                                {
                                                    responseString = globalAppSettings.Texts.TranslationTrading.tooExpensive
                                                    .Replace("[PRICE]", $"{tradeRequest.Price}")
                                                    .Replace("[CURRENT_MONEY]", $"{tradeRequest.UserWhoMadeRequest.Stats.CustomMoney}");
                                                    break;
                                                }
                                                if (!tradeRequest.CheckIfCanTradeThisItem())
                                                {
                                                    responseString = globalAppSettings.Texts.TranslationTrading.elementNotInPossession;
                                                    break;
                                                }
                                            }

                                            settings.TradeRequests.Add(tradeRequest);
                                            responseString = tradeRequest.GetMessageCode(globalAppSettings);
                                        }
                                        catch (Exception e)
                                        {
                                            JsonDocument jsonDocument = JsonDocument.Parse(requestBody);
                                            JsonElement root = jsonDocument.RootElement;
                                            try
                                            {
                                                string PokeSent = root.GetProperty("PokeSent").GetString();
                                                string PokeRequested = root.GetProperty("PokeWanted").GetString();

                                                if (e.Message == "Poke Wanted Not Found")
                                                {
                                                    responseString = globalAppSettings.Texts.TranslationTrading.creatureNotFound.Replace("[CREATURE]", PokeRequested);
                                                }
                                                else if (e.Message == "Poke Sent Not Found")
                                                {
                                                    responseString = globalAppSettings.Texts.TranslationTrading.creatureNotFound.Replace("[CREATURE]", PokeSent);
                                                }
                                            }
                                            catch
                                            {
                                                responseString = globalAppSettings.Texts.error;
                                                break;
                                            }
                                        }
                                        break;

                                    case "Trade/Cancel":
                                        TradeCancel tradeCancel = JsonSerializer.Deserialize<TradeCancel>(requestBody, options);
                                        if (settings.TradeRequests.Where(c => c.ID == tradeCancel.ID).Any())
                                        {
                                            TradeRequest TR = settings.TradeRequests.Where(x => x.ID == tradeCancel.ID && !x.Completed).FirstOrDefault();
                                            if (tradeCancel.User.Code_user == TR.UserWhoMadeRequest.Code_user)
                                            {
                                                TR.Complete();
                                                settings.TradeRequests.Remove(TR);
                                                responseString = globalAppSettings.Texts.TranslationTrading.cancelled;
                                            }
                                            else
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.cannotCancelNotOwner;
                                            }
                                        }
                                        else
                                        {
                                            responseString = globalAppSettings.Texts.TranslationTrading.codeInvalidOrExpired;
                                        }
                                        break;

                                    case "Trade/Accept":
                                        TradeAccept tradeAccept = JsonSerializer.Deserialize<TradeAccept>(requestBody, options);
                                        if (settings.TradeRequests.Where(c => c.ID == tradeAccept.ID && !c.Completed).Any())
                                        {
                                            TradeRequest TR = settings.TradeRequests.Where(x => x.ID == tradeAccept.ID && !x.Completed).FirstOrDefault();
                                            tradeAccept.UserWhoAccepted.generateStats();
                                            if (!tradeAccept.VerifEligibilityMoney(TR.Price))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.tooExpensive;
                                                break;
                                            }
                                            if (!tradeAccept.VerifEligibilityCreature(TR.CreatureRequested, data))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationTrading.elementNotInPossession;
                                                break;
                                            }

                                            TR.UserWhoAccepted = tradeAccept.UserWhoAccepted;
                                            var tradeAction = new Trade(TR);
                                            tradeAction.SetEnv(globalAppSettings);
                                            responseString = tradeAction.DoWork(paid: true);
                                            TR.Complete();

                                            if (!settings.UsersToExport.Where(u => u.Code_user == tradeAction.trader1.Code_user || (u.Pseudo == tradeAction.trader1.Pseudo && u.Platform == tradeAction.trader1.Platform)).Any())
                                                settings.UsersToExport.Add(tradeAction.trader1);
                                            if (!settings.UsersToExport.Where(u => u.Code_user == tradeAction.trader2.Code_user || (u.Pseudo == tradeAction.trader2.Pseudo && u.Platform == tradeAction.trader2.Platform)).Any())
                                                settings.UsersToExport.Add(tradeAction.trader2);
                                            settings.TradeRequests.Remove(TR);
                                        }
                                        else
                                        {
                                            responseString = globalAppSettings.Texts.TranslationTrading.codeInvalidOrExpired;
                                        }
                                        break;

                                    case "Raid/GiveawayPoke":
                                        if (settings.ActiveRaid is null)
                                        {
                                            responseString = globalAppSettings.Texts.TranslationRaid.NoActiveRaid;
                                            break;
                                        }
                                        GiveawayPokeFromRaidRequest giveawayPokeFromRaidRequest = JsonSerializer.Deserialize<GiveawayPokeFromRaidRequest>(requestBody, options);
                                        responseString = settings.ActiveRaid.GivePoke(giveawayPokeFromRaidRequest, appSettings: settings, globalAppSettings: globalAppSettings);
                                        break;

                                    case "Raid/Attack":
                                        User raidAttacker = JsonSerializer.Deserialize<User>(requestBody, options);
                                        await GlobalDataAction.UserClean(raidAttacker, settings, data);
                                        if (settings.ActiveRaid is null)
                                        {
                                            responseString = globalAppSettings.Texts.TranslationRaid.NoActiveRaid;
                                            break;
                                        }
                                        if (settings.ActiveRaid.DefeatedTime is not null)
                                        {
                                            if (settings.ActiveRaid.DefeatedTime < DateTime.Now.AddMinutes(globalAppSettings.RaidSettings.TimeMinuteToCatchAfterDefeat))
                                            {
                                                responseString = globalAppSettings.Texts.TranslationRaid.RaidAlreadyGone
                                                    .Replace("[TIME-TO-CATCH]", $"{globalAppSettings.RaidSettings.TimeMinuteToCatchAfterDefeat}");
                                                break;
                                            }
                                            responseString = globalAppSettings.Texts.TranslationRaid.BossDeafeatedUseCmdToCatchIt
                                                .Replace("[CMD-CATCH]", globalAppSettings.CommandSettings.CmdRaidCatch);
                                            break;
                                        }

                                        responseString = settings.ActiveRaid.Attack(raidAttacker, globalAppSettings, settings);
                                        break;

                                    case "Raid/CatchResult":
                                        User raidCatcher = JsonSerializer.Deserialize<User>(requestBody, options);
                                        if (settings.ActiveRaid is null)
                                        {
                                            responseString = globalAppSettings.Texts.TranslationRaid.NoActiveRaid;
                                            break;
                                        }
                                        responseString = settings.ActiveRaid.Catch(globalAppSettings, raidCatcher);
                                        break;

                                    case "Raid/Heal/Self":
                                        User raidSelfHealer = JsonSerializer.Deserialize<User>(requestBody, options);

                                        responseString = settings.ActiveRaid.Heal(raidSelfHealer, self: true);
                                        break;

                                    case "Raid/Heal/Full":
                                        User raidFullHealer = JsonSerializer.Deserialize<User>(requestBody, options);

                                        responseString = settings.ActiveRaid.Heal(raidFullHealer, self: false);
                                        break;

                                    case "Raid/ForceStatut":
                                        RaidStatutApplication raidStatutApplication = JsonSerializer.Deserialize<RaidStatutApplication>(requestBody, options);
                                        responseString = RaidStatutDistributionImpl.ProcessApplication(settings.ActiveRaid, raidStatutApplication);
                                        break;

                                    case "Interface/GetAll/Creatures":
                                        responseString = JsonSerializer.Serialize<List<Pokemon>>(settings.pokemons, options);
                                        break;

                                    case "Interface/GetAll/Balls":
                                        responseString = JsonSerializer.Serialize<List<Pokeball>>(settings.pokeballs, options);
                                        break;

                                    case "Interface/GetAll/Users":
                                        responseString = JsonSerializer.Serialize<List<User>>(data.GetAllUserPlatforms());
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
                                        DateTime date_a = DateTime.Now;
                                        ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                        bool forced = ctx.TriggerName == "API_FWE_Force";
                                        responseString = API_FullExport(data, settings, globalAppSettings, forced: forced);
                                        DateTime date_b = DateTime.Now;
                                        Console.WriteLine((date_b - date_a).TotalSeconds);
                                        break;

                                    case "Interface/Trade":
                                        Trade trade = JsonSerializer.Deserialize<Trade>(requestBody, options);
                                        trade.SetEnv(globalAppSettings: globalAppSettings);
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

                                    case "Interface/Raid/Start":
                                        Raid raid = JsonSerializer.Deserialize<Raid>(requestBody, options);
                                        raid.InitializeBoss(settings.pokemons);
                                        raid.SetDefaultValues(globalAppSettings, data);
                                        settings.ActiveRaid = raid;
                                        responseString = $"Raid {settings.ActiveRaid.Boss.Name_FR} {settings.ActiveRaid.PVMax}PV";
                                        break;

                                    case "Interface/Raid/Cancel":
                                        if (settings.ActiveRaid is not null)
                                        {
                                            responseString = "Raid Stopped";
                                            settings.ActiveRaid = null;
                                        }
                                        else
                                        {
                                            responseString = "No Active Raid";
                                        }
                                        break;

                                    case "Interface/Raid/Boost/Set":
                                        RaidDamageBoost raidDamageBoost = JsonSerializer.Deserialize<RaidDamageBoost>(requestBody, options);
                                        raidDamageBoost.Initialize();
                                        if (settings.ActiveRaid is not null)
                                        {
                                            settings.ActiveRaid.ActiveBoost = raidDamageBoost;
                                        }
                                        else
                                        {
                                            responseString = "No Active Raid";
                                        }
                                        break;

                                    case "Interface/Raid/Boost/Cancel":
                                        break;

                                    case "Debug/GetAllData":
                                        Debug debug = JsonSerializer.Deserialize<Debug>(requestBody, options);
                                        debug.SetEnv(usersHere, settings, requestBody, globalAppSettings);
                                        responseString = await debug.DoDebug();
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
                                            LoadAllData(ref settings, ref globalAppSettings, data, usersHere);
                                            LogInitialsDatas(settings, globalAppSettings, usersHere);

                                            Commun.Logger($"white#Tous les settings ont été rechargés |red#sauf le port du serveur, cet élement nécessite un redémarre si vous le changez !\n");

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

                                    case "System/TransfertAccount":
                                        AccountTransfert transfert = JsonSerializer.Deserialize<AccountTransfert>(requestBody, options);
                                        transfert.SetEnv(data);
                                        responseString = transfert.DoTransfert();
                                        break;

                                    default:
                                        ctx = JsonSerializer.Deserialize<UserRequest>(requestBody, options);
                                        responseString = $"Route non reconnue. \nDEBUG : {requestBody}";
                                        break;
                                }

                                responseString = responseString
                                    .Replace("<", "\uFF1C")
                                    .Replace(">", "\uFF1E");
                                if (globalAppSettings.Log.logConsole.console)
                                    Console.WriteLine($"Réponse envoyée à twitchat : {responseString}");

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
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("CRITICAL POST ERROR : " + e.Message + "\n" + e.Data + "\n" + e.Source + "\n" + e.InnerException + "\n");
                            }

                            #region post-request POST

                            // Auto Export
                            if (actionToExport && globalAppSettings.MustAutoFullExport && urlPath != "Interface/FullExport" && lastExportTime.AddMinutes(globalAppSettings.DelayBeforeFullWebUpdate) < DateTime.Now)
                            {
                                try
                                {
                                    string a = API_FullExport(data, settings, globalAppSettings, forced: false, assets: false);
                                    lastExportTime = DateTime.Now;
                                    if (globalAppSettings.Log.logConsole.console)
                                        Console.WriteLine("Export done.");
                                }
                                catch (Exception e)
                                {
                                    {
                                        if (globalAppSettings.Log.logConsole.console)
                                        {
                                            Console.WriteLine("Export not possible.");
                                            Console.WriteLine(e.Message);
                                            Console.WriteLine(e.StackTrace);
                                            Console.WriteLine(e.Source);
                                        }
                                    }
                                }

                                try
                                {
                                    List<CustomOverlay> a = []; //JsonSerializer.Deserialize<List<CustomOverlay>>(File.ReadAllText("./customOverlays.json"), options);
                                    foreach (CustomOverlay overlayReset in a)
                                    {
                                        settings.customOverlays.Where(overlay => overlay.Filename == overlayReset.Filename).FirstOrDefault().Content = overlayReset.Content;
                                    }
                                    settings.customOverlays.ForEach(overlay => { overlay.BuildOverlay(false); });
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Error while Building custom Overlays : " + e.Message);
                                }

                                try
                                {
                                    generateOverlays(new OverlayGeneration(data, settings, globalAppSettings, usersHere), First: false);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Error while genereting PKServs Overlay :  " + e.Message + "\n" + e.Data);
                                }

                                #endregion post-request POST
                            }
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
                            try
                            {
                                switch (urlPath)
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

                                    case "GetRaidInfos":
                                        if (settings.ActiveRaid is not null)
                                        {
                                            var ResponseRaidInfos = new
                                            {
                                                Url_Creature = settings.ActiveRaid.DisplayShiny ? settings.ActiveRaid.Boss.Sprite_Shiny : settings.ActiveRaid.Boss.Sprite_Normal,
                                                Url_Overlay = settings.ActiveRaid.PV > 0 ? "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1280px-HD_transparent_picture.png" : "https://png.pngtree.com/png-vector/20230527/ourmid/pngtree-red-cross-paint-clipart-transparent-background-vector-png-image_7110618.png",
                                                Bar_Max = settings.ActiveRaid.PVMax,
                                                Bar_CurrentValue = settings.ActiveRaid.PV
                                            };
                                            responseString = JsonSerializer.Serialize(ResponseRaidInfos);
                                        }
                                        else
                                            responseString = "{}";
                                        break;

                                    case "GetRaidStatus":
                                        if (settings.ActiveRaid is not null && settings.ActiveRaid.DefeatedTime is not null && settings.ActiveRaid.DefeatedTime.Value.AddMinutes(globalAppSettings.RaidSettings.TimeMinuteToCatchAfterDefeat) < DateTime.Now)
                                        {
                                            settings.ActiveRaid = null;
                                        }

                                        if (settings.ActiveRaid is null)
                                        {
                                            responseString = globalAppSettings.Texts.TranslationRaid.NoActiveRaid;
                                            break;
                                        }
                                        responseString = settings.ActiveRaid.GetRaidStatuts();
                                        break;

                                    case "Debug/CatchHistory":

                                        switch (queryParameters.AllKeys[0])
                                        {
                                            case "Count":

                                                int CountCatchHistory = int.Parse(queryParameters["Count"]);
                                                string r_console = string.Empty;

                                                foreach (CatchHistory ch in settings.catchHistory.OrderByDescending(o => o.time).Take(CountCatchHistory))
                                                {
                                                    string shinyStatut = ch.shiny ? "shiny" : "normal";
                                                    r_console += $"{ch.time} {ch.User.ToString()} - {ch.Pokemon.Name_FR} ({shinyStatut}) - {ch.Ball.Name}\n";
                                                }
                                                Console.WriteLine(r_console);
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
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("CRITICAL GET ERROR :  " + e.Message + "\n" + e.Data);
                            }

                            byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                            response.ContentLength64 = buffer.Length;
                            using (Stream output = response.OutputStream)
                            {
                                output.Write(buffer, 0, buffer.Length);
                            }

                            try
                            {
                                checkScheduledTasks(globalAppSettings);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Error while executing scheduled task : " + e.Message + "\n" + e.Data);
                            }
                        }
                    }
                }
            });

            Console.ReadLine();
            listener.Stop();
        }

        private static void LoadAllData(ref AppSettings settings, ref GlobalAppSettings globalAppSettings, DataConnexion data, List<User> usersHere)
        {
            JsonSerializerOptions options = Commun.GetJsonSerializerOptions();
            // Initialisations des listes
            settings.allPokemons = new List<Pokemon>();
            settings.pokeballs = new List<Pokeball>();
            settings.triggers = new List<Trigger>();
            settings.badges = new List<Badge>();
            settings.TrainerCardsBackgrounds = new List<Background>();
            settings.customOverlays = new List<CustomOverlay>();
            settings.Zones = new List<Zone>();
            settings.SeriesData = new List<(string Serie, int Count)>();
            settings.pokemons = new List<Pokemon>();
            settings.giveaways = new List<Giveaway>();
            settings.Zones.Add(Commun.GetBaseZone());

            // Chargement des données de base
            settings.allPokemons.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./Data/StreamDex/Creatures.json"), options));
            settings.pokeballs.AddRange(JsonSerializer.Deserialize<List<Pokeball>>(File.ReadAllText("./Data/StreamDex/Balls.json"), options));
            settings.triggers.AddRange(JsonSerializer.Deserialize<List<Trigger>>(File.ReadAllText("./Data/StreamDex/Triggers.json"), options));
            settings.TrainerCardsBackgrounds.AddRange(JsonSerializer.Deserialize<List<Background>>(File.ReadAllText("./Data/StreamDex/TrainerCardBackgrounds.json"), options));
            settings.badges.AddRange(JsonSerializer.Deserialize<List<Badge>>(File.ReadAllText("./Data/StreamDex/badges.json"), options).Where(x => !x.Locked).ToList());
            settings.customOverlays.AddRange(JsonSerializer.Deserialize<List<CustomOverlay>>(File.ReadAllText("./Data/StreamDex/Overlays.json"), options));

            // Chargement des données globales
            LoadCustomBadges(ref settings);
            LoadCustomBalls(ref settings);
            LoadCustomCreature(ref settings);
            LoadCustomOverlay(ref settings);
            LoadCustomTrainerCardsBackground(ref settings);
            LoadCustomZone(ref settings);

            string temp = "";

            settings.Zones.ForEach(settings =>
            {
                temp += "      \"" + settings.Name + "\",\n";
            });

            var settingsLocal = settings;
            var globalAppSettingsLocal = globalAppSettings;

            settings.customOverlays.ForEach(overlay => { overlay.SetEnv(data, settingsLocal, globalAppSettingsLocal, usersHere); overlay.BuildOverlay(true); });
            OverlayGeneration overlays = new OverlayGeneration(data, settings, globalAppSettings, usersHere);

            settings.pokemons = settings.allPokemons.Where(p => p.enabled).ToList();
            settings.allPokemons.ForEach(p => { p.SetData(settingsLocal.Zones); });
            settings.giveaways = GiveawayInitializer.GetGiveaways(settings);

            List<string> Series = settings.pokemons.Select(a => a.Serie).Distinct().ToList();
            Series.ForEach(serie =>
            {
                settingsLocal.SeriesData.Add((serie, settingsLocal.pokemons.Where(poke => poke.Serie == serie).Count()));
            });

            generateOverlays(overlays, First: true);

            GenerateDexFull(data, settings, globalAppSettings);
            // buylist & scraplist
            ExportBuyList bl = new ExportBuyList(settings, data, globalAppSettings);
            ExportScrapList sl = new ExportScrapList(settings, data, globalAppSettings);

            bl.BuildDocument();
            sl.BuildDocument();

            // export commandGenerator.html

            ExportCommandGenerator commandGenerator = new ExportCommandGenerator(settings, data, globalAppSettings);
            commandGenerator.ExportFile();

            //  export pokestats.html
            ExportStats exportStats = new ExportStats(settings, data, globalAppSettings);
            exportStats.ExportFile();

            ExportIndividualPoke exportIndividualPoke = new ExportIndividualPoke(settings, data, globalAppSettings);
            exportIndividualPoke.ExportAllFile().Wait();

            ExportIndividualZone exportIndividualZone = new ExportIndividualZone(settings, data, globalAppSettings);
            exportIndividualZone.ExportAllFile().Wait();
        }

        #region LoadCustomDatas

        private static void LoadCustomTrainerCardsBackground(ref AppSettings settings)
        {
            // Chargement des backgrounds personnalisés pour les cartes de dresseur
            List<Background> custom = new List<Background>();
            string path = "./Data/Custom/TrainerCardsBackgrounds";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    List<Background> backgroundsInFile = JsonSerializer.Deserialize<List<Background>>(
                        File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(backgroundsInFile);
                    Commun.Logger($"white#Custom Trainer Cards Background loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{backgroundsInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            settings.TrainerCardsBackgrounds.AddRange(custom);
        }

        private static void LoadCustomOverlay(ref AppSettings settings)
        {
            // Chargement des overlays personnalisés
            List<CustomOverlay> custom = [];
            string path = "./Data/Custom/Overlays";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    List<CustomOverlay> overlaysInFile = JsonSerializer.Deserialize<List<CustomOverlay>>(
                        File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(overlaysInFile);
                    Commun.Logger($"white#Custom Overlay loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{overlaysInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            settings.customOverlays.AddRange(custom);
        }

        private static void LoadCustomBalls(ref AppSettings settings)
        {
            // Chargement des balles personnalisées
            List<Pokeball> custom = new List<Pokeball>();
            string path = "./Data/Custom/Balls";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    List<Pokeball> ballsInFile = JsonSerializer.Deserialize<List<Pokeball>>(
                        File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(ballsInFile);
                    Commun.Logger($"white#Custom Balls loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{ballsInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            settings.pokeballs.AddRange(custom);
        }

        private static void LoadCustomBadges(ref AppSettings settings)
        {
            // Chargement des badges personnalisés
            List<Badge> custom = new List<Badge>();
            string path = "./Data/Custom/Badges";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            foreach (string file in Directory.GetFiles(path, "*.json"))
            {
                try
                {
                    List<Badge> badgesInFile = JsonSerializer.Deserialize<List<Badge>>(
                        File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(badgesInFile);
                    Commun.Logger($"white#Custom Badges loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{badgesInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            settings.badges.AddRange(custom);
        }

        private static void LoadCustomCreature(ref AppSettings settings)
        {
            // Creatures personnalisées
            List<Pokemon> custom = [];
            if (!Directory.Exists("./Data/Custom/Creatures"))
            {
                Directory.CreateDirectory("./Data/Custom/Creatures");
            }
            foreach (string file in Directory.GetFiles("./Data/Custom/Creatures", "*.json"))
            {
                try
                {
                    List<Pokemon> pokeInFile = JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    custom.AddRange(pokeInFile);
                    Commun.Logger($"white#Custom Pokémon loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{pokeInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
            custom.ForEach(p => { p.isCustom = true; });
            settings.allPokemons.AddRange(custom);
        }

        private static void LoadCustomZone(ref AppSettings settings)
        {
            if (!Directory.Exists("./Data/Custom/Zones"))
            {
                Directory.CreateDirectory("./Data/Custom/Zones");
            }
            foreach (string file in Directory.GetFiles("./Data/Custom/Zones", "*.json"))
            {
                try
                {
                    List<Zone> zoneInFile = JsonSerializer.Deserialize<List<Zone>>(File.ReadAllText(file), Commun.GetJsonSerializerOptions());
                    settings.Zones.AddRange(zoneInFile);
                    Commun.Logger($"white#Zones loaded from file: |yellow#{Path.GetFileName(file)}|white# : |aqua#{zoneInFile.Count}|white#.");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading {file}: {e.Message}");
                }
            }
        }

        #endregion LoadCustomDatas

        private static string GetOneCreatureStat(GetPokeStats requestGetPokeStats, DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            List<Entrie> entries = data.GetEntriesByPseudo(requestGetPokeStats.User.Pseudo, requestGetPokeStats.User.Platform);
            if (entries.Count == 0)
            {
                return globalAppSettings.Texts.noCreatureRegistered;
            }
            Entrie target = entries.Where(entrie => entrie.IsLinkedWithThatCreatureName(requestGetPokeStats.Name)).FirstOrDefault();
            if (target == null)
            {
                if (settings.allPokemons.Where(poke => poke.Name_EN.ToLower() == requestGetPokeStats.Name || poke.Name_FR.ToLower() == requestGetPokeStats.Name || poke.Name_EN.ToLower() == requestGetPokeStats.Name).Count() == 0)
                {
                    return globalAppSettings.Texts.noCreatureWithThatName;
                }
                return globalAppSettings.Texts.CreatureNotRegistered;
            }
            TimeSpan fc = (DateTime.Now - target.dateFirstCatch);
            TimeSpan lc = (DateTime.Now - target.dateLastCatch);
            string TimeSinceFirstCapture = $"{fc.Days} ({target.dateFirstCatch.ToString("g")})";
            string TimeSinceLastCapture = $"{lc.Days} ({target.dateLastCatch.ToString("g")})";

            return globalAppSettings.Texts.pokeStatsInfos
                .Replace("[COUNT_NORMAL]", $"{target.CountNormal}")
                .Replace("[COUNT_SHINY]", $"{target.CountShiny}")
                .Replace("[TIME_SINCE_FIRST_CAPTURE]", TimeSinceFirstCapture)
                .Replace("[TIME_SINCE_LAST_CAPTURE]", TimeSinceLastCapture);
        }

        private static void LogInitialsDatas(AppSettings settings, GlobalAppSettings globalAppSettings, List<User> usersHere)
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
                string settingsServer = "Server settings (you can change those settings in _settings.json)";
                Console.WriteLine("\n" + settingsServer);

                Commun.Logger($"aqua#Log infos on console : |yellow#{globalAppSettings.Log.logConsole.console}");
                Commun.Logger($"aqua#Log Json on console (require infos on console) : |yellow#{globalAppSettings.Log.logConsole.logJsonOnConsole}");
                Commun.Logger($"aqua#Log also on File : |yellow#{globalAppSettings.Log.logFile}");
            }
        }

        private static string API_GetUserHere(List<User> usersHere)
        {
            try
            {
                return JsonSerializer.Serialize(usersHere.Where(x => x.Platform != "system").ToList());
            }
            catch (Exception e)
            {
                Console.WriteLine("API Error while genereting User HERE :  " + e.Message + "\n" + e.Data);
                return "";
            }
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
            try
            {
                List<User> users = data.GetAllUserPlatforms();
                users.ForEach(x =>
                {
                    data.GetEntriesByPseudo(pseudoTriggered: x.Pseudo, platformTriggered: x.Platform).ForEach(a => { a.code = x.Code_user; a.Validate(false); });
                });
            }
            catch (Exception e)
            {
                Console.WriteLine("Error while genereting PKServs Overlay :  " + e.Message + "\n" + e.Data);
            }
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
                    Commun.Logger($"red#{user.Pseudo}|white# (on |red#{user.Platform}|white#) a ajouté à la liste du giveaway.");
                    Console.WriteLine("\r");
                }
            }
        }

        /// <summary>
        /// Méthode spéciale qui récupère les sprites dans un dossiers
        /// </summary>
        /// <param name="allPokemons"></param>
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
                ExportDexAvailablePokemon a = new ExportDexAvailablePokemon(settings, data, globalAppSettings);
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
                string sentence = $"@{user.Pseudo} => Level {user.Stats.level} ({user.Stats.currentXP}/{user.Stats.MaxXPLevel}) {user.Stats.badges.Where(x => x.Obtained).Count()}/{settings.badges.Count} badges. Génère ton Pokédex pour plus d'infos.";
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
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n{ex.Data}---\n");
                return globalAppSettings.Texts.error;
            }
        }

        private static string GenerateDexFull(DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            try
            {
                var data = new ExportMain(appSettings, cnx, globalAppSettings);
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

        private static string API_FullExport(DataConnexion cnx, AppSettings appSettings, GlobalAppSettings globalAppSettings, bool forced = false, bool assets = true)
        {
            try
            {
                string result = "";
                if (assets)
                {
                    // export all dex
                    result = Exporter.DoFullExport(connexion: cnx, appSettings, globalAppSettings, forced);

                    // main
                    ExportMain exportMain = new ExportMain(appSettings, cnx, globalAppSettings);
                    exportMain.ExportFile().Wait();

                    //  export availablespokemon.html
                    ExportDexAvailablePokemon a = new ExportDexAvailablePokemon(appSettings, cnx, globalAppSettings);
                    a.GenerateFile();

                    // buylist & scraplist
                    ExportBuyList bl = new ExportBuyList(appSettings, cnx, globalAppSettings);
                    ExportScrapList sl = new ExportScrapList(appSettings, cnx, globalAppSettings);

                    bl.BuildDocument();
                    sl.BuildDocument();
                }

                // export commandGenerator.html

                ExportCommandGenerator commandGenerator = new ExportCommandGenerator(appSettings, cnx, globalAppSettings);
                commandGenerator.ExportFile();

                //  export pokestats.html
                ExportStats exportStats = new ExportStats(appSettings, cnx, globalAppSettings);
                exportStats.ExportFile();

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
    }
}