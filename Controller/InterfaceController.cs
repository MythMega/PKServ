using PKServ.Business;
using PKServ.Business._Tool;
using PKServ.Business.Exports.JsonExporters;
using PKServ.Configuration;
using PKServ.Entity;
using PKServ.Entity.Raid;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Interface/* :
    /// POST : Interface/GetAll/Creatures, Interface/GetAll/Balls, Interface/GetAll/Users,
    ///        Interface/LaunchBall, Interface/GiveAway, Interface/FullExport, Interface/Trade,
    ///        Interface/SignList, Interface/GenerateAvailableDex, Interface/ExecuteTask,
    ///        Interface/Raid/Start, Interface/Raid/Cancel,
    ///        Interface/Raid/Boost/Set, Interface/Raid/Boost/Cancel
    /// GET  : Interface/GetUserHere
    /// </summary>
    public class InterfaceController : BaseController
    {
        public InterfaceController(ControllerContext ctx) : base(ctx) { }

        // ── POST ─────────────────────────────────────────────────────

        public override Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Interface/GetAll/Creatures":
                    List<Pokemon> creaturesCopy = StreamDexTools.DeepClone(Ctx.Settings.pokemons, Ctx.JsonOptions);
                    creaturesCopy.ForEach(a => { a.ZonesList.Clear(); a.ZonesNames.Clear(); a.Artist.Clear(); });
                    return Task.FromResult(JsonSerializer.Serialize(creaturesCopy, Ctx.JsonOptions));

                case "Interface/GetAll/Balls":
                    return Task.FromResult(JsonSerializer.Serialize(Ctx.Settings.pokeballs, Ctx.JsonOptions));

                case "Interface/GetAll/Users":
                    return Task.FromResult(JsonSerializer.Serialize(Ctx.Data.GetAllUserPlatforms()));

                case "Interface/LaunchBall":
                    UserRequest ballReq = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    ballReq = Ctx.TempFixUserRequest(ballReq);
                    return Task.FromResult(SendBall(ballReq));

                case "Interface/GiveAway":
                    UserRequest giveReq = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    return Task.FromResult(GiveAway(giveReq));

                case "Interface/FullExport":
                    DateTime t0 = DateTime.Now;
                    UserRequest exportReq = JsonSerializer.Deserialize<UserRequest>(body, Ctx.JsonOptions);
                    bool forced = exportReq.TriggerName == "API_FWE_Force";
                    string exportResult = FullExport(forced);
                    Console.WriteLine((DateTime.Now - t0).TotalSeconds);
                    return Task.FromResult(exportResult);

                case "Interface/Trade":
                    Trade trade = JsonSerializer.Deserialize<Trade>(body, Ctx.JsonOptions);
                    trade.SetEnv(globalAppSettings: Ctx.GlobalSettings);
                    return Task.FromResult(DoTrade(trade));

                case "Interface/SignList":
                    return Task.FromResult(SignedUserHere());

                case "Interface/GenerateAvailableDex":
                    return Task.FromResult(GenerateAvailableDex());

                case "Interface/ExecuteTask":
                    ScheduledTask task = JsonSerializer.Deserialize<ScheduledTask>(body, Ctx.JsonOptions);
                    task = Ctx.GlobalSettings.ScheduledTasks.Find(t => t.ProcessFilePath == task.ProcessFilePath);
                    ExecuteTask(task);
                    return Task.FromResult("");

                case "Interface/Raid/Start":
                    Raid raid = JsonSerializer.Deserialize<Raid>(body, Ctx.JsonOptions);
                    raid.InitializeBoss(Ctx.Settings.pokemons);
                    raid.SetDefaultValues(Ctx.GlobalSettings, Ctx.Data);
                    Ctx.Settings.ActiveRaid = raid;
                    return Task.FromResult($"Raid {Ctx.Settings.ActiveRaid.Boss.Name_FR} {Ctx.Settings.ActiveRaid.PVMax}PV");

                case "Interface/Raid/Cancel":
                    if (Ctx.Settings.ActiveRaid is not null)
                    {
                        Ctx.Settings.ActiveRaid = null;
                        return Task.FromResult("Raid Stopped");
                    }
                    return Task.FromResult("No Active Raid");

                case "Interface/Raid/Boost/Set":
                    RaidDamageBoost boost = JsonSerializer.Deserialize<RaidDamageBoost>(body, Ctx.JsonOptions);
                    boost.Initialize();
                    if (Ctx.Settings.ActiveRaid is not null)
                        Ctx.Settings.ActiveRaid.ActiveBoost = boost;
                    else
                        return Task.FromResult("No Active Raid");
                    return Task.FromResult("");

                case "Interface/Raid/Boost/Cancel":
                    return Task.FromResult("");

                default:
                    return Task.FromResult($"[InterfaceController] Route non reconnue : {path}");
            }
        }

        // ── GET ──────────────────────────────────────────────────────

        public override Task<string> HandleGetAsync(string path, NameValueCollection query)
        {
            switch (path)
            {
                case "Interface/GetUserHere":
                    try
                    {
                        return Task.FromResult(JsonSerializer.Serialize(
                            Ctx.UsersHere.FindAll(x => x.Platform != "system")));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"API Error while genereting User HERE : {e.Message}\n{e.Data}");
                        return Task.FromResult("");
                    }

                default:
                    return Task.FromResult($"[InterfaceController] Route GET non reconnue : {path}");
            }
        }

        // ── Méthodes privées ─────────────────────────────────────────

        private string SendBall(UserRequest json)
        {
            try
            {
                string result = Ctx.Settings.pokeballs.Exists(p => p.Name == json.TriggerName)
                    ? new Work(json, Ctx.Data, Ctx.Settings, Ctx.GlobalSettings).DoCatchRandomPoke(true, Ctx.Settings.pokeballs.Find(p => p.Name == json.TriggerName))
                    : "No pokeball with that name exist. ";
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string GiveAway(UserRequest json)
        {
            try
            {
                string result = new Work(json, Ctx.Data, Ctx.Settings, Ctx.GlobalSettings).DistributePoke(Ctx.UsersHere);
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        public string FullExport(bool forced = false, bool assets = true)
        {
            try
            {
                string result = Exporter.DoFullExport(connexion: Ctx.Data, Ctx.Settings, Ctx.GlobalSettings, forced);
                StaticFileCopier.EnsureDataDirectories();
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string DoTrade(Trade trade)
        {
            try
            {
                string result = trade.DoWork();
                if (Ctx.GlobalSettings.Log.logConsole.console) Console.WriteLine($"---\nresult : {result}\n---\n");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"---\nERROR : {ex.InnerException}\n{ex.Message}\n---\n");
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string SignedUserHere()
        {
            string r = $"{Ctx.UsersHere.FindAll(w => w.Platform != "system").Count} personnes.\n\n";
            Ctx.UsersHere.FindAll(_ => true).ForEach(user => r += $"[{user.Platform}] {user.Pseudo}\n");
            return r;
        }

        private string GenerateAvailableDex()
        {
            try
            {
                ExportDexAvailablePokemon exporter = new ExportDexAvailablePokemon(Ctx.Settings, Ctx.Data, Ctx.GlobalSettings);
                exporter.GenerateFile();
                return exporter.filename;
            }
            catch (Exception ex) { return ex.Message; }
        }

        private static void ExecuteTask(ScheduledTask task)
        {
            if (task is null || !File.Exists(task.ProcessFilePath))
            {
                Console.WriteLine($"\n{task?.ProcessFilePath} not found.");
                return;
            }
            Process process = new Process();
            process.StartInfo.FileName         = task.ProcessFilePath;
            process.StartInfo.WorkingDirectory = Path.GetDirectoryName(task.ProcessFilePath);
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute  = false;
            process.StartInfo.CreateNoWindow   = true;
            process.Start();
            Console.WriteLine($"\ntask {Path.GetFileName(task.ProcessFilePath)} success.");
        }
    }
}
