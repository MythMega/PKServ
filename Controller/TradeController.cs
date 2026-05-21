using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKServ.Controller
{
    /// <summary>
    /// Routes Trade/* :
    /// Trade/Request, Trade/Cancel, Trade/Accept
    /// </summary>
    public class TradeController : BaseController
    {
        public TradeController(ControllerContext ctx) : base(ctx) { }

        public override Task<string> HandlePostAsync(string path, string body)
        {
            switch (path)
            {
                case "Trade/Request":
                    return Task.FromResult(HandleTradeRequest(body));

                case "Trade/Cancel":
                    return Task.FromResult(HandleTradeCancel(body));

                case "Trade/Accept":
                    return Task.FromResult(HandleTradeAccept(body));

                default:
                    return Task.FromResult($"[TradeController] Route non reconnue : {path}");
            }
        }

        private string HandleTradeRequest(string body)
        {
            try
            {
                TradeRequest req = JsonSerializer.Deserialize<TradeRequest>(body, Ctx.JsonOptions);
                req.InitializePokemons(Ctx.Settings.pokemons);

                TradeRequest oldTrade = Ctx.Settings.TradeRequests.Find(t =>
                    t.UserWhoMadeRequest.Code_user == req.UserWhoMadeRequest.Code_user && !t.Completed);
                if (oldTrade is not null)
                    return $"{Ctx.GlobalSettings.Texts.TranslationTrading.atLeastOneTradeInitialized} [{Ctx.GlobalSettings.CommandSettings.CmdTradeCancel} {oldTrade.ID}]";

                var cond = Ctx.GlobalSettings.TradeSettings.TradeConditions;
                if (!cond.EnableShinyInTrade && (req.CreatureRequested.isShiny || req.CreatureSent.isShiny))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeShiny;
                if (!cond.EnableLockedPokemonInTrade && (req.CreatureRequested.isLock || req.CreatureSent.isLock))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeLocked;
                if (!cond.EnableLegendariesInTrade && (req.CreatureRequested.isLegendary || req.CreatureSent.isLegendary))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeShiny;
                if (!cond.EnableShinyAgainstNormal && (req.CreatureRequested.isShiny != req.CreatureSent.isShiny))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeShinyAndNormal;
                if (!cond.EnableTradeBetweenClassicAndCustom && (req.CreatureRequested.isCustom != req.CreatureSent.isCustom))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeClassicAndCustom;
                if (!cond.EnableTradeBetweenDifferentSeries && !string.Equals(req.CreatureRequested.Serie, req.CreatureSent.Serie, StringComparison.OrdinalIgnoreCase))
                    return Ctx.GlobalSettings.Texts.TranslationTrading.cannotTradeShiny;

                if (Ctx.GlobalSettings.TradeSettings.PaidTrade)
                {
                    req.CalculatePrice(Ctx.GlobalSettings);
                    req.UserWhoMadeRequest.generateStats();
                    if (req.Price > req.UserWhoMadeRequest.Stats.CustomMoney)
                        return Ctx.GlobalSettings.Texts.TranslationTrading.tooExpensive
                            .Replace("[PRICE]", $"{req.Price}")
                            .Replace("[CURRENT_MONEY]", $"{req.UserWhoMadeRequest.Stats.CustomMoney}");
                    if (!req.CheckIfCanTradeThisItem())
                        return Ctx.GlobalSettings.Texts.TranslationTrading.elementNotInPossession;
                }

                Ctx.Settings.TradeRequests.Add(req);
                return req.GetMessageCode(Ctx.GlobalSettings);
            }
            catch (Exception e)
            {
                try
                {
                    JsonDocument doc  = JsonDocument.Parse(body);
                    string pokeSent   = doc.RootElement.GetProperty("PokeSent").GetString();
                    string pokeWanted = doc.RootElement.GetProperty("PokeWanted").GetString();
                    if (e.Message == "Poke Wanted Not Found")
                        return Ctx.GlobalSettings.Texts.TranslationTrading.creatureNotFound.Replace("[CREATURE]", pokeWanted);
                    if (e.Message == "Poke Sent Not Found")
                        return Ctx.GlobalSettings.Texts.TranslationTrading.creatureNotFound.Replace("[CREATURE]", pokeSent);
                }
                catch { }
                return Ctx.GlobalSettings.Texts.error;
            }
        }

        private string HandleTradeCancel(string body)
        {
            TradeCancel cancel = JsonSerializer.Deserialize<TradeCancel>(body, Ctx.JsonOptions);
            TradeRequest tr = Ctx.Settings.TradeRequests.Find(t => t.ID == cancel.ID && !t.Completed);
            if (tr is null)
                return Ctx.GlobalSettings.Texts.TranslationTrading.codeInvalidOrExpired;
            if (cancel.User.Code_user != tr.UserWhoMadeRequest.Code_user)
                return Ctx.GlobalSettings.Texts.TranslationTrading.cannotCancelNotOwner;
            tr.Complete();
            Ctx.Settings.TradeRequests.Remove(tr);
            return Ctx.GlobalSettings.Texts.TranslationTrading.cancelled;
        }

        private string HandleTradeAccept(string body)
        {
            TradeAccept accept = JsonSerializer.Deserialize<TradeAccept>(body, Ctx.JsonOptions);
            TradeRequest tr = Ctx.Settings.TradeRequests.Find(t => t.ID == accept.ID && !t.Completed);
            if (tr is null)
                return Ctx.GlobalSettings.Texts.TranslationTrading.codeInvalidOrExpired;

            accept.UserWhoAccepted.generateStats();
            if (!accept.VerifEligibilityMoney(tr.Price))
                return Ctx.GlobalSettings.Texts.TranslationTrading.tooExpensive;
            if (!accept.VerifEligibilityCreature(tr.CreatureRequested, Ctx.Data))
                return Ctx.GlobalSettings.Texts.TranslationTrading.elementNotInPossession;

            tr.UserWhoAccepted = accept.UserWhoAccepted;
            var tradeAction = new Trade(tr);
            tradeAction.SetEnv(Ctx.GlobalSettings);
            string result = tradeAction.DoWork(paid: true);
            tr.Complete();
            Ctx.MarkForExport(tradeAction.trader1);
            Ctx.MarkForExport(tradeAction.trader2);
            Ctx.Settings.TradeRequests.Remove(tr);
            return result;
        }
    }
}
