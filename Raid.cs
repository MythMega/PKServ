using PKServ.Binding;
using PKServ.Business;
using PKServ.Business.Raid;
using PKServ.Configuration;
using PKServ.Entity.Raid;
using PKServ.Entity.Raid.ManualRandomRaid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PKServ
{
    public class Raid
    {
        public Dictionary<string, int> UserDamageBase { get; set; }

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

        public List<RaidDamages> RaidDamagesHistory { get; set; } = [];

        public List<UserRaidStats> UserRaidStats { get; set; } = [];

        public bool? alreadyGiven { get; set; } = false;

        public string? LastAttackerUserCode { get; set; } = null;
        public bool isAutoRaid { get; set; } = false;

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

        public Raid(AutoRaidSettings autoRaidSettings, AppSettings settings, int userHere, DataConnexion data)
        {
            this.PVMax = 0 + autoRaidSettings.BossCreatureBasePV + (userHere * autoRaidSettings.PVAdditionalPerUser);
            CatchRate = null;
            ShinyRate = null;
            this.Boss = CreatureImpl.GetPokeRandom(autoRaidSettings.BossCreatureRarity, autoRaidSettings.BossCreatureSerie, null, autoRaidSettings.IncludeLockCreatures, autoRaidSettings.IncludeLegendaryCreatures, settings);
            this.BossName = this.Boss.Name_FR;
            this.UserCodeCatchStatut = [];
            this.UserDamageBase = [];
            this.UserCodeLastAttack = [];
            this.UserCodeStatut = [];

            this.DisplayShiny = autoRaidSettings.BossCreatureShinyRate == 1 ||
                autoRaidSettings.BossCreatureShinyRate != 0 && new Random().Next(autoRaidSettings.BossCreatureShinyRate) == 1;
            this.Stats = new RaidStats();

            if (autoRaidSettings.RarityMultiplier)
            {
                this.PVMax = RaidFeaturesImpl.MultiplyPVByRarity(this.PVMax.Value, this.Boss.Rarity);
            }
            this.PV = this.PVMax.Value;
            this.DataConnexion = data;
            this.isAutoRaid = true;
        }

        public Raid(ManualRandomRaid ManualRandomRaid, AppSettings settings, int userHere, DataConnexion data)
        {
            this.PVMax = 0 + ManualRandomRaid.BossCreatureBasePV + (userHere * ManualRandomRaid.PVAdditionalPerUser);
            CatchRate = null;
            ShinyRate = null;
            if (ManualRandomRaid.CreatureSelectionMode == ManualRandomRaidSelector.CREATURE_SERIE_AND_RARITY)
                this.Boss = CreatureImpl.GetPokeRandom(ManualRandomRaid.BossCreatureRarity, ManualRandomRaid.BossCreatureSerie, null, ManualRandomRaid.IncludeLockCreatures, ManualRandomRaid.IncludeLegendaryCreatures, settings);
            else
            {
                this.Boss = CreatureImpl.GetPokeRandomFromNameList(ManualRandomRaid.CreatureList, settings);
            }
            this.BossName = this.Boss.Name_FR;
            this.UserCodeCatchStatut = [];
            this.UserDamageBase = [];
            this.UserCodeLastAttack = [];
            this.UserCodeStatut = [];

            this.DisplayShiny = ManualRandomRaid.BossCreatureShinyRate == 1 ||
                ManualRandomRaid.BossCreatureShinyRate != 0 && new Random().Next(ManualRandomRaid.BossCreatureShinyRate) == 1;
            this.Stats = new RaidStats();
            this.PV = this.PVMax.Value;
            this.DataConnexion = data;
            this.isAutoRaid = true;
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
        public async Task<string> AttackAsync(RaidAttacker attacker, GlobalAppSettings globalAppSettings, AppSettings settings)
        {
            User user = attacker.User;
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
            if (UserDamageBase.Where(x => x.Key == user.Code_user).Any())
            {
                bool skipCritical = UserCodeStatut.Keys.Contains(user.Code_user) && UserCodeStatut[user.Code_user].statut == StatutBinding.STATUT_BACKWIND;
                if (random.Next(12) == 2 && !skipCritical)
                {
                    critical = true;
                }
                damageDone = UserDamageBase.Where(x => x.Key == user.Code_user).FirstOrDefault().Value;
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
                    user.Stats.RaidCount * 3
                    ;
                damageDone = (int)Math.Ceiling(damageDone * (1 + (user.Stats.RaidCount / 200f)));
                UserDamageBase[user.Code_user] = damageDone;
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
                            statutEffect = "Tu n'es plus paralysé !";
                        }
                        else
                        {
                            damageDone = 0;
                            UserCodeStatut[user.Code_user] = (UserCodeStatut[user.Code_user].statut, UserCodeStatut[user.Code_user].remainingTours - 1, UserCodeStatut[user.Code_user].recoveryTime);
                            statutEffect = $"Paralysé pendant {UserCodeStatut[user.Code_user].remainingTours} tours.";
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
                    statutEffect = "Tu es confus. Ta prochaine attaque peut doubler ou soigner le boss !";
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

            if (Stats.UserDamageCount.Where(u => u.Key == user.Code_user).Any())
            {
                Stats.UserDamageCount[Stats.UserDamageCount.FirstOrDefault(u => u.Key == user.Code_user).Key] += 1;
            }
            else
            {
                Stats.UserDamageCount[user.Code_user] = 1;
            }

            if (Stats.UserDamageTotal.Where(u => u.Key == user.Code_user).Any())
            {
                Stats.UserDamageTotal[Stats.UserDamageTotal.FirstOrDefault(u => u.Key == user.Code_user).Key] += damageDone;
            }
            else
            {
                Stats.UserDamageTotal[user.Code_user] = damageDone;
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

            this.RaidDamagesHistory.Add(new RaidDamages
            {
                Active = true,
                Critical = critical,
                Damages = damageDone,
                User = attacker.User,
                Heal = damageDone < 0
            });

            if (this.PV <= 0)
            {
                this.PV = 0;
                this.DefeatedTime = DateTime.Now;
                if (globalAppSettings.RaidSettings.AutoRaidSettings.Enabled)
                {
                    await GivePoke(new GiveawayPokeFromRaidRequest
                    {
                        ChannelSource = attacker.ChannelSource,
                        Shiny = this.DisplayShiny ? "shiny" : "normal",
                    }, settings, globalAppSettings);
                }
            }

            string typeDamage = damageDone >= 0 ? "dégats" : "soins";
            if (critical || statutEffect.Length > 1)
            {
                return critical ? $"[X{multiplier}] CRITIQUE ! {user.Pseudo} fait {Math.Abs(damageDone)} {typeDamage} ! [{PV}/{PVMax}]." : $"[X{multiplier}] {user.Pseudo} fait {damageDone} dégats ! {afkboost} {statutEffect} [{PV}/{PVMax}].";
            }
            else
            {
                return "";
            }
        }

        internal string GetRaidStatuts()
        {
            return JsonSerializer.Serialize(this);
        }

        public async Task<string> GivePoke(GiveawayPokeFromRaidRequest giveawayPokeFromRaidRequest, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            bool shiny = giveawayPokeFromRaidRequest.Shiny.ToLower().StartsWith('s');
            try
            {
                if (alreadyGiven is not null && !alreadyGiven.Value)
                {
                    alreadyGiven = true;
                    foreach (string usercode in this.Stats.UserDamageTotal.Keys)
                    {
                        try
                        {
                            User user = await DataConnexion.GetUserByCodeUser(usercode);
                            user.generateStats();
                            user.Stats.RaidTotalDmg += this.Stats.UserDamageTotal[user.Code_user];
                            user.Stats.RaidCount++;
                            user.Stats.CustomMoney += (int)Math.Ceiling((decimal)this.Stats.UserDamageCount[user.Code_user] / 5);
                            user.ValidateStatsBDD();
                            if (!appSettings.UsersToExport.Where(u => u.Code_user == user.Code_user || (u.Pseudo == user.Pseudo && u.Platform == user.Platform)).Any())
                                appSettings.UsersToExport.Add(user);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("------");
                            Console.WriteLine($"erreur stat {usercode}");
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
            foreach (string usercode in this.UserDamageBase.Keys)
            {
                if (usercode is null)
                {
                    r += $"Erreur pour un gens ou le user est null\n";
                }

                count++;
                User user = await DataConnexion.GetUserByCodeUser(usercode);
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
            foreach (var itemcode in sortedDict)
            {
                User item = await DataConnexion.GetUserByCodeUser(itemcode.Key);
                r_console += $"{item.Platform} • {item.Pseudo}, {itemcode.Value} dégats ! [en {Stats.UserDamageCount[item.Code_user]} Attaques] (avg {itemcode.Value / Stats.UserDamageCount[itemcode.Key]}/hit)\n";
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

            await generateStatsCSV(settings: appSettings, data: DataConnexion, globalAppSettings: globalAppSettings);
            Commun.AddRecords($"Raid ({count} users)", this.Boss, shiny, DataConnexion);
            // L'export records.html était généré ici — remplacé par records.json via ExportRecordsAsync dans le cycle d'export normal.
            return r;
        }

        private async Task generateStatsCSV(AppSettings settings, DataConnexion data, GlobalAppSettings globalAppSettings)
        {
            // ── Export JSON raid (remplace le CSV) ───────────────────────────
            string raidDir = Path.Combine("WebExport", "Data", "raids");
            if (!Directory.Exists(raidDir))
                Directory.CreateDirectory(raidDir);

            // Calcul du coefficient de chance par user
            var lucks = new Dictionary<string, float>();
            foreach (var kv in Stats.UserDamageTotal)
            {
                float avg  = (float)kv.Value / Stats.UserDamageCount[kv.Key];
                lucks[kv.Key] = avg / UserDamageBase[kv.Key];
            }

            // Construction des entrées par joueur
            var players = new List<object>();
            var sortedByDmg = Stats.UserDamageTotal.OrderByDescending(x => x.Value);
            foreach (var kv in sortedByDmg)
            {
                User u = await data.GetUserByCodeUser(kv.Key);
                u.generateStats();
                u.generateStatsAchievement(settings, globalAppSettings);
                players.Add(new
                {
                    platform   = u.Platform,
                    pseudo     = u.Pseudo,
                    damage     = kv.Value,
                    countAtk   = Stats.UserDamageCount[kv.Key],
                    baseDmg    = UserDamageBase[kv.Key],
                    level      = u.Stats.level,
                    raidCount  = u.Stats.RaidCount,
                    luck       = lucks.TryGetValue(kv.Key, out float l) ? l : 1f
                });
            }

            // Damage par plateforme
            var platformDmg = Stats.PlatformDamage
                .Select(kv => new { platform = kv.Key, damage = kv.Value })
                .ToList();

            // Fun facts (UserRaidStats)
            var funFacts = new List<object>();
            foreach (var urs in UserRaidStats)
            {
                funFacts.Add(new
                {
                    pseudo        = urs.User.Pseudo,
                    platform      = urs.User.Platform,
                    healPeople    = urs.HealPeople,
                    healSelf      = urs.HealSelf,
                    poisonOther   = urs.PoisonOther,
                    ko            = urs.StatutCountKo,
                    para          = urs.StatutCountPara,
                    frozen        = urs.StatutCountFrozen,
                    burnt         = urs.StatutCountBurnt,
                    confused      = urs.StatutCountConfused,
                    backWind      = urs.StatutCountBackWind,
                    asleep        = urs.StatutCountAsleep,
                    healing       = urs.StatutCountHealing,
                    poisoned      = urs.StatutCountPoisoned,
                    roundsUnderFx = urs.TotalRoundUnderEffect
                });
            }

            TimeSpan duration = DefeatedTime.HasValue
                ? DefeatedTime.Value - StartedTime
                : TimeSpan.Zero;

            var raidJson = new
            {
                bossName    = Boss.Name_FR,
                bossSprite  = Boss.isShiny ? Boss.Sprite_Shiny  : Boss.Sprite_Normal,
                bossSpriteShiny = Boss.Sprite_Shiny,
                bossRarity  = Boss.Rarity,
                shiny       = Boss.isShiny,
                pvMax       = PVMax,
                date        = StartedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                durationSec = (int)duration.TotalSeconds,
                players,
                platformDmg,
                funFacts
            };

            string ts   = StartedTime.ToString("yyyy-MM-dd-HH-mm-ss");
            string path = Path.Combine(raidDir, $"raid-{ts}.json");
            string json = JsonSerializer.Serialize(raidJson, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(path, json);

            // Générer l'index des raids (liste des fichiers existants)
            await UpdateRaidIndexAsync(raidDir);

            // Maintien du rapport HTML legacy (génération supprimée, fichier conservé pour compatibilité)
            await File.WriteAllTextAsync(
                Path.Combine("WebExport", "raid.html"),
                $"<!-- Rapport déplacé vers raids.html -->\n<script>location.href='raids.html';</script>\n");
        }

        /// <summary>Génère WebExport/Data/raids/index.json listant tous les fichiers raid avec métadonnées légères.</summary>
        private static async Task UpdateRaidIndexAsync(string raidDir)
        {
            var entries = new List<object>();
            foreach (var filePath in Directory.GetFiles(raidDir, "raid-*.json")
                         .Where(f => !f.EndsWith("index.json"))
                         .OrderByDescending(f => f))
            {
                string filename = Path.GetFileName(filePath);
                string bossName = string.Empty;
                string date     = string.Empty;
                try
                {
                    // Lecture minimale : on ne désérialise que les deux premiers champs utiles
                    using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(filePath));
                    var root = doc.RootElement;
                    if (root.TryGetProperty("bossName", out var bn)) bossName = bn.GetString() ?? string.Empty;
                    if (root.TryGetProperty("date",     out var dt)) date     = dt.GetString() ?? string.Empty;
                }
                catch { /* fichier corrompu : on l'inclut quand même sans métadonnées */ }

                entries.Add(new { filename, bossName, date });
            }

            string indexPath = Path.Combine(raidDir, "index.json");
            await File.WriteAllTextAsync(indexPath,
                JsonSerializer.Serialize(new { files = entries }, new JsonSerializerOptions { WriteIndented = true }));
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

        public List<string> GetDamagesOverlay()
        {
            List<string> result = new List<string>();

            this.RaidDamagesHistory.Where(x => x.Active).ToList().ForEach(r =>
            {
                r.Active = false;
                result.Add($"{(r.Heal ? "+" : "")}{r.Damages}{(r.Critical ? "!" : "")}");
            });

            return result;
        }
    }
}