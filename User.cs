using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace PKServ
{
    public class User
    {
        public DataConnexion Data { get; set; }
        public string Pseudo { get; set; }
        public string Platform { get; set; }
        public string Code_user { get; set; }
        public string? AvatarUrl { get; set; }
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
            Stats.favoritePoke = Data.GetDataUserStats_FavoritePoke(this);
            Stats.moneySpent = Data.GetDataUserStats_MoneySpent(Pseudo, Platform);
            Stats.ballLaunched = Data.GetDataUserStats_BallLaunched(Pseudo, Platform);
            Stats.giveawayNormal = Data.GetDataUserStats_Giveaway(Pseudo, Platform, false);
            Stats.giveawayShiny = Data.GetDataUserStats_Giveaway(Pseudo, Platform, true);
            Stats.CustomMoney = Data.GetDataUserStats_Money(Pseudo, Platform);
            Stats.scrappedNormal = Data.GetDataUserStats_Scrap(Pseudo, Platform, shiny: false);
            Stats.scrappedShiny = Data.GetDataUserStats_Scrap(Pseudo, Platform, shiny: true);
            Stats.TradeCount = Data.GetDataUserStats_TradeCount(this);
            Stats.RaidCount = Data.GetDataUserStats_RaidCount(this);
            Stats.RaidTotalDmg = Data.GetDataUserStats_RaidTotalDmg(this);

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

        public void generateStatsAchievement(AppSettings apS, GlobalAppSettings gas)
        {
            List<Entrie> entries = Data.GetEntriesByPseudo(Pseudo, Platform);
            TimeSpan diff = DateTime.Now - Stats.firstCatch;
            int days = diff.Days;
            List<Pokemon> pokemonsLegendaries = apS.pokemons.Where(x => x.isLegendary).ToList();
            List<Pokemon> pokemonsCustom = apS.pokemons.Where(x => x.isCustom).ToList();
            Stats.badges = apS.badges;

            var element = 0;
            Stats.badges.ForEach(x => x.Obtained = false);

            var aS = apS.pokemons; // Objet contenant par exemple la liste de tous les pokemons

            Parallel.ForEach(Stats.badges.Where(x => !x.Locked), badge =>
            {
                // On travaille sur une itération par badge.
                // Pour modifier "element" de façon thread-safe, on utilisera Interlocked.
                switch (badge.Type)
                {
                    case "TotalCatch":
                        if (badge.Value <= (Stats.pokeCaught - (Stats.giveawayNormal + Stats.giveawayShiny)))
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "ShinyCatch":
                        if (badge.Value <= (Stats.shinyCaught - Stats.giveawayShiny))
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalRegistered":
                        if (badge.Value <= entries.Count())
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "ShinyRegistered":
                        if (badge.Value <= entries.Count(e => e.CountShiny >= 1))
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "BallLaunched":
                        if (badge.Value <= Stats.ballLaunched)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "DaySinceStart":
                        if (badge.Value <= days)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "MoneySpent":
                        if (badge.Value <= Stats.moneySpent)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalTade":
                        if (badge.Value <= Stats.TradeCount)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalRaid":
                        if (badge.Value <= Stats.RaidCount)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalRaidDamages":
                        if (badge.Value <= Stats.RaidTotalDmg)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "LengendariesRegistered":
                        {
                            int count = 0;
                            foreach (Pokemon poke in pokemonsLegendaries)
                            {
                                // On peut aussi optimiser ici avec LINQ
                                count += entries.Count(entrie => entrie.PokeName == poke.Name_FR);
                            }
                            if (badge.Value <= count)
                            {
                                badge.Obtained = true;
                                Interlocked.Increment(ref element);
                            }
                        }
                        break;

                    case "CustomRegistered":
                        {
                            int count = 0;
                            foreach (Pokemon poke in pokemonsCustom)
                            {
                                count += entries.Count(entrie => entrie.PokeName == poke.Name_FR);
                            }
                            if (badge.Value <= count)
                            {
                                badge.Obtained = true;
                                Interlocked.Increment(ref element);
                            }
                        }
                        break;

                    case "TotalGiven":
                        if ((Stats.giveawayNormal + Stats.giveawayShiny) > badge.Value)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "ShinyGiven":
                        if (Stats.giveawayShiny > badge.Value)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "SpecificPoke":
                        {
                            // Note : Pour comparer en minuscules sans changer la culture, on peut utiliser ToLowerInvariant.
                            Entrie entry = entries
                                .FirstOrDefault(e => e.PokeName.ToLowerInvariant() == badge.SpecificValue.ToLowerInvariant());
                            if (entry != null && entry.CountNormal + entry.CountShiny >= badge.Value)
                            {
                                badge.Obtained = true;
                                Interlocked.Increment(ref element);
                            }
                        }
                        break;

                    case "MultiplePoke":
                        {
                            bool valide = true;
                            if (badge.SpecificValue.Contains(','))
                            {
                                foreach (string poke in badge.SpecificValue.Split(','))
                                {
                                    // On fait la comparaison en utilisant une casse uniforme.
                                    valide = entries.Any(e => e.PokeName.Equals(poke.Trim(), StringComparison.CurrentCultureIgnoreCase));
                                    if (!valide)
                                    {
                                        break;
                                    }
                                }
                            }
                            badge.Obtained = valide;
                            if (valide)
                            {
                                Interlocked.Increment(ref element);
                            }
                        }
                        break;

                    case "FullColecSeries":
                        {
                            List<Pokemon> targetList = apS.pokemons.Where(x => x.Serie == badge.SpecificValue).ToList();
                            int targetCount = targetList.Count;
                            foreach (Pokemon poke in targetList)
                            {
                                if (!entries.Any(e => Commun.isSamePoke(poke, e.PokeName)))
                                {
                                    badge.Obtained = false;
                                    break;
                                }
                            }
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;
                }
            });

            foreach (Badge bdg in Stats.badges)
            {
                if (bdg.Obtained)
                {
                    Stats.totalXP += bdg.XP;
                }
            }
            Stats.totalXP += Stats.normalCaught * gas.BadgeSettings.XPCatch;
            Stats.totalXP += Stats.shinyCaught * gas.BadgeSettings.XPShinyCatch;
            Stats.totalXP += Stats.ballLaunched * gas.BadgeSettings.XPBallLaunched;
            Stats.totalXP += days * gas.BadgeSettings.PerDayReward;
            Stats.MaxXPLevel = gas.BadgeSettings.XPRequiredToLevelUp;

            if (gas.BadgeSettings.LevelUpXPRequiredMultiplierPercent == 0)
            {
                Stats.currentXP = Stats.totalXP % gas.BadgeSettings.XPRequiredToLevelUp;
                Stats.MaxXPLevel = gas.BadgeSettings.XPRequiredToLevelUp;
                Stats.level = 1 + ((Stats.totalXP - Stats.currentXP) / gas.BadgeSettings.XPRequiredToLevelUp);
            }
            else
            {
                Stats.currentXP = Stats.totalXP;
                Stats.level = 1;
                while (Stats.currentXP > Stats.MaxXPLevel)
                {
                    Stats.level++;
                    Stats.currentXP -= Stats.MaxXPLevel;
                    Stats.MaxXPLevel += (int)(Stats.MaxXPLevel * gas.BadgeSettings.LevelUpXPRequiredMultiplierPercent / 100);
                }
            }
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
            catch
            {
                success = false;
            }
            return success;
        }

        public void ChangeBackground(string url)
        {
            DataConnexion data = new DataConnexion();
            data.UpdateCardBackground(this, url);
        }

        public string GetBackground()
        {
            string? url = Data.GetCardBackgroundUrl(this);
            if (string.IsNullOrEmpty(url))
                return "https://raw.githubusercontent.com/MythMega/PkServData/refs/heads/master/img/background/cards/base/base_card.jpg";
            return url;
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
        public int MaxXPLevel = 0;
        public int totalXP = 0;
        public int currentXP = 0;
        public int achievementCount = 0;
        public int currentAchievementCount = 0;
        public int LengendariesRegistered = 0;
        public int CustomRegistered = 0;
        public int CustomMoney = 0;
        public int TradeCount = 0;
        public int RaidCount = 0;
        public int RaidTotalDmg = 0;

        public Stats()
        {
        }
    }
}