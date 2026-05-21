using PKServ;
using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PKServ.Business.Users.Stats
{
    public static class StatsAchievementsImpl
    {
        // OPTIM P2 : surcharge qui accepte les entrées déjà chargées pour éviter un second
        // aller-retour SQL lorsque GenerateBaseStats les a déjà récupérées.
        // Appelée par GenerateBaseStats → generateStatsAchievement en interne.
        public static PKServ.Stats GenerateAchievements(AppSettings apS, DataConnexion Data, GlobalAppSettings gas, PKServ.User user,
            List<Entrie> preloadedEntries = null)
        {
            // Si les entrées sont passées en paramètre on les réutilise, sinon on les charge.
            // Cela évite le double-appel SQL quand GenerateBaseStats enchaîne sur GenerateAchievements.
            List<Entrie> entries = preloadedEntries
                ?? user._cachedEntries   // OPTIM P2 : cache posé par GenerateBaseStats
                ?? Data.GetEntriesByPseudo(user.Pseudo, user.Platform);
            List<(Entrie Entree, string Serie)> listSerieEntree = [];
            foreach (Entrie entry in entries)
            {
                listSerieEntree.Add(
                    (entry, apS.pokemons.FirstOrDefault(p => Commun.isSamePoke(p, entry.PokeName))?.Serie ?? "Inconnu"
                    ));
            }
            TimeSpan diff = DateTime.Now - user.Stats.firstCatch;
            int days = diff.Days;
            List<Pokemon> pokemonsLegendaries = apS.pokemons.Where(x => x.isLegendary).ToList();
            List<Pokemon> pokemonsCustom = apS.pokemons.Where(x => x.isCustom).ToList();

            // OPTIM P8 : anciennement user.Stats.badges = apS.badges assignait la référence
            // partagée de la liste globale. Lors d'exports parallèles (Task.WhenAll sur N users),
            // plusieurs threads mutaient badge.Obtained = true/false sur les MÊMES objets Badge
            // simultanément → race condition et résultats incorrects (badges obtenus ou non obtenus
            // aléatoirement selon l'ordre des threads).
            // On crée une copie superficielle des badges pour chaque utilisateur : chaque calcul
            // travaille sur ses propres instances, sans affecter les autres utilisateurs ni la liste
            // globale apS.badges.
            user.Stats.badges = apS.badges.Select(b => b.ShallowCopy()).ToList();

            var element = 0;
            user.Stats.badges.ForEach(x => x.Obtained = false);

            var aS = apS.pokemons;

            Parallel.ForEach(user.Stats.badges.Where(x => !x.Locked), badge =>
            {
                // Chaque badge appartient maintenant à cet utilisateur uniquement.
                // Interlocked reste nécessaire pour le compteur element (partagé entre threads).
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
                            badge.Obtained = targetCount <= listSerieEntree.Count(x => x.Serie == badge.SpecificValue);
                            if (badge.Obtained)
                            {
                                Interlocked.Increment(ref element);
                            }
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
            // OPTIM P1 : anciennement, cette méthode ouvrait 12 connexions SQLite séparées :
            //   GetEntriesByPseudo + GetDataUserStats_Scrap×2 + GetDataUserStats_FavoritePoke
            //   + GetDataUserStats_MoneySpent + GetDataUserStats_BallLaunched
            //   + GetDataUserStats_Giveaway×2 + GetDataUserStats_Money
            //   + GetDataUserStats_TradeCount + GetDataUserStats_RaidCount + GetDataUserStats_RaidTotalDmg
            //
            // Toutes les colonnes stats sont dans la table 'user' → une seule SELECT via GetUserStatsRow.
            // Les entrées (table 'entrees') restent une requête séparée car c'est une autre table.
            // Total : 12 connexions → 2 connexions par utilisateur.
            //
            // OPTIM P2 : les entrées chargées ici sont stockées dans _lastLoadedEntries pour être
            // réutilisées par GenerateAchievements si appelée dans la foulée, évitant un second
            // aller-retour SQL identique.

            List<Entrie> entrie = data.GetEntriesByPseudo(user.Pseudo, user.Platform);

            // Une seule requête pour toutes les colonnes stats de la table user
            var statsRow = data.GetUserStatsRow(user.Pseudo, user.Platform);

            user.Stats = new PKServ.Stats();
            user.Stats.dexCount = entrie.Count;
            // normalCaught = total capturé (hors scraps) + scraps normaux pour comptage cohérent
            user.Stats.normalCaught = user.getPokeCaught(entrie, shiny: false) + statsRow.pokeScrapped_normal;
            user.Stats.shinyCaught  = user.getPokeCaught(entrie, shiny: true)  + statsRow.pokeScrapped_shiny;
            user.Stats.pokeCaught   = user.Stats.normalCaught + user.Stats.shinyCaught;
            user.Stats.shinydex     = entrie.Count(entry => entry.CountShiny > 0);
            user.Stats.favoritePoke    = statsRow.favoriteCreature;
            user.Stats.moneySpent      = statsRow.Stat_MoneySpent;
            user.Stats.ballLaunched    = statsRow.Stat_BallLaunched;
            user.Stats.giveawayNormal  = statsRow.pokeReceived_normal;
            user.Stats.giveawayShiny   = statsRow.pokeReceived_shiny;
            user.Stats.CustomMoney     = statsRow.customMoney;
            user.Stats.scrappedNormal  = statsRow.pokeScrapped_normal;
            user.Stats.scrappedShiny   = statsRow.pokeScrapped_shiny;
            user.Stats.TradeCount      = statsRow.Stat_tradeCount;
            user.Stats.RaidCount       = statsRow.Stat_RaidCount;
            user.Stats.RaidTotalDmg    = statsRow.Stat_RaidTotalDmg;

            // OPTIM P2 : on stocke les entrées dans le champ dédié pour que
            // GenerateAchievements puisse les réutiliser sans requête SQL supplémentaire.
            user._cachedEntries = entrie;

            try
            {
                var firstEntry = entrie.OrderBy(e => e.dateFirstCatch).FirstOrDefault();
                user.Stats.firstCatch = firstEntry?.dateFirstCatch ?? DateTime.Now;
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