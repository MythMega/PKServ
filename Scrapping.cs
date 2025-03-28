﻿using PKServ.Configuration;
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
        /// - fulldex
        /// - fulldexnormal
        /// - fulldexshiny
        /// </summary>
        public string mode { get; set; }

        private DataConnexion dataConnexion { get; set; }
        private GlobalAppSettings globalAppSettings { get; set; }
        private AppSettings appSettings { get; set; }

        public Scrapping()
        {
            dataConnexion = null;
            appSettings = null;
            globalAppSettings = null;
        }

        public Scrapping(DataConnexion data, AppSettings appSettings, GlobalAppSettings globalAppSettings)
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
            Pokemon poke = appSettings.pokemons.Where(p => Commun.isSamePoke(p, this.pokename)).FirstOrDefault();
            if (poke is null)
                return false;

            string nameFR = poke.Name_FR.ToLower();
            string nameEN = poke.Name_EN.ToLower();
            string altName = poke.AltName.ToLower();
            return dataConnexion.GetEntriesByPseudo(pseudoTriggered: User.Pseudo, platformTriggered: User.Platform).Where(p => p.PokeName.ToLower() == nameEN || p.PokeName.ToLower() == nameFR || p.PokeName.ToLower() == altName).Count() == 1 &&
                new List<string> { "complete", "all", "fullnormal", "fullshiny", "normal", "shiny", "fulldex", "fulldexnormal", "fulldexshiny" }.Contains(mode.ToLower());
        }

        public string DoResult(AppSettings settings)
        {
            if (this.mode == "")
            {
                return globalAppSettings.Texts.TranslationScrapping.ScrapModeNotGiven;
            }
            this.pokename = pokename.Replace('_', ' ').ToLower();
            Pokemon poke = appSettings.pokemons.Where(p => Commun.isSamePoke(p, this.pokename)).FirstOrDefault();

            string nameFR = String.Empty;
            string nameEN = String.Empty;
            string altName = String.Empty;

            if (poke is not null)
            {
                nameFR = poke.Name_FR.ToLower();
                nameEN = poke.Name_EN.ToLower();
                altName = poke.AltName.ToLower();
            }

            List<Entrie> entries = dataConnexion.GetEntriesByPseudo(pseudoTriggered: User.Pseudo, platformTriggered: User.Platform).Where(p => p.PokeName.ToLower() == nameEN || p.PokeName.ToLower() == nameFR || p.PokeName.ToLower() == altName).ToList();
            string resultat = string.Empty;

            int moneyEarned = 0;
            int scrapCountNormal = 0;
            int scrapCountShiny = 0;
            int multiplierNormal = 1;
            int multiplierShiny = 1;

            if (mode == "complete" || mode == "fullnormal" || mode == "fullshiny" || mode == "normal" || mode == "shiny" || mode == "all")
            {
                if (!IsValide())
                {
                    if (entries.Count != 1)
                    {
                        if (entries.Count < 1)
                            return globalAppSettings.Texts.TranslationScrapping.ElementNotRegistered;
                        Console.WriteLine($"Pokemon '{pokename}' has multiples entries ({entries.Count}). Run a FixEntries to fix it");
                        return globalAppSettings.Texts.error;
                    }
                    return globalAppSettings.Texts.TranslationScrapping.ScrapModeDoesNotExist;
                }
                Entrie targetEntrie = entries.FirstOrDefault();

                WorkOnAEntry(targetEntrie, ref resultat, ref moneyEarned, ref scrapCountNormal, ref scrapCountShiny, ref multiplierNormal, ref multiplierShiny);
            }
            else if (mode == "fulldex" || mode == "fulldexnormal" || mode == "fulldexshiny")
            {
                entries = dataConnexion.GetEntriesByPseudo(User.Pseudo, User.Platform).Where(entr => entr.CountNormal >= globalAppSettings.ScrapSettings.minimumToScrap + 1 || entr.CountShiny >= globalAppSettings.ScrapSettings.minimumToScrap + 1).ToList();
                foreach (Entrie each in entries)
                {
                    WorkOnAEntry(each, ref resultat, ref moneyEarned, ref scrapCountNormal, ref scrapCountShiny, ref multiplierNormal, ref multiplierShiny);
                }
            }

            // ajouter la thune générée par le scrap à l'utilisateur

            //patch temporaire
            moneyEarned = Math.Abs(moneyEarned);

            dataConnexion.UpdateUserStatsMoney(moneyEarned: moneyEarned, user: User, mode: "add");

            // ajouter la quantite d'element scrap aux stats
            dataConnexion.UpdateUserStatsScrapCount(normal: scrapCountNormal, shiny: scrapCountShiny, user: User);

            if (scrapCountShiny > 0 || scrapCountNormal > 0)
            {
                resultat = scrapCountNormal > 0 ? $" {scrapCountNormal} normal scrapped," : "";
                resultat += scrapCountShiny > 0 ? $" {scrapCountShiny} shiny scrapped," : "";
                resultat += $" +{moneyEarned} money.";
                if (!settings.UsersToExport.Where(u => u.Code_user == User.Code_user || (u.Pseudo == User.Pseudo && u.Platform == User.Platform)).Any())
                    settings.UsersToExport.Add(User);
            }
            else
            {
                if (User.Pseudo == "batgo_")
                {
                    resultat = "hé pas toi";
                }
                else
                {
                    resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                }
            }
            User.generateStats();
            resultat += $" Money actuelle : {User.Stats.CustomMoney}.";
            return resultat;
        }

        public void WorkOnAEntry(Entrie targetEntrie, ref string resultat, ref int moneyEarned, ref int scrapCountNormal, ref int scrapCountShiny, ref int multiplierNormal, ref int multiplierShiny)
        {
            int localScrapCountNormal = 0;
            int localScrapCountShiny = 0;
            multiplierNormal = 1;
            multiplierShiny = 1;

            #region Work

            Pokemon poke = appSettings.pokemons.Where(p => p.Name_FR.ToLower() == pokename.ToLower()).FirstOrDefault();
            if (poke == null)
            {
                poke = appSettings.pokemons.Where(p => p.Name_FR.ToLower() == targetEntrie.PokeName.ToLower()).FirstOrDefault();
                if (poke == null)
                {
                    return;
                }
            }
            if (poke.isLegendary && !poke.valueNormal.HasValue)
            {
                multiplierNormal = globalAppSettings.ScrapSettings.legendaryMultiplier;
            }

            if (poke.isLegendary && !poke.valueShiny.HasValue)
            {
                multiplierShiny = globalAppSettings.ScrapSettings.legendaryMultiplier;
            }
            switch (mode)
            {
                case "fulldex":
                case "complete":
                case "all":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap && targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    if (targetEntrie.CountNormal > globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        localScrapCountNormal += targetEntrie.CountNormal - globalAppSettings.ScrapSettings.minimumToScrap;
                        if (poke.valueNormal is null)
                            moneyEarned += localScrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                        else
                            moneyEarned += localScrapCountNormal * poke.valueNormal.Value * multiplierNormal;

                        targetEntrie.CountNormal = globalAppSettings.ScrapSettings.minimumToScrap;
                    }
                    if (targetEntrie.CountShiny > globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        localScrapCountShiny += targetEntrie.CountShiny - globalAppSettings.ScrapSettings.minimumToScrap;
                        if (poke.valueShiny is null)
                            moneyEarned += localScrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                        else
                            moneyEarned += localScrapCountShiny * poke.valueShiny.Value * multiplierShiny;

                        targetEntrie.CountShiny = globalAppSettings.ScrapSettings.minimumToScrap;
                    }

                    break;

                case "normal":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }
                    else
                    {
                        localScrapCountNormal++;
                    }

                    if (poke.valueNormal is null)
                        moneyEarned += localScrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                    else
                        moneyEarned += localScrapCountNormal * poke.valueNormal.Value * multiplierNormal;

                    targetEntrie.CountNormal -= 1;
                    break;

                case "fulldexnormal":
                case "fullnormal":
                    if (targetEntrie.CountNormal < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    localScrapCountNormal += targetEntrie.CountNormal - globalAppSettings.ScrapSettings.minimumToScrap;

                    if (poke.valueNormal is null)
                        moneyEarned += localScrapCountNormal * globalAppSettings.ScrapSettings.ValueDefaultNormal * multiplierNormal;
                    else
                        moneyEarned += localScrapCountNormal * poke.valueNormal.Value * multiplierNormal;

                    targetEntrie.CountNormal = globalAppSettings.ScrapSettings.minimumToScrap;
                    break;

                case "shiny":
                    if (targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }
                    else
                    {
                        localScrapCountShiny++;
                    }

                    if (poke.valueShiny is null)
                        moneyEarned += localScrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                    else
                        moneyEarned += localScrapCountShiny * poke.valueShiny.Value * multiplierShiny;

                    targetEntrie.CountShiny -= 1;
                    break;

                case "fulldexshiny":
                case "fullshiny":
                    if (targetEntrie.CountShiny < globalAppSettings.ScrapSettings.minimumToScrap)
                    {
                        resultat = globalAppSettings.Texts.TranslationScrapping.NotEnoughElementCopy;
                    }

                    localScrapCountShiny += targetEntrie.CountShiny - globalAppSettings.ScrapSettings.minimumToScrap;

                    if (poke.valueShiny is null)
                        moneyEarned += localScrapCountShiny * globalAppSettings.ScrapSettings.ValueDefaultShiny * multiplierShiny;
                    else
                        moneyEarned += localScrapCountShiny * poke.valueShiny.Value * multiplierShiny;

                    targetEntrie.CountShiny = globalAppSettings.ScrapSettings.minimumToScrap;
                    break;
            }

            // valider le modification en base de donnée (sans créer de nouvelle ligne)
            targetEntrie.Validate(NewLine: false);

            scrapCountNormal += localScrapCountNormal;
            scrapCountShiny += localScrapCountShiny;

            #endregion Work
        }
    }
}