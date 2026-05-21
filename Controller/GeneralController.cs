using PKServ.Business;
using PKServ.Business.Admin;
using PKServ.Business.Exports.JsonExporters;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Specialized;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes sans préfixe de namespace (segment unique).
    /// POST : CatchPoke, CatchPokeNew, Evolve, SignIn, ChoseFavoriteCreature,
    ///        GenerateDexFull, GetUserStats, GetPokeStats, GetUserLevels,
    ///        ScrapElement, BuyElement, ChangeBackground, GetOneValue
    /// GET  : Get, GetRaidInfos, GetRaidStatus
    /// </summary>
    public class GeneralController : BaseController
    {
        public GeneralController(ControllerContext ctx) : base(ctx) { }

        // ── POST ─────────────────────────────────────────────────────

        public override async Task<string> HandlePostAsync(string path, string body)
        {
            UserRequest ctx = null;

            switch (path)
            {
                case "CatchPoke":
                    ctx = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, Ctx.Data));
                    var catchResult = CatchPoke(ctx);
                    try { if (ctx.avatarUrl != null) Ctx.Data.UpdateAvatar(ctx); } catch { }
                    // Broadcast SSE : lancer de balle + sprite si capture + stats globales/session
                    Ctx.BroadcastBallThrow();
                    Ctx.BroadcastLastCaughtSprite();
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastGlobalMoneySpent();
                    Ctx.BroadcastSessionStats();
                    return catchResult;

                case "CatchPokeNew":
                    BallThrowRequest ballThrowRequest = JsonSerializer.Deserialize<BallThrowRequest>(body, Ctx.JsonOptions);
                    ballThrowRequest = await Admin.TempFix.FixUserNameYoutube(ballThrowRequest, Ctx.Data);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(ballThrowRequest.UserName, ballThrowRequest.Platform, ballThrowRequest.UserCode, Ctx.Data));
                    BallThrowTreatement traitement = new BallThrowTreatement();
                    await traitement.InitializeAsync(ballThrowRequest, Ctx.Settings, Ctx.Data, Ctx.GlobalSettings);
                    var catchPokeNewResult = await traitement.ProcessAsync(Ctx.Settings, Ctx.GlobalSettings, Ctx.Data);
                    // Broadcast SSE : lancer de balle + sprite si capture + stats globales/session
                    Ctx.BroadcastBallThrow();
                    Ctx.BroadcastLastCaughtSprite();
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastGlobalMoneySpent();
                    Ctx.BroadcastSessionStats();
                    return catchPokeNewResult;

                case "Evolve":
                    CreatureEvolutionRequest evolveReq = JsonSerializer.Deserialize<CreatureEvolutionRequest>(body, Ctx.JsonOptions);
                    try
                    {
                        evolveReq.SetCreatures(Ctx.Settings.pokemons, Ctx.GlobalSettings);
                        string evolveResult = evolveReq.DoEvolve(Ctx.Data, Ctx.GlobalSettings);
                        Ctx.MarkForExport(evolveReq.User);
                        return evolveResult;
                    }
                    catch (Exception e) { return e.Message; }

                case "SignIn":
                    ctx = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    User signInUser = JsonSerializer.Deserialize<User>(body, Ctx.JsonOptions);
                    User su = new User { Code_user = ctx.UserCode, Pseudo = signInUser.Pseudo, Platform = signInUser.Platform };
                    return SignIn(su);

                case "ChoseFavoriteCreature":
                    FavoriteCreatureRequest favReq = JsonSerializer.Deserialize<FavoriteCreatureRequest>(body, Ctx.JsonOptions);
                    if (favReq.IsValide(Ctx.Data, Ctx.Settings))
                    {
                        string favResult = favReq.Set(favReq.User, favReq.Name, favReq.Mode, Ctx.Data);
                        Ctx.MarkForExport(favReq.User);
                        return favResult;
                    }
                    return "invalide";

                case "GenerateDexFull":
                    return GenerateDexFull();

                case "GetUserStats":
                    ctx = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, Ctx.Data));
                    return GetUserStats(ctx);

                case "GetPokeStats":
                    GetPokeStats pokeStatsReq = JsonSerializer.Deserialize<GetPokeStats>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(pokeStatsReq.User.Pseudo, pokeStatsReq.User.Platform, pokeStatsReq.User.Code_user, Ctx.Data));
                    return GetOneCreatureStat(pokeStatsReq);

                case "GetUserLevels":
                    ctx = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(ctx.UserName, ctx.Platform, ctx.UserCode, Ctx.Data));
                    return GetUserLevels(ctx);

                case "ScrapElement":
                    try
                    {
                        Scrapping scrapping = JsonSerializer.Deserialize<Scrapping>(body, Ctx.JsonOptions);
                        scrapping.SetEnv(Ctx.Data, Ctx.Settings, Ctx.GlobalSettings);
                        return scrapping.DoResult(Ctx.Settings);
                    }
                    catch { Console.WriteLine("erreur scrap"); return ""; }

                case "BuyElement":
                    Buying buying = JsonSerializer.Deserialize<Buying>(body, Ctx.JsonOptions);
                    buying.SetEnv(Ctx.Data, Ctx.Settings, Ctx.GlobalSettings);
                    var buyResult = buying.DoResult();
                    // Broadcast SSE : un achat peut modifier les dex globaux (pokémon obtenu)
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastSessionStats();
                    return buyResult;

                case "ChangeBackground":
                    BackgroundChange bgChange = JsonSerializer.Deserialize<BackgroundChange>(body, Ctx.JsonOptions);
                    if (Ctx.GlobalSettings.AutoSignInGiveAway)
                        Ctx.AddToHere(new User(bgChange.User.Pseudo, bgChange.User.Platform, bgChange.User.Code_user, Ctx.Data));
                    if (bgChange.IsValide(Ctx.Data, Ctx.Settings))
                    {
                        string bgResult = bgChange.DoResult(Ctx.Settings);
                        Ctx.MarkForExport(bgChange.User);
                        return bgResult;
                    }
                    return "T'as pas le droit frere";

                case "GetOneValue":
                    SearchValue sv = JsonSerializer.Deserialize<SearchValue>(body, Ctx.JsonOptions);
                    sv.SetEnv(Ctx.Data, Ctx.Settings, Ctx.GlobalSettings, Ctx.UsersHere);
                    return sv.searchResult();

                default:
                    return $"Route non reconnue. \nDEBUG : {body}";
            }
        }

        // ── GET ──────────────────────────────────────────────────────

        public override Task<string> HandleGetAsync(string path, NameValueCollection query)
        {
            switch (path)
            {
                case "Get":
                    if (query.Count > 0 && query.AllKeys[0] == "Value")
                    {
                        SearchValue sv = new SearchValue();
                        sv.SetEnv(Ctx.Data, Ctx.Settings, Ctx.GlobalSettings, Ctx.UsersHere);
                        return Task.FromResult(sv.searchValue(query["Value"]));
                    }
                    return Task.FromResult($"Route non reconnue.");

                case "GetRaidInfos":
                    if (Ctx.Settings.ActiveRaid is not null)
                    {
                        var info = new
                        {
                            Url_Creature    = Ctx.Settings.ActiveRaid.DisplayShiny ? Ctx.Settings.ActiveRaid.Boss.Sprite_Shiny : Ctx.Settings.ActiveRaid.Boss.Sprite_Normal,
                            Url_Overlay     = Ctx.Settings.ActiveRaid.PV > 0 ? "https://upload.wikimedia.org/wikipedia/commons/thumb/8/89/HD_transparent_picture.png/1280px-HD_transparent_picture.png" : Ctx.GlobalSettings.RaidSettings.PictureOverlayWhenCreatureFainted,
                            Bar_Max         = Ctx.Settings.ActiveRaid.PVMax,
                            Bar_CurrentValue = Ctx.Settings.ActiveRaid.PV,
                            Rarity          = Ctx.Settings.ActiveRaid.Boss.Rarity,
                            Damages         = Ctx.Settings.ActiveRaid.GetDamagesOverlay()
                        };
                        return Task.FromResult(JsonSerializer.Serialize(info));
                    }
                    return Task.FromResult("{}");

                case "GetRaidStatus":
                    if (Ctx.Settings.ActiveRaid is null)
                        return Task.FromResult(Ctx.GlobalSettings.Texts.TranslationRaid.NoActiveRaid);
                    return Task.FromResult(Ctx.Settings.ActiveRaid.GetRaidStatuts());

                default:
                    return Task.FromResult($"Route non reconnue.");
            }
        }

        // ── Méthodes privées ─────────────────────────────────────────

        private string CatchPoke(UserRequest json)
        {
            try
            {
                string result = new Work(json, Ctx.Data, Ctx.Settings, Ctx.GlobalSettings).DoCatchRandomPoke();
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n{ex.Data}---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string GetUserStats(UserRequest ctx)
        {
            Ctx.TempFixUserCodeInBDD(ctx);
            try
            {
                User user = new User(ctx.UserName, ctx.Platform, ctx.UserCode, Ctx.Data);
                int total = Ctx.Settings.pokemons.Count;
                string sentence = $"@{user.Pseudo} => tu as eu {user.Stats.pokeCaught} poké dont {user.Stats.shinyCaught} shiny en tout, tes dex sont {Ctx.GlobalSettings.Texts.emotes.dex}[{user.Stats.dexCount}/{total} ({(user.Stats.dexCount * 100) / total}%)]{Ctx.GlobalSettings.Texts.emotes.dex} - {Ctx.GlobalSettings.Texts.emotes.shiny}[{user.Stats.shinydex}/{total} ({(user.Stats.shinydex * 100) / total}%)]{Ctx.GlobalSettings.Texts.emotes.shiny} ! {user.Stats.moneySpent}{Ctx.GlobalSettings.Texts.emotes.money} dépensés et {user.Stats.ballLaunched} {Ctx.GlobalSettings.Texts.emotes.ball} lancées. Money : {user.Stats.CustomMoney}.";
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {sentence}\n---\n");
                return sentence;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string GetUserLevels(UserRequest ctx)
        {
            Ctx.TempFixUserCodeInBDD(ctx);
            try
            {
                User user = new User(ctx.UserName, ctx.Platform, ctx.UserCode, Ctx.Data);
                user.generateStatsAchievement(Ctx.Settings, Ctx.GlobalSettings);
                string sentence = $"@{user.Pseudo} => Level {user.Stats.level} ({user.Stats.currentXP}/{user.Stats.MaxXPLevel}) {user.Stats.badges.FindAll(x => x.Obtained).Count}/{Ctx.Settings.badges.Count} badges. Génère ton Pokédex pour plus d'infos.";
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {sentence}\n---\n");
                return sentence;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string GetOneCreatureStat(GetPokeStats req)
        {
            var entries = Ctx.Data.GetEntriesByPseudo(req.User.Pseudo, req.User.Platform);
            if (entries.Count == 0) return Ctx.GlobalSettings.Texts.noCreatureRegistered;
            Entrie target = entries.Find(e => Commun.CompareStrings(e.PokeName, req.Name));
            if (target == null)
                return Ctx.Settings.allPokemons.FindAll(p => Commun.isSamePoke(p, req.Name)).Count == 0
                    ? Ctx.GlobalSettings.Texts.noCreatureWithThatName
                    : Ctx.GlobalSettings.Texts.CreatureNotRegistered;
            TimeSpan fc = DateTime.Now - target.dateFirstCatch;
            TimeSpan lc = DateTime.Now - target.dateLastCatch;
            return Ctx.GlobalSettings.Texts.pokeStatsInfos
                .Replace("[COUNT_NORMAL]", $"{target.CountNormal}")
                .Replace("[COUNT_SHINY]",  $"{target.CountShiny}")
                .Replace("[TIME_SINCE_FIRST_CAPTURE]", $"{fc.Days} ({target.dateFirstCatch:g})")
                .Replace("[TIME_SINCE_LAST_CAPTURE]",  $"{lc.Days} ({target.dateLastCatch:g})");
        }

        private string GenerateDexFull()
        {
            try
            {
                StaticFileCopier.EnsureDataDirectories();
                JsonExportPages.ExportMain(Ctx.Data, Ctx.Settings, Ctx.GlobalSettings);
                string result = "Génération JSON main.json effectuée avec succès.";
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string SignIn(User user)
        {
            bool alreadyIn = Ctx.UsersHere.Exists(b => b.Pseudo == user.Pseudo && b.Platform == user.Platform);
            if (alreadyIn) return $"@{user.Pseudo} tu fais dejà partie de la liste des participants :)";

            Ctx.Data.SetCodeUserByPlatformPseudo(item: user);
            var entries = Ctx.Data.GetEntriesByPseudo(pseudoTriggered: user.Pseudo, platformTriggered: user.Platform);
            entries.ForEach(entry => { entry.code = user.Code_user; entry.Validate(false); });
            Ctx.Data.SetCodeUserByPlatformPseudo(item: user);

            Ctx.UsersHere.Add(user);
            string json = JsonSerializer.Serialize(Ctx.UsersHere.FindAll(x => x.Platform != "system"), new JsonSerializerOptions { WriteIndented = true });
            System.IO.File.WriteAllText("./user.data", json);
            return $"@{user.Pseudo} tu as bien été ajouté a la liste des participants !";
        }
    }
}
