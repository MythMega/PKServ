using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes System/* :
    /// System/FixCodeUser, System/ClearEmptyAccounts, System/ReloadData,
    /// System/ClearPeopleHere, System/TransfertAccount
    /// </summary>
    public class SystemController : BaseController
    {
        public SystemController(ControllerContext ctx) : base(ctx) { }

        public override Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "System/FixCodeUser":
                    FixCodeUser();
                    return Task.FromResult("");

                case "System/ClearEmptyAccounts":
                    return Task.FromResult(ClearEmptyAccounts());

                case "System/ReloadData":
                    try
                    {
                        DataLoader.LoadAllData(Ctx.Settings, Ctx.GlobalSettings, Ctx.Data, Ctx.UsersHere);
                        DataLoader.LogInitialsDatas(Ctx.Settings, Ctx.GlobalSettings, Ctx.UsersHere);
                        Commun.Logger($"white#Tous les settings ont été rechargés |red#sauf le port du serveur, cet élement nécessite un redémarre si vous le changez !\n");
                        return Task.FromResult("system data reloaded");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        return Task.FromResult("ERROR" + ex.ToString());
                    }

                case "System/ClearPeopleHere":
                    try
                    {
                        Ctx.UsersHere.RemoveAll(x => x.Platform != "system");
                        File.WriteAllText("./user.data", "[]");
                        return Task.FromResult("success");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                        return Task.FromResult("ERROR" + ex.ToString());
                    }

                case "System/TransfertAccount":
                    AccountTransfert transfert = JsonSerializer.Deserialize<AccountTransfert>(body, Ctx.JsonOptions);
                    transfert.SetEnv(Ctx.Data);
                    return Task.FromResult(transfert.DoTransfert());

                default:
                    return Task.FromResult($"[SystemController] Route non reconnue : {path}");
            }
        }

        private void FixCodeUser()
        {
            try
            {
                List<User> users = Ctx.Data.GetAllUserPlatforms();
                users.ForEach(x =>
                    Ctx.Data.GetEntriesByPseudo(x.Pseudo, x.Platform)
                        .ForEach(a => { a.code = x.Code_user; a.Validate(false); }));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error while fixing code user : {e.Message}\n{e.Data}");
            }
        }

        private string ClearEmptyAccounts()
        {
            int counter = 0;
            var users = Ctx.Data.GetAllUserPlatforms();
            foreach (var user in users.Where(x =>
                x.Platform == "twitch" || x.Platform == "youtube" || x.Platform == "tiktok").ToList())
            {
                var entries = Ctx.Data.GetEntriesByPseudo(user.Pseudo, user.Platform);
                user.generateStats();
                if (entries == null || user.Stats.ballLaunched == 0)
                {
                    counter++;
                    if (Ctx.GlobalSettings.Log.logConsole.console)
                        Console.WriteLine($"{user.Pseudo} on {user.Platform} [{user.Code_user}] Deleted ({entries?.Count ?? 0} entries, {user.Stats.ballLaunched} ball launched.)");
                    user.DeleteUser();
                    user.DeleteAllEntries();
                }
            }
            return $"{counter} users deleted.";
        }
    }
}
