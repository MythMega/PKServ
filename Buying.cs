using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    public class Buying
    {
        public User User { get; set; }
        public UserRequest UserRequest { get; set; }
        public string pokename { get; set; }
        public string mode { get; set; }
        private DataConnexion dataConnexion { get; set; }
        private GlobalAppSettings globalAppSettings { get; set; }
        private AppSettings appSettings { get; set; }

        public Buying()
        {
            dataConnexion = null;
            appSettings = null;
            globalAppSettings = null;
        }

        public Buying(DataConnexion data, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            dataConnexion = data;
            this.appSettings = appSettings;
            this.globalAppSettings = globalAppSettings;
        }

        internal void SetEnv(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            dataConnexion = data;
            this.globalAppSettings = globalAppSettings;
            appSettings = settings;
        }

        public bool IsValide()
        {
            return appSettings.pokemons.Where(p => p.Name_FR.ToLower() == pokename.ToLower()).Count() == 1 ||
                 appSettings.pokemons.Where(p => p.Name_EN.ToLower() == pokename.ToLower()).Count() == 1;
        }

        public string DoResult()
        {
            string result = string.Empty;
            if (!IsValide())
            {
                return globalAppSettings.Texts.TranslationBuying.ElementDoesNotExist;
            }

            Pokemon poke = appSettings.pokemons.Where(x => x.Name_EN.ToLower() == pokename.ToLower() || x.Name_FR.ToLower() == pokename.ToLower()).FirstOrDefault();

            if (!new List<string> { "normal", "shiny" }.Contains(mode.ToLower()))
            {
                return globalAppSettings.Texts.TranslationBuying.BuyingModeNotRecognized;
            }

            if (poke == null)
            {
                return globalAppSettings.Texts.TranslationBuying.ElementDoesNotExist;
            }
            else
            {
                if (poke.priceNormal.HasValue && mode.ToLower() == "normal" || poke.priceShiny.HasValue && mode.ToLower() == "shiny")
                {
                    User.generateStats();
                    if (mode.ToLower() == "normal" && User.Stats.CustomMoney >= poke.priceNormal || mode.ToLower() == "shiny" && User.Stats.CustomMoney >= poke.priceShiny)
                    {
                        poke.isShiny = mode.ToLower() == "shiny";
                        new Work(UserRequest, dataConnexion, appSettings, globalAppSettings).ObtainPoke(User, poke);
                        int price = poke.isShiny ? poke.priceShiny.Value : poke.priceNormal.Value;
                        // ajouter la thune générée par le scrap à l'utilisateur
                        dataConnexion.UpdateUserStatsMoney(moneyEarned: User.Stats.CustomMoney - price, user: User, mode: "update");
                        result = $"+1 {poke.Name_FR}/{poke.Name_EN}, -{price} money, restant : {User.Stats.CustomMoney - price}";
                    }
                    else
                    {
                        string info = mode.ToLower() == "normal" ? poke.priceNormal.Value.ToString() : poke.priceShiny.Value.ToString();
                        return globalAppSettings.Texts.TranslationBuying.ElementTooExpensive + $"[{User.Stats.CustomMoney}/{info}]";
                    }
                }
                else
                {
                    return globalAppSettings.Texts.TranslationBuying.ElementNonBuyable;
                }
            }

            return result;
        }
    }
}