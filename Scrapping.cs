using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    public class Scrapping
    {
        public User User { get; set; }
        public string pokename { get; set; }

        /// <summary>
        /// can be :
        /// - full
        /// - normal
        /// - shiny
        /// - fullnormal
        /// - fullshiny
        /// </summary>
        public string mode { get; set; }

        private DataConnexion dataConnexion { get; set; }
        private GlobalAppSettings globalAppSettings { get; set; }
        private AppSettings appSettings { get; set; }

        public Scrapping()
        {
            this.dataConnexion = null;
            this.appSettings = null;
            this.globalAppSettings = null;
        }

        public Scrapping(DataConnexion data, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            this.dataConnexion = data;
            this.appSettings = appSettings;
            this.globalAppSettings = globalAppSettings;
        }


        internal void SetEnv(DataConnexion data, AppSettings settings, GlobalAppSettings globalAppSettings)
        {
            this.dataConnexion = data;
            this.globalAppSettings = globalAppSettings;
            this.appSettings = settings;
        }

        public bool IsValide()
        {
            return this.dataConnexion.GetEntriesByPseudo(pseudoTriggered: User.Pseudo, platformTriggered: User.Platform).Where(p => p.PokeName.ToLower() == pokename.ToLower()).Count() == 1 &&
                (new List<string> { "complete", "fullnormal", "fullshiny", "normal", "shiny" }.Contains(mode.ToLower()));
        }

        public string DoResult()
        {
            List<Entrie> entries = this.dataConnexion.GetEntriesByPseudo(User.Pseudo, User.Platform).Where(p => p.PokeName.ToLower() == pokename.ToLower()).ToList();
            string resultat = string.Empty;

            if (!IsValide())
            {
                if (entries.Count != 1)
                {
                    if (entries.Count < 1)
                        return globalAppSettings.Texts.TranslationScrapping.ElementNotRegistered;
                    Console.WriteLine($"Pokemon '{pokename}' has multiples entries ({entries.Count}). Run a FixEntries to fix it");
                    return globalAppSettings.Texts.error;
                }
                if (!new List<string> { "complete", "fullnormal", "fullshiny", "normal", "shiny" }.Contains(mode.ToLower()))
                {
                    return globalAppSettings.Texts.TranslationScrapping.ScrapModeDoesNotExist;
                }
            }

            #region Work

            Entrie targetEntrie = entries.FirstOrDefault();
            int moneyEarned = 0;
            int scrapCountNormal = 0;
            int scrapCountShiny = 0;
            int multiplierNormal = 1;
            int multiplierShiny = 1;
            Pokemon poke = appSettings.pokemons.Where(p => p.Name_FR.ToLower() == pokename.ToLower()).FirstOrDefault();

            if (poke.isLegendary && !poke.valueNormal.HasValue)
            {
                multiplierNormal = globalAppSettings.ScrapSettings.ValueDefaultNormal * globalAppSettings.ScrapSettings.legendaryMultiplier;
            }

            if (poke.isLegendary && !poke.valueShiny.HasValue)
            {
                multiplierShiny = globalAppSettings.ScrapSettings.ValueDefaultShiny * globalAppSettings.ScrapSettings.legendaryMultiplier;
            }
            switch (mode)
            {
                case "complete":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap && targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        return globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    if(targetEntrie.CountNormal > globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        scrapCountNormal = targetEntrie.CountNormal - globalAppSettings.ScrapSettings.minimumToScrap;
                        if (poke.valueNormal is null)
                            moneyEarned += scrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                        else
                            moneyEarned += scrapCountNormal * poke.valueNormal.Value * multiplierNormal;


                        targetEntrie.CountNormal = globalAppSettings.ScrapSettings.minimumToScrap;
                    }
                    if(targetEntrie.CountShiny > globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        scrapCountShiny = targetEntrie.CountShiny - globalAppSettings.ScrapSettings.minimumToScrap;
                        if (poke.valueShiny is null)
                            moneyEarned += scrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                        else
                            moneyEarned += scrapCountShiny * poke.valueShiny.Value * multiplierShiny;


                        targetEntrie.CountShiny = globalAppSettings.ScrapSettings.minimumToScrap;
                    }                    

                    break;

                case "normal":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        return globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    scrapCountNormal++;

                    if (poke.valueNormal is null)
                        moneyEarned += scrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                    else
                        moneyEarned += scrapCountNormal * poke.valueNormal.Value * multiplierNormal;


                    targetEntrie.CountNormal -= 1;
                    break;

                case "fullnormal":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        return globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    scrapCountNormal = targetEntrie.CountNormal - globalAppSettings.ScrapSettings.minimumToScrap;

                    if (poke.valueNormal is null)
                        moneyEarned += scrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                    else
                        moneyEarned += scrapCountNormal * poke.valueNormal.Value * multiplierNormal;

                    targetEntrie.CountNormal = globalAppSettings.ScrapSettings.minimumToScrap;
                    break;

                case "shiny":
                    if (targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        return globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    scrapCountShiny++;

                    if (poke.valueShiny is null)
                        moneyEarned += scrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                    else
                        moneyEarned += scrapCountShiny * poke.valueShiny.Value * multiplierShiny;


                    targetEntrie.CountShiny -= 1;
                    break;

                case "fullshiny":
                    if (targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        return globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    scrapCountShiny = targetEntrie.CountShiny - globalAppSettings.ScrapSettings.minimumToScrap;

                    if (poke.valueShiny is null)
                        moneyEarned += scrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                    else
                        moneyEarned += scrapCountShiny * poke.valueShiny.Value * multiplierShiny;

                    targetEntrie.CountShiny = globalAppSettings.ScrapSettings.minimumToScrap;
                    break;
            }

            // valider le modification en base de donnée (sans créer de nouvelle ligne)
            targetEntrie.Validate(NewLine: false);

            // ajouter la thune générée par le scrap à l'utilisateur

            //patch temporaire
            moneyEarned = Math.Abs(moneyEarned);

            dataConnexion.UpdateUserStatsMoney(moneyEarned: moneyEarned, user: User, mode: "add");

            // ajouter la quantite d'element scrap aux stats
            dataConnexion.UpdateUserStatsScrapCount(normal: scrapCountNormal, shiny: scrapCountShiny, user: User);

            #endregion Work
            if (scrapCountShiny > 0 || scrapCountNormal > 0)
            {

                resultat += scrapCountNormal > 0 ? $" {scrapCountNormal} normal scrapped," : "";
                resultat += scrapCountShiny > 0 ? $" {scrapCountShiny} shiny scrapped," : "";
                resultat += $" +{moneyEarned} money.";
            }
            else
            {
                if(User.Pseudo == "batgo_")
                {
                    resultat = "hé pas toi";
                }
                else
                {
                    resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                }
            }
            return resultat;
        }

    }
}