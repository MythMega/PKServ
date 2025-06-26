using PKServ.Binding;
using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity.Raid;
using System;
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

        /// <summary>
        /// Last attack
        /// </summary>
        public Dictionary<string, DateTime> UserCodeLastAttack { get; set; }

        /// <summary>
        /// Statut
        /// </summary>
        public Dictionary<string, (string statut, int remainingTours, DateTime recoveryTime)> UserCodeStatut { get; set; }

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
        public string BossName { get; set; }

        public List<UserRaidStats> UserRaidStats { get; set; } = [];

        public bool? alreadyGiven { get; set; } = false;

        public string? LastAttackerUserCode { get; set; } = null;

        [JsonConstructor]
        public Raid(string bossName, bool displayShiny, int? PVMax = null, int? catchRate = null, int? shinyRate = null)
        {
            this.PVMax = PVMax;
            CatchRate = catchRate;
            ShinyRate = shinyRate;
            this.BossName = bossName;
            this.UserCodeCatchStatut = [];
            this.UserDamageBase = [];
            this.UserCodeLastAttack = [];
            this.UserCodeStatut = [];
            this.DisplayShiny = displayShiny;
            this.Stats = new RaidStats();
        }

        public void InitializeBoss(List<Pokemon> pokemons)
        {
            var options = new JsonSerializerOptions
            {
                IncludeFields = true
            };
            BossName = BossName.Trim().Replace("_", " ").ToLower();
            this.Boss = pokemons.Where(x => Commun.isSamePoke(x, BossName)).FirstOrDefault();
            if (this.Boss == null)
            {
                throw new Exception($"No boss with name {BossName}");
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
            LastAttackerUserCode = user.Code_user;

            Random random = new Random();
            bool critical = false;

            UserRaidStats personalStats;
            if (HasRaidStats(user))
            {
                personalStats = UserRaidStats.First(urs => urs.User.Code_user == user.Code_user);
            }
            else
            {
                personalStats = new UserRaidStats(user);
                UserRaidStats.Add(personalStats);
            }

            int damageDone = 0;
            if (UserDamageBase.Where(x => x.Key.Code_user == user.Code_user).Any())
            {
                bool skipCritical = UserCodeStatut.Keys.Contains(user.Code_user) && UserCodeStatut[user.Code_user].statut == StatutBinding.STATUT_BACKWIND;
                if (random.Next(12) == 2 && !skipCritical)
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

            string afkboost = "";
            string statutEffect = "";

            // gestion du statut

            if (UserCodeStatut.Keys.Contains(user.Code_user))
            {
                personalStats.TotalRoundUnderEffect += 1;
                switch (UserCodeStatut[user.Code_user].statut)
                {
                    case StatutBinding.STATUT_KO:
                        if (UserCodeStatut[user.Code_user].recoveryTime < DateTime.Now)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus Ko !";
                        }
                        else
                        {
                            damageDone = 0;
                            statutEffect = $"Ko jusqu'à {UserCodeStatut[user.Code_user].recoveryTime:HH\\hmm\\:ss}.";
                        }
                        break;

                    case StatutBinding.STATUT_PARALYZED:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus paralisé !";
                        }
                        else
                        {
                            damageDone = 0;
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            statutEffect = $"Paralisé pendant {UserCodeStatut[user.Code_user].remainingTours} tours.";
                        }
                        break;

                    case StatutBinding.STATUT_FROZEN:
                        if (random.Next(100) <= 30 || UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus gelé !";
                        }
                        else
                        {
                            damageDone = 0;
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            statutEffect = $"Toujours gelé.";
                        }
                        break;

                    case StatutBinding.STATUT_BURNT:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus brûlé !";
                        }
                        else
                        {
                            damageDone = damageDone = damageDone / 2;
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            statutEffect = $"Brûlé, dégat divisé par deux, pendant encore {UserCodeStatut[user.Code_user].remainingTours} tours.";
                        }
                        break;

                    case StatutBinding.STATUT_CONFUSED:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus confus !";
                        }
                        else
                        {
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            // 50% chance de multiplier les dégats par deux, sinon -1
                            if (random.Next(2) == 2)
                            {
                                damageDone = damageDone * -1;
                                statutEffect = $"Confus, tu as soigné le boss.";
                            }
                            else
                            {
                                damageDone = damageDone * 2;
                                statutEffect = $"Confus, tu as super attaqué le boss.";
                            }
                        }
                        break;

                    case StatutBinding.STATUT_BACKWIND:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus sous vent arrière ennemi !";
                        }
                        else
                        {
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                        }
                        break;

                    case StatutBinding.STATUT_ASLEEP:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus endormis !";
                        }
                        else
                        {
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            if (UserCodeStatut[user.Code_user].remainingTours == 0)
                            {
                                damageDone = 1;
                            }
                            else
                                damageDone = damageDone / UserCodeStatut[user.Code_user].remainingTours;
                        }
                        break;

                    case StatutBinding.STATUT_HEALINGFOUNTAIN:
                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus healer !";
                        }
                        else
                        {
                            if (UserCodeStatut.Keys.Count(x => x != user.Code_user) >= 1)
                            {
                                damageDone = 0;
                                string userToHeal = UserCodeLastAttack.Keys.Where(w => w != user.Code_user).ElementAt(random.Next(UserCodeLastAttack.Keys.Count(w => w != user.Code_user)));
                                UserCodeStatut.Remove(userToHeal);
                                personalStats.HealPeople += 1;
                            }
                        }
                        break;

                    case StatutBinding.STATUT_POISONED:

                        if (UserCodeStatut[user.Code_user].remainingTours <= 0)
                        {
                            UserCodeStatut.Remove(user.Code_user);
                            statutEffect = "Tu n'es plus empoisonné !";
                        }
                        else
                        {
                            bool poisonSomeone = false;

                            // 1) Construis la liste des cibles possibles
                            var potentialTargets = UserCodeLastAttack.Keys
                                .Where(code =>
                                    code != user.Code_user               // pas toi-même
                                    && !UserCodeStatut.ContainsKey(code) // pas déjà empoisonné
                                )
                                .ToList();

                            // 2) Si la liste n'est pas vide, choisis-en un au hasard
                            if (potentialTargets.Count > 0 && random.Next(6) <= 1)
                            {
                                string userToPoison = potentialTargets[
                                    random.Next(potentialTargets.Count)
                                ];

                                personalStats.PoisonOther += 1;
                                statutEffect +=
                                    $"{DataConnexion.GetPseudoByCodeUser(userToPoison)} a été empoisonné(e) par {user.Pseudo}. ";
                                poisonSomeone = true;
                                UserCodeStatut[userToPoison] = (StatutBinding.STATUT_POISONED, 3, DateTime.Now);
                            }

                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            damageDone = damageDone / 4;
                            string remain = $"Il te reste {UserCodeStatut[user.Code_user].remainingTours} tours de poison.";
                            statutEffect += poisonSomeone ? remain : $"Tu n'as empoisonné personne. {remain}";
                        }
                        break;
                }
            }
            else
            {
                int randValue = random.Next(0, 100);
                Console.WriteLine($"--------------------------------DEBUG = randValue - StatutEffect {randValue}");
                if (randValue <= 2)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_KO, 0, DateTime.Now.AddMinutes(5));
                    statutEffect = "Tu as été mis KO. Tes attaques ne feront plus de dégats pendant 5 minutes !";
                    personalStats.StatutCountKo += 1;
                }
                else if (randValue <= 3)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_FROZEN, 5, DateTime.Now);
                    statutEffect = "Tu as été gelé. Tes attaques ne feront plus de dégat jusqu'a ce que tu dégèle (30% de chance à chaque tour) !";
                    personalStats.StatutCountFrozen += 1;
                }
                else if (randValue <= 4)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_BURNT, 5, DateTime.Now);
                    statutEffect = "Tu as été brulé. Tes attaques ne feront moitié moins de dégat pendant 5 tours !";
                    personalStats.StatutCountBurnt += 1;
                }
                else if (randValue <= 5)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_PARALYZED, 3, DateTime.Now);
                    statutEffect = "Tu as été paralysé. Tes attaques ne feront pas de dégats pendant 3 tours !";
                    personalStats.StatutCountPara += 1;
                }
                else if (randValue <= 6)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_CONFUSED, 1, DateTime.Now);
                    statutEffect = "Tu es confus. Ta prochaine attaque peut doubler ou soignezr le boss !";
                    personalStats.StatutCountConfused += 1;
                }
                else if (randValue <= 7)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_ASLEEP, 3, DateTime.Now);
                    statutEffect = "Tu es endormis. Tes prochaines attaques seront faibles jusqu'a revenir a la normale !";
                    personalStats.StatutCountAsleep += 1;
                }
                else if (randValue <= 8)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_BACKWIND, 3, DateTime.Now);
                    statutEffect = "Tu es sous vent arrière. Tes prochaines attaques ne peuvent ni être chargées ni critique !";
                    personalStats.StatutCountBackWind += 1;
                }
                else if (randValue <= 9)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_HEALINGFOUNTAIN, 3, DateTime.Now);
                    statutEffect = "Tu ne fais plus de dégats pendant 3 tours, par contre tu heal un allié au hasard.";
                    personalStats.StatutCountHealing += 1;
                }
                else if (randValue <= 10)
                {
                    UserCodeStatut[user.Code_user] = (StatutBinding.STATUT_POISONED, 3, DateTime.Now);
                    statutEffect = "Tu es empoisonné ! Tes attaques sont divisées par 4 pendant 3 tours. 10% de chances de refiler le poison a quelqu'un a chaque attaque.";
                    personalStats.StatutCountPoisoned += 1;
                }
            }
            // charged attack
            if (damageDone > 0 && UserCodeLastAttack.ContainsKey(user.Code_user) && (!UserCodeStatut.Keys.Contains(user.Code_user) || UserCodeStatut[user.Code_user].statut != StatutBinding.STATUT_BACKWIND))
            {
                // minutes depuis la dernière attaque
                double minutes = (DateTime.Now - UserCodeLastAttack[user.Code_user]).TotalMinutes;
                // Le boost démarre à 2 min et culmine à +100% à 7 min
                double chargeBoost = 1 + Math.Min(1, Math.Max(0, (minutes - 2) / 5));
                //  minutes<2  => 1.0
                //  2≤min<7   => [1.0 → 2.0[
                //  min≥7     => 2.0

                damageDone = (int)(damageDone * chargeBoost);
                if (chargeBoost > 1.01)
                    afkboost += $"Attaque chargée : x{chargeBoost:F1}. ";
            }

            UserCodeLastAttack[user.Code_user] = DateTime.Now;

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

            string typeDamage = damageDone > 0 ? "dégats" : "soins";

            return critical ? $"[X{multiplier}] CRITIQUE ! {user.Pseudo} fait {Math.Abs(damageDone)} {typeDamage} ! [{PV}/{PVMax}]." : $"[X{multiplier}] {user.Pseudo} fait {damageDone} dégats ! {afkboost} {statutEffect} [{PV}/{PVMax}].";
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
            RecordsGeneratorImpl.GenerateRecords(DataConnexion, appSettings, globalAppSettings);
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

        public string Heal(User raidHealer, bool self)
        {
            UserRaidStats personalStats;
            if (HasRaidStats(raidHealer))
            {
                personalStats = UserRaidStats.First(urs => urs.User.Code_user == raidHealer.Code_user);
            }
            else
            {
                personalStats = new UserRaidStats(raidHealer);
                UserRaidStats.Add(personalStats);
            }

            if (self)
            {
                if (UserCodeStatut.ContainsKey(raidHealer.Code_user))
                {
                    UserCodeStatut.Remove(raidHealer.Code_user);
                    personalStats.HealSelf += 1;
                    return $"{raidHealer.Pseudo} s'est soigné (que lui)";
                }
                else
                {
                    return $"{raidHealer.Pseudo} a tenté de se soigner (mais n'était pas infecté)";
                }
            }
            else
            {
                int nombreDePersonneQuiAvaientUnStatut = UserCodeStatut.Count;
                personalStats.HealSelf += UserCodeStatut.Count;
                UserCodeStatut.Clear();
                return $"{raidHealer.Pseudo} a soigné tout le monde ! ( {nombreDePersonneQuiAvaientUnStatut} personne(s) )";
            }
        }

        public bool HasRaidStats(User element)
        {
            return UserRaidStats.Any(urs => urs.User.Code_user == element.Code_user);
        }
    }
}