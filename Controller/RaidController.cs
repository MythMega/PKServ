using PKServ.Business.Raid;
using PKServ.Admin;
using PKServ.Business;
using PKServ.Business.Raid;
using PKServ.Configuration;
using PKServ.Entity;
using PKServ.Entity.Raid;
using PKServ.Entity.Raid.ManualRandomRaid;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Raid/* :
    /// Raid/GiveawayPoke, Raid/Attack, Raid/Heal/Self, Raid/Heal/Full,
    /// Raid/ForceStatut, Raid/Save, Raid/Load, Raid/StartManualRandomRaid
    /// </summary>
    public class RaidController : BaseController
    {
        public RaidController(ControllerContext ctx) : base(ctx) { }

        public override async Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Raid/GiveawayPoke":
                    if (Ctx.Settings.ActiveRaid is null)
                        return Ctx.GlobalSettings.Texts.TranslationRaid.NoActiveRaid;
                    GiveawayPokeFromRaidRequest giveReq = JsonSerializer.Deserialize<GiveawayPokeFromRaidRequest>(body, Ctx.JsonOptions);
                    var givePoke = await Ctx.Settings.ActiveRaid.GivePoke(giveReq, appSettings: Ctx.Settings, globalAppSettings: Ctx.GlobalSettings);
                    // Notifie les clients SSE de l'overlay temps réel après distribution de pokémon
                    Ctx.BroadcastRaidUpdate();
                    // Un pokémon distribué → dex globaux + stats session
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastSessionStats();
                    return givePoke;

                case "Raid/Attack":
                    RaidAttacker attacker = JsonSerializer.Deserialize<RaidAttacker>(body, Ctx.JsonOptions);
                    await GlobalDataAction.UserClean(attacker.User, Ctx.Settings, Ctx.Data);
                    if (Ctx.Settings.ActiveRaid is null)
                        return Ctx.GlobalSettings.Texts.TranslationRaid.NoActiveRaid;
                    string attackResult = await Ctx.Settings.ActiveRaid.AttackAsync(attacker, Ctx.GlobalSettings, Ctx.Settings);
                    if (Ctx.AutoRaid && Ctx.Settings.ActiveRaid.isAutoRaid && Ctx.Settings.ActiveRaid.DefeatedTime is not null)
                    {
                        Ctx.Settings.ActiveRaid = null;
                        Ctx.LastRaidCheck = System.DateTime.Now;
                        Ctx.AutoRaidCount++;
                    }
                    // Notifie les clients SSE : si le raid vient de se terminer (ActiveRaid == null),
                    // les clients recevront { active: false } et masqueront l'overlay automatiquement.
                    Ctx.BroadcastRaidUpdate();
                    // Fin de raid → stats globales et session potentiellement modifiées
                    Ctx.BroadcastGlobalStats();
                    Ctx.BroadcastSessionStats();
                    return attackResult;

                case "Raid/Heal/Self":
                    User selfHealer = JsonSerializer.Deserialize<User>(body, Ctx.JsonOptions);
                    if (Ctx.Settings.ActiveRaid is null) return "Y a pas de raid en cours lol, scammed.";
                    return Ctx.Settings.ActiveRaid.Heal(selfHealer, self: true);

                case "Raid/Heal/Full":
                    User fullHealer = JsonSerializer.Deserialize<User>(body, Ctx.JsonOptions);
                    if (Ctx.Settings.ActiveRaid is null) return "Y a pas de raid en cours lol, scammed.";
                    return Ctx.Settings.ActiveRaid.Heal(fullHealer, self: false);

                case "Raid/ForceStatut":
                    RaidStatutApplication statut = JsonSerializer.Deserialize<RaidStatutApplication>(body, Ctx.JsonOptions);
                    return RaidStatutDistributionImpl.ProcessApplication(Ctx.Settings.ActiveRaid, statut);

                case "Raid/Save":
                    return RaidSaverImpl.SaveRaid(Ctx.Settings, Ctx.JsonOptions);

                case "Raid/Load":
                    var loadResult = RaidSaverImpl.LoadRaid(Ctx.Settings, Ctx.JsonOptions, Ctx.Data);
                    // Le raid vient d'être chargé → on pousse l'état initial aux clients SSE connectés
                    Ctx.BroadcastRaidUpdate();
                    return loadResult;

                case "Raid/StartManualRandomRaid":
                    ManualRandomRaidRequest raidReq = JsonSerializer.Deserialize<ManualRandomRaidRequest>(body, Ctx.JsonOptions);
                    var startResult = ManualRandomRaidImpl.StartRandomRaid(raidReq, Ctx.GlobalSettings, Ctx.Settings, Ctx.Data, Ctx.UsersHere.Count, Ctx.JsonOptions);
                    // Nouveau raid lancé manuellement → on prévient immédiatement les clients SSE
                    Ctx.BroadcastRaidUpdate();
                    return startResult;

                default:
                    return $"[RaidController] Route non reconnue : {path}";
            }
        }
    }
}
