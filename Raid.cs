using PKServ.Configuration;
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
        public Dictionary<User, int> UserDamageTotal { get; set; }
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

        // JSON
        public Pokemon Boss { get; set; }

        public int? PVMax { get; set; } = -1;
        public int? CatchRate { get; set; } = -1;
        public int? ShinyRate { get; set; } = -1;
        public string bossName { get; set; }

        [JsonConstructor]
        public Raid(string bossName, int? PVMax = null, int? catchRate = null, int? shinyRate = null)
        {
            UserDamageTotal = [];
            this.PVMax = PVMax;
            CatchRate = catchRate;
            ShinyRate = shinyRate;
            this.bossName = bossName;
            InitializeBoss(this.bossName);
            this.UserCodeCatchStatut = [];
            this.UserDamageBase = [];
            this.UserDamageTotal = [];
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
            this.Boss = pokemons.Where(x => x.AltName.ToLower() == Name || x.Name_EN.ToLower() == Name || x.Name_FR.ToLower() == Name).FirstOrDefault();
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
        public string Attack(User user, GlobalAppSettings globalAppSettings)
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
                damageDone = critical ? (int)(damageDone * 1.5) : damageDone + random.Next(30);
            }
            else
            {
                user.generateStats();
                damageDone = 50 +
                    (random.Next(100)) +
                    (user.Stats.dexCount * 2) +
                    (user.Stats.LengendariesRegistered * 10) +
                    (user.Stats.shinydex * 3);
                UserDamageBase[user] = damageDone;
            }

            if (UserDamageTotal.Where(u => u.Key.Code_user == user.Code_user).Any())
            {
                UserDamageTotal[UserDamageTotal.FirstOrDefault(u => u.Key.Code_user == user.Code_user).Key] += damageDone;
            }
            else
            {
                UserDamageTotal[user] = damageDone;
            }

            this.PV -= damageDone;
            if (this.PV <= 0)
            {
                this.PV = 0;
                this.DefeatedTime = DateTime.Now;
            }

            return critical ? $"CRITIQUE ! {user.Pseudo} fait {damageDone} dégats !" : $"{user.Pseudo} fait {damageDone} dégats ! [{PV}/{PVMax}].";
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

        public string GivePoke()
        {
            string r = string.Empty;
            string r_console = string.Empty;
            int count = 0;
            foreach (User user in this.UserDamageBase.Keys)
            {
                count++;
                Commun.ObtainPoke(user, Boss, DataConnexion, "mythmega");
                r_console += $"{user.Platform} • {user.Pseudo}\n";
            }
            r += $"{count} ont reçu le pokémon, voir console pour + d'info";
            Console.WriteLine("===================RAID===================");
            Console.WriteLine(r_console);
            Console.WriteLine("==========================================");
            return r;
        }
    }
}