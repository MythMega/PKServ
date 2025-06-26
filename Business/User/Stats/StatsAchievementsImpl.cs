using PKServ;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PKServ.Business.User.Stats
{
    public static class StatsAchievementsImpl
    {
        public static PKServ.Stats GenerateAchievements(AppSettings apS, DataConnexion Data, GlobalAppSettings gas, PKServ.User user)
        {
            List<Entrie> entries = Data.GetEntriesByPseudo(user.Pseudo, user.Platform);
            TimeSpan diff = DateTime.Now - user.Stats.firstCatch;
            int days = diff.Days;
            List<Pokemon> pokemonsLegendaries = apS.pokemons.Where(x => x.isLegendary).ToList();
            List<Pokemon> pokemonsCustom = apS.pokemons.Where(x => x.isCustom).ToList();
            user.Stats.badges = apS.badges;

            var element = 0;
            user.Stats.badges.ForEach(x => x.Obtained = false);

            var aS = apS.pokemons; // Objet contenant par exemple la liste de tous les pokemons

            Parallel.ForEach(user.Stats.badges.Where(x => !x.Locked), badge =>
            {
                // On travaille sur une itération par badge.
                // Pour modifier "element" de façon thread-safe, on utilisera Interlocked.
                switch (badge.Type)
                {
                    case "TotalCatch":
                        if (badge.Value <= (user.Stats.pokeCaught - (user.Stats.giveawayNormal + user.Stats.giveawayShiny)))
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "ShinyCatch":
                        if (badge.Value <= (user.Stats.shinyCaught - user.Stats.giveawayShiny))
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
                        if (badge.Value <= user.Stats.ballLaunched)
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
                        if (badge.Value <= user.Stats.moneySpent)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalTade":
                        if (badge.Value <= user.Stats.TradeCount)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalRaid":
                        if (badge.Value <= user.Stats.RaidCount)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "TotalRaidDamages":
                        if (badge.Value <= user.Stats.RaidTotalDmg)
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
                        if ((user.Stats.giveawayNormal + user.Stats.giveawayShiny) > badge.Value)
                        {
                            badge.Obtained = true;
                            Interlocked.Increment(ref element);
                        }
                        break;

                    case "ShinyGiven":
                        if (user.Stats.giveawayShiny > badge.Value)
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

            foreach (Badge bdg in user.Stats.badges)
            {
                if (bdg.Obtained)
                {
                    user.Stats.totalXP += bdg.XP;
                }
            }
            user.Stats.totalXP += user.Stats.normalCaught * gas.BadgeSettings.XPCatch;
            user.Stats.totalXP += user.Stats.shinyCaught * gas.BadgeSettings.XPShinyCatch;
            user.Stats.totalXP += user.Stats.ballLaunched * gas.BadgeSettings.XPBallLaunched;
            user.Stats.totalXP += days * gas.BadgeSettings.PerDayReward;
            user.Stats.MaxXPLevel = gas.BadgeSettings.XPRequiredToLevelUp;

            if (gas.BadgeSettings.LevelUpXPRequiredMultiplierPercent == 0)
            {
                user.Stats.currentXP = user.Stats.totalXP % gas.BadgeSettings.XPRequiredToLevelUp;
                user.Stats.MaxXPLevel = gas.BadgeSettings.XPRequiredToLevelUp;
                user.Stats.level = 1 + ((user.Stats.totalXP - user.Stats.currentXP) / gas.BadgeSettings.XPRequiredToLevelUp);
            }
            else
            {
                user.Stats.currentXP = user.Stats.totalXP;
                user.Stats.level = 1;
                while (user.Stats.currentXP > user.Stats.MaxXPLevel)
                {
                    user.Stats.level++;
                    user.Stats.currentXP -= user.Stats.MaxXPLevel;
                    user.Stats.MaxXPLevel += (int)(user.Stats.MaxXPLevel * gas.BadgeSettings.LevelUpXPRequiredMultiplierPercent / 100);
                }
            }

            return user.Stats;
        }

        internal static PKServ.Stats GenerateBaseStats(DataConnexion data, PKServ.User user)
        {
            List<Entrie> entrie = data.GetEntriesByPseudo(user.Pseudo, user.Platform);

            user.Stats = new PKServ.Stats();
            user.Stats.dexCount = entrie.Count;
            user.Stats.normalCaught = user.getPokeCaught(entrie, shiny: false) + data.GetDataUserStats_Scrap(user.Pseudo, user.Platform, shiny: false);
            user.Stats.shinyCaught = user.getPokeCaught(entrie, shiny: true) + data.GetDataUserStats_Scrap(user.Pseudo, user.Platform, shiny: true);
            user.Stats.pokeCaught = user.Stats.normalCaught + user.Stats.shinyCaught;
            user.Stats.shinydex = entrie.Where(entry => entry.CountShiny > 0).ToList().Count;
            user.Stats.favoritePoke = data.GetDataUserStats_FavoritePoke(user);
            user.Stats.moneySpent = data.GetDataUserStats_MoneySpent(user.Pseudo, user.Platform);
            user.Stats.ballLaunched = data.GetDataUserStats_BallLaunched(user.Pseudo, user.Platform);
            user.Stats.giveawayNormal = data.GetDataUserStats_Giveaway(user.Pseudo, user.Platform, false);
            user.Stats.giveawayShiny = data.GetDataUserStats_Giveaway(user.Pseudo, user.Platform, true);
            user.Stats.CustomMoney = data.GetDataUserStats_Money(user.Pseudo, user.Platform);
            user.Stats.scrappedNormal = data.GetDataUserStats_Scrap(user.Pseudo, user.Platform, shiny: false);
            user.Stats.scrappedShiny = data.GetDataUserStats_Scrap(user.Pseudo, user.Platform, shiny: true);
            user.Stats.TradeCount = data.GetDataUserStats_TradeCount(user);
            user.Stats.RaidCount = data.GetDataUserStats_RaidCount(user);
            user.Stats.RaidTotalDmg = data.GetDataUserStats_RaidTotalDmg(user);

            try
            {
                var a = entrie.OrderBy(entrie => entrie.dateFirstCatch).FirstOrDefault();
                if (a is not null)
                {
                    user.Stats.firstCatch = entrie.OrderBy(entrie => entrie.dateFirstCatch).FirstOrDefault().dateFirstCatch;
                }
                else
                {
                    user.Stats.firstCatch = DateTime.Now;
                }
            }
            catch { user.Stats.firstCatch = DateTime.Now; }
            try
            {
                user.Stats.catchratePercentage = (int)Math.Round(((double)user.Stats.pokeCaught) / user.Stats.ballLaunched);
            }
            catch { user.Stats.catchratePercentage = 0; }
            try
            {
                user.Stats.personalshinyRate = (int)Math.Round(((double)user.Stats.pokeCaught) / user.Stats.shinyCaught);
            }
            catch { user.Stats.personalshinyRate = 0; }

            return user.Stats;
        }
    }
}