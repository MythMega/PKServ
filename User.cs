using PKServ.Business.Users;
using PKServ.Business.Users.Stats;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ
{
    public class User
    {
        public int? Id { get; set; }
        public DataConnexion Data { get; set; }
        public string Pseudo { get; set; }
        public string Platform { get; set; }
        public string Code_user { get; set; }
        public string? AvatarUrl { get; set; }
        public Zone? Location { get; set; } // la zone de capture
        public Pokemon FavoritePoke { get; set; } // le pokemon favori du joueur
        public string? CardBackgroundUrl { get; set; } // l'url de l'image de fond de la carte
        public Stats Stats { get; set; }

        // OPTIM P2 : cache temporaire des entrées posé par GenerateBaseStats et consommé par
        // GenerateAchievements pour éviter un second aller-retour SQL lorsque les deux sont
        // appelées consécutivement. Non sérialisé, durée de vie = une session de calcul.
        [System.Text.Json.Serialization.JsonIgnore]
        public List<Entrie> _cachedEntries { get; set; }

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

        /// <summary>
        /// Renvoie true si le compte a fait sa dernière capture il y a plus de trois mois
        /// </summary>
        /// <returns></returns>
        public bool ArchivedAccount()
        {
            return lastCatch() < DateTime.Now.AddMonths(-3);
        }

        public void generateStatsAndAchievements(AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            this.Stats = StatsAchievementsImpl.GenerateBaseStats(Data, this);
            this.Stats = StatsAchievementsImpl.GenerateAchievements(appSettings, Data, globalAppSettings, this);
        }

        public void generateStats()
        {
            this.Stats = StatsAchievementsImpl.GenerateBaseStats(Data, this);
        }

        public void generateStatsAchievement(AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            this.Stats = StatsAchievementsImpl.GenerateAchievements(appSettings, Data, globalAppSettings, this);
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

        public string GetUserCardsHTML(AppSettings appSettings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            Code_user = dataConnexion.GetCodeUserByPlatformPseudo(this);
            generateStats();
            generateStatsAchievement(appSettings, globalAppSettings);

            return UserCardImpl.GetUserCardsHTML(appSettings, dataConnexion, globalAppSettings, this);
        }

        public string GetUserStatsHTML(AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            generateStats();
            generateStatsAchievement(appSettings, globalAppSettings);

            return UserStatsImpl.GetUserStatsHTML(appSettings, globalAppSettings, this);
        }

        public string GetUserBadgeHTML(AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion dataConnexion)
        {
            generateStats();
            generateStatsAchievement(appSettings, globalAppSettings);

            return UserBadgeImpl.GetUserBadgeHTML(appSettings, globalAppSettings, this);
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