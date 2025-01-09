using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using PKServ.Configuration;

namespace PKServ
{
    public class User
    {
        public DataConnexion Data { get; set; }
        public string Pseudo { get; set; }
        public string Platform { get; set; }
        public string Code_user { get; set; }
        public Stats Stats { get; set; }

        // Constructeur sans paramètres nécessaire pour la désérialisation
        public User()
        {
            this.Data = new DataConnexion();
            this.Stats = new Stats();
        }

        public User(string unPseudo, string platform)
        {
            this.Pseudo = unPseudo;
            this.Platform = platform;
            this.Data = new DataConnexion();
            try
            {
                generateStats();
            }
            catch
            {
            }
        }

        public override string ToString()
        {
            return $"{Pseudo} ({Platform})";
        }

        public User(string unPseudo, string platform, string code_user, DataConnexion data)
        {
            this.Pseudo = unPseudo;
            this.Platform = platform;
            this.Data = data;
            this.Code_user = code_user;
            generateStats();
        }

        /// <summary>
        /// Retourne la date de dernière de capture
        /// </summary>
        /// <returns></returns>
        public DateTime lastCatch()
        {
            try
            {
                List<Entrie> donnee = Data.GetEntriesByPseudo(Pseudo, Platform).OrderByDescending(entry => entry.dateLastCatch).ToList();
                return donnee.First().dateLastCatch;
            }
            catch { return DateTime.MinValue; }
        }

        public void generateStats()
        {
            List<Entrie> entrie = Data.GetEntriesByPseudo(Pseudo, Platform);

            Stats = new Stats();
            Stats.dexCount = entrie.Count;
            Stats.normalCaught = getPokeCaught(entrie, shiny: false) + Data.GetDataUserStats_Scrap(this.Pseudo, this.Platform, shiny: false);
            Stats.shinyCaught = getPokeCaught(entrie, shiny: true) + Data.GetDataUserStats_Scrap(this.Pseudo, this.Platform, shiny: true);
            Stats.pokeCaught = Stats.normalCaught + Stats.shinyCaught;
            Stats.shinydex = entrie.Where(entry => entry.CountShiny > 0).ToList().Count;
            Stats.favoritePoke = entrie.OrderByDescending(e => e.CountNormal + (e.CountShiny * 2)).FirstOrDefault()?.PokeName;
            Stats.moneySpent = Data.GetDataUserStats_MoneySpent(Pseudo, Platform);
            Stats.ballLaunched = Data.GetDataUserStats_BallLaunched(Pseudo, Platform);
            Stats.giveawayNormal = Data.GetDataUserStats_Giveaway(Pseudo, Platform, false);
            Stats.giveawayShiny = Data.GetDataUserStats_Giveaway(Pseudo, Platform, true);
            Stats.CustomMoney = Data.GetDataUserStats_Money(Pseudo, Platform);
            Stats.scrappedNormal = Data.GetDataUserStats_Scrap(Pseudo, Platform, shiny: false);
            Stats.scrappedShiny = Data.GetDataUserStats_Scrap(Pseudo, Platform, shiny: true);

            try
            {
                Stats.firstCatch = entrie.OrderBy(entrie => entrie.dateFirstCatch).FirstOrDefault().dateFirstCatch;
            }
            catch { Stats.firstCatch = DateTime.Now; }
            try
            {
                Stats.catchratePercentage = (int)Math.Round(((double)Stats.pokeCaught) / Stats.ballLaunched);
            }
            catch { Stats.catchratePercentage = 0; }
            try
            {
                Stats.personalshinyRate = (int)Math.Round(((double)Stats.pokeCaught) / Stats.shinyCaught);
            }
            catch { Stats.personalshinyRate = 0; }
        }

        public void generateStatsAchievement(AppSettings aS, GlobalAppSettings gas)
        {
            List<Entrie> entries = Data.GetEntriesByPseudo(Pseudo, Platform);
            TimeSpan diff = DateTime.Now - Stats.firstCatch;
            int days = diff.Days;
            List<Pokemon> pokemonsLegendaries = aS.pokemons.Where(x => x.isLegendary).ToList();
            List<Pokemon> pokemonsCustom = aS.pokemons.Where(x => x.isCustom).ToList();
            Stats.badges = aS.badges;

            int count = 0;
            var element = 0;
            Stats.badges.ForEach(x => x.Obtained = false);
            foreach (Badge badge in Stats.badges.Where(x => !x.Locked))
            {
                switch(badge.Type)
                {
                    case "TotalCatch":
                        if (badge.Value <= (Stats.pokeCaught-(Stats.giveawayNormal+Stats.giveawayShiny)))
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "ShinyCatch":
                        if (badge.Value <= Stats.shinyCaught - Stats.giveawayShiny)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "TotalRegistered":
                        if (badge.Value <= entries.Count())
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "ShinyRegistered":
                        if (badge.Value <= entries.Where(x => x.CountShiny >= 1).Count())
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "BallLaunched":
                        if (badge.Value <= Stats.ballLaunched)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "DaySinceStart":
                        if (badge.Value <= days)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "MoneySpent":
                        if (badge.Value <= Stats.moneySpent)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "LengendariesRegistered":
                        count = 0;
                        foreach (Pokemon poke in pokemonsLegendaries)
                        {
                            foreach (Entrie entrie in entries)
                            {
                                if (entrie.PokeName == poke.Name_FR)
                                {
                                    count++;
                                }
                            }
                        }
                        if (badge.Value <= count)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;
                    case "CustomRegistered":
                        count = 0;
                        foreach (Pokemon poke in pokemonsCustom)
                        {
                            foreach (Entrie entrie in entries)
                            {
                                if (entrie.PokeName == poke.Name_FR)
                                {
                                    count++;
                                }
                            }
                        }
                        if (badge.Value <= count)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;

                    case "TotalGiven":
                        if (Stats.giveawayNormal+Stats.giveawayShiny > badge.Value) {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;

                    case "ShinyGiven":
                        if (Stats.giveawayShiny > badge.Value) {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;

                    case "SpecificPoke":
                        Entrie entry = entries.Where(e => e.PokeName.ToLower() == badge.SpecificValue.ToLower()).FirstOrDefault();
                        if (entry is not null && entry.CountNormal + entry.CountShiny >= badge.Value)
                        {
                            badge.Obtained = true;
                            element += 1;
                        }
                        break;

                    case "MultiplePoke":
                        bool valide = true;
                        if (badge.SpecificValue.Contains(','))
                        {
                            foreach(string poke in badge.SpecificValue.Split(','))
                            {
                                valide = entries.Where(e => e.PokeName.ToLower().Equals(poke.Trim().ToLower(), StringComparison.CurrentCultureIgnoreCase)).Any();
                                if (!valide) { break; }
                            }
                        }
                        badge.Obtained = valide;
                        element += valide ? 1 : 0;
                        break;
                }
                
            }
            foreach(Badge bdg in Stats.badges)
            {
                if(bdg.Obtained)
                {
                    Stats.totalXP += bdg.XP;
                }
            }
            Stats.totalXP += Stats.normalCaught * gas.BadgeSettings.XPCatch;
            Stats.totalXP += Stats.shinyCaught * gas.BadgeSettings.XPShinyCatch;
            Stats.totalXP += Stats.ballLaunched * gas.BadgeSettings.XPBallLaunched;
            Stats.totalXP += days * gas.BadgeSettings.PerDayReward;

            Stats.currentXP = Stats.totalXP % gas.BadgeSettings.XPPerLevel;
            Stats.level = 1 + ((Stats.totalXP - Stats.currentXP) / gas.BadgeSettings.XPPerLevel);

        }

        public int getPokeCaught(List<Entrie> entries, bool shiny)
        {
            int count = 0;
            if (shiny) { entries.ForEach(entry => count += entry.CountShiny); } else { entries.ForEach(entry => count += entry.CountNormal); }
            return count;
        }

        internal void DeleteAllEntries()
        {
            Data.DeleteAllEntries(this);
        }

        internal void DeleteUser()
        {
            Data.DeleteUser(this);
        }

        internal bool ValidateStatsBDD()
        {
            bool success = true;
            try
            {
                Data.UpdateUserAllStats(this);
            }
            catch {
                success = false;
            }
            return success;
        }
    }

    public class Stats
    {
        public int dexCount;
        public int shinydex;
        public int ballLaunched;
        public int moneySpent;
        public int pokeCaught;
        public int normalCaught;
        public int shinyCaught;
        public string favoritePoke;
        public DateTime firstCatch;
        public int catchratePercentage;
        public int personalshinyRate;
        public int giveawayNormal;
        public int giveawayShiny;
        public List<Badge> badges;
        public int scrappedShiny = 0;
        public int scrappedNormal = 0;
        public int level = 0;
        public int totalXP = 0;
        public int currentXP = 0;
        public int achievementCount = 0;
        public int currentAchievementCount = 0;
        public int LengendariesRegistered = 0;
        public int CustomRegistered = 0;
        public int CustomMoney = 0;

        public Stats()
        {
        }
    }
}