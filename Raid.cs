using PKServ.Business;
using PKServ.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKServ
{
    public class Raid
    {
        public Dictionary<User, int> UserDamageBase { get; set; }

        /// <summary>
        /// UserCodeCatchStatut[Usercode] = Value
        /// Value = 1 : catch failed
        ///         2 : catch normal
        ///         3 : catch shiny
        /// </summary>
        public Dictionary<string, int> UserCodeCatchStatut { get; set; }

        public int PV { get; set; }
        public DataConnexion DataConnexion { get; set; }

        public DateTime? DefeatedTime { get; set; } = null;

        public DateTime StartedTime { get; set; } = DateTime.Now;

        // JSON
        public Pokemon Boss { get; set; }

        public RaidDamageBoost? ActiveBoost { get; set; }
        public RaidStats Stats { get; set; }
        public bool DisplayShiny { get; set; } = false;

        public int? PVMax { get; set; } = -1;
        public int? CatchRate { get; set; } = -1;
        public int? ShinyRate { get; set; } = -1;
        public string bossName { get; set; }

        public bool? alreadyGiven { get; set; } = false;

        [JsonConstructor]
        public Raid(string bossName, bool displayShiny, int? PVMax = null, int? catchRate = null, int? shinyRate = null)
        {
            this.PVMax = PVMax;
            CatchRate = catchRate;
            ShinyRate = shinyRate;
            this.bossName = bossName;
            InitializeBoss(this.bossName);
            this.UserCodeCatchStatut = [];
            this.UserDamageBase = [];
            this.DisplayShiny = displayShiny;
            this.Stats = new RaidStats();
        }

        public void InitializeBoss(string Name)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            List<Pokemon> pokemons = new List<Pokemon>();
            pokemons.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./pokemons.json"), options));
            pokemons.AddRange(JsonSerializer.Deserialize<List<Pokemon>>(File.ReadAllText("./customPokemons.json"), options));
            Name = Name.Trim().Replace("_", " ").ToLower();
            this.Boss = pokemons.Where(x => Commun.isSamePoke(x, Name)).FirstOrDefault();
            if (this.Boss == null)
            {
                throw new Exception($"No boss with name {Name}");
            }
        }

        /// <summary>
        /// Récupère PV, Catch/Shiny Rate par défaut
        /// </summary>
        /// <param name="settings"></param>
        public void SetDefaultValues(GlobalAppSettings settings, DataConnexion data)
        {
            this.PVMax = this.PVMax is null || this.CatchRate < 0 ? settings.RaidSettings.DefaultPV : this.PVMax;
            this.PV = this.PVMax.Value;
            this.CatchRate = this.CatchRate is null || this.CatchRate < 0 ? settings.RaidSettings.DefaultCatchRate : this.CatchRate;
            this.ShinyRate = this.ShinyRate is null || this.CatchRate < 0 ? settings.RaidSettings.DefaultShinyRate : this.ShinyRate;
            this.DataConnexion = data;
        }

        /// <summary>
        /// fais des dégats a partir des elements de l'utilisateurs
        /// </summary>
        /// <param name="user"></param>
        /// <param name="globalAppSettings"></param>
        /// <returns></returns>
        public string Attack(User user, GlobalAppSettings globalAppSettings, AppSettings settings)
        {
            Random random = new Random();
            bool critical = false;
            int damageDone = 0;
            if (UserDamageBase.Where(x => x.Key.Code_user == user.Code_user).Any())
            {
                if (random.Next(12) == 2)
                {
                    critical = true;
                }
                damageDone = UserDamageBase.Where(x => x.Key.Code_user == user.Code_user).FirstOrDefault().Value;
                damageDone = critical ? (int)(damageDone * 1.5) + random.Next(185) : damageDone + random.Next(130);
            }
            else
            {
                user.generateStats();
                user.generateStatsAchievement(settings, globalAppSettings);
                damageDone = 150 +
                    (random.Next(100)) +
                    (user.Stats.dexCount * 1) +
                    (user.Stats.LengendariesRegistered * 12) +
                    (user.Stats.shinydex * 3) +
                    (user.Stats.level * 20) +
                    (int)(user.Stats.pokeCaught / 10) +
                    (int)(user.Stats.shinyCaught / 2) +
                    user.Stats.RaidCount
                    ;
                damageDone = (int)Math.Ceiling(damageDone * (1 + (user.Stats.RaidCount / 50f)));
                UserDamageBase[user] = damageDone;
            }
            // gestion du boost
            int multiplier = 1;
            if (ActiveBoost is not null)
            {
                if (!ActiveBoost.Validity())
                {
                    ActiveBoost = null;
                }
                else
                {
                    multiplier = ActiveBoost.Multiplicator;
                    damageDone *= multiplier;
                }
            }

            if (Stats.UserDamageCount.Where(u => u.Key.Code_user == user.Code_user).Any())
            {
                Stats.UserDamageCount[Stats.UserDamageCount.FirstOrDefault(u => u.Key.Code_user == user.Code_user).Key] += 1;
            }
            else
            {
                Stats.UserDamageCount[user] = 1;
            }

            if (Stats.UserDamageTotal.Where(u => u.Key.Code_user == user.Code_user).Any())
            {
                Stats.UserDamageTotal[Stats.UserDamageTotal.FirstOrDefault(u => u.Key.Code_user == user.Code_user).Key] += damageDone;
            }
            else
            {
                Stats.UserDamageTotal[user] = damageDone;
            }

            if (Stats.PlatformDamage.ContainsKey(user.Platform))
            {
                Stats.PlatformDamage[user.Platform] += damageDone;
            }
            else
            {
                Stats.PlatformDamage[user.Platform] = damageDone;
            }

            this.PV -= damageDone;
            if (this.PV <= 0)
            {
                this.PV = 0;
                this.DefeatedTime = DateTime.Now;
            }

            return critical ? $"[X{multiplier}] CRITIQUE ! {user.Pseudo} fait {damageDone} dégats ! [{PV}/{PVMax}]." : $"[X{multiplier}] {user.Pseudo} fait {damageDone} dégats ! [{PV}/{PVMax}].";
        }

        /// <summary>
        /// Define if the boss is caught
        /// </summary>
        /// <param name="globalAppSettings"></param>
        /// <returns></returns>
        internal string Catch(GlobalAppSettings globalAppSettings, User catcher)
        {
            string result = string.Empty;
            bool caught = false;
            bool shiny = false;
            Random random = new Random();
            if (random.Next(100) < this.CatchRate)
            {
                caught = true;
                if (random.Next(100) < this.ShinyRate)
                {
                    shiny = true;
                }
            }
            else
            {
            }

            //Commun.ObtainPoke(user: catcher, poke: this.Boss, new DataConnexion() );

            return result;
        }

        internal string GetRaidStatuts()
        {
            return JsonSerializer.Serialize(this);
        }

        public string GivePoke(GiveawayPokeFromRaidRequest giveawayPokeFromRaidRequest, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            bool shiny = giveawayPokeFromRaidRequest.Shiny.StartsWith('s');
            try
            {
                if (alreadyGiven is not null && !alreadyGiven.Value)
                {
                    alreadyGiven = true;
                    foreach (User user in this.Stats.UserDamageTotal.Keys)
                    {
                        try
                        {
                            user.generateStats();
                            user.Stats.RaidTotalDmg += this.Stats.UserDamageTotal[user];
                            user.Stats.RaidCount++;
                            user.Stats.CustomMoney += (int)Math.Ceiling((decimal)this.Stats.UserDamageCount[user] / 5);
                            user.ValidateStatsBDD();
                            if (!appSettings.UsersToExport.Where(u => u.Code_user == user.Code_user || (u.Pseudo == user.Pseudo && u.Platform == user.Platform)).Any())
                                appSettings.UsersToExport.Add(user);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("------");
                            Console.WriteLine($"erreur stat {user}");
                            Console.WriteLine(ex.ToString());
                            Console.WriteLine("------");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("------");
                Console.WriteLine($"erreur stat globale");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("------");
            }
            string r = string.Empty;
            string r_console = string.Empty;
            int count = 0;
            Boss.isShiny = shiny;
            foreach (User user in this.UserDamageBase.Keys)
            {
                if (user == null)
                {
                    r += $"Erreur pour un gens ou le user est null\n";
                }

                count++;
                try
                {
                    Commun.ObtainPoke(user, Boss, DataConnexion, giveawayPokeFromRaidRequest.ChannelSource);
                }
                catch
                {
                    r += $"Erreur lors du don de poké à {user.Pseudo} [{user.Platform}]\n";
                }
            }
            // Trier par valeur croissante
            var sortedDict = Stats.UserDamageTotal.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
            // Afficher le dictionnaire trié
            foreach (var item in sortedDict)
            {
                r_console += $"{item.Key.Platform} • {item.Key.Pseudo}, {item.Value} dégats ! [en {Stats.UserDamageCount[item.Key]} Attaques] (avg {item.Value / Stats.UserDamageCount[item.Key]}/hit)\n";
            }

            r_console += "\n\nDamage per platform :\n";

            foreach (var item in Stats.PlatformDamage)
            {
                r_console += $" - {item.Key} : {item.Value}\n";
            }
            try
            {
                TimeSpan diff = this.DefeatedTime.Value - this.StartedTime;
                int secondes = (int)diff.TotalSeconds;
                int minutes = 0;
                int hours = 0;
                while (secondes > 60)
                {
                    minutes++;
                    secondes -= 60;
                }
                while (minutes > 60)
                {
                    hours++;
                    minutes -= 60;
                }
                r_console += $"Durée {hours} heures, {minutes} minutes, {secondes} secondes";
            }
            catch { }

            r += $"{count} ont reçu le pokémon, voir console pour + d'info";
            Console.WriteLine("===================RAID===================");
            Console.WriteLine(r_console);
            Console.WriteLine("==========================================");

            generateStatsCSV(settings: appSettings, data: DataConnexion, globalAppSettings: globalAppSettings);
            Commun.AddRecords($"Raid ({count} users)", this.Boss, shiny, DataConnexion);
            RecordsGeneratorImpl.GenerateRecords(DataConnexion, appSettings);
            return r;
        }

        private void generateStatsCSV(AppSettings settings, DataConnexion data, GlobalAppSettings globalAppSettings)
        {
            #region csv generation

            var sortedDict = Stats.UserDamageTotal.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            string csv = "platform;pseudo;damage;countAtk;baseDmg;level;raidCount\n";
            foreach (var item in sortedDict)
            {
                item.Key.generateStats();
                item.Key.generateStatsAchievement(settings, globalAppSettings);
                csv += $"{item.Key.Platform};{item.Key.Pseudo};{item.Value};{Stats.UserDamageCount[item.Key]};{UserDamageBase[item.Key]};{item.Key.Stats.level};{item.Key.Stats.RaidCount}\n";
            }
            if (!Directory.Exists("WebExport\\assets\\data"))
            {
                Directory.CreateDirectory("WebExport\\assets\\data");
            }
            File.WriteAllText("WebExport\\assets\\data\\RaidStats.csv", csv);
            File.WriteAllText("WebExport\\raid.html", Business.RaidStatsReportImpl.GenerateRaidReport(this, settings, globalAppSettings, data));

            #endregion csv generation
        }
    }

    public class RaidStats
    {
        public Dictionary<string, int> PlatformDamage { get; set; } = [];
        public Dictionary<User, int> UserDamageCount { get; set; } = [];
        public Dictionary<User, int> UserDamageTotal { get; set; } = [];

        public RaidStats()
        {
        }
    }

    public class RaidDamageBoost
    {
        public int Multiplicator { get; set; }
        public DateTime? End { get; set; }
        public int? Minute { get; set; }

        public RaidDamageBoost()
        {
        }

        /// <summary>
        /// dans le cas ou on veut une durée indéfinie, on met le max
        /// </summary>
        /// <param name="Multiplier"></param>
        public RaidDamageBoost(int Multiplicator)
        {
            this.Multiplicator = Multiplicator;
            End = DateTime.MaxValue;
        }

        /// <summary>
        /// dans le cas ou un json contiendrais une entrée "Minute"
        /// </summary>
        /// <param name="Multiplier"></param>
        /// <param name="Minute"></param>
        public RaidDamageBoost(int Multiplicator, int Minute)
        {
            this.Multiplicator = Multiplicator;
            End = DateTime.Now.AddMinutes(Minute);
        }

        public void Initialize()
        {
            if (Minute.HasValue)
            {
                End = DateTime.Now.AddMinutes(Minute.Value);
            }
            else
            {
                End = DateTime.MaxValue;
            }
        }

        public bool Validity()
        {
            return DateTime.Now < End;
        }
    }
}