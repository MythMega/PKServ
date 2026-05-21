using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Giveaway/* :
    /// Giveaway/Claim
    /// </summary>
    public class GiveawayController : BaseController
    {
        public GiveawayController(ControllerContext ctx) : base(ctx) { }

        public override Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Giveaway/Claim":
                    GiveawayClaim claim = JsonSerializer.Deserialize<GiveawayClaim>(body, Ctx.JsonOptions);
                    Giveaway giveaway = Ctx.Settings.giveaways.Find(a => a.Code == claim.Code);
                    if (giveaway is null)
                        return Task.FromResult(Ctx.GlobalSettings.Texts.TranslationGiveaway.CodeDoesNotExist);

                    if (DateTime.Now < giveaway.Start)
                        return Task.FromResult(Ctx.GlobalSettings.Texts.TranslationGiveaway.CodeNotYetAvailable);
                    if (DateTime.Now > giveaway.End)
                        return Task.FromResult(Ctx.GlobalSettings.Texts.TranslationGiveaway.CodeExpired);

                    string result = GiveawayImpl.DoGiveaway(claim, giveaway, Ctx.GlobalSettings, Ctx.Settings, Ctx.Data);
                    Ctx.MarkForExport(claim.User);
                    // Broadcast SSE : le giveaway distribue un pokémon → dex globaux + stats session
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastSessionStats();
                    return Task.FromResult(result);

                default:
                    return Task.FromResult($"[GiveawayController] Route non reconnue : {path}");
            }
        }
    }
}
