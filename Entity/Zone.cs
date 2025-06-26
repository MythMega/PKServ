using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKServ.Entity
{
    public class ZoneChange
    {
        public User User { get; set; }
        public string Name { get; set; }

        // Constructeur par défaut requis pour la désérialisation
        public ZoneChange()
        { }

        internal bool IsValide(AppSettings settings)
        {
            return settings.Zones.Any(z => Commun.CompareStrings(z.Name, this.Name));
        }

        internal async Task<string> DoResult(AppSettings settings, DataConnexion dataConnexion)
        {
            Zone target = settings.Zones.FirstOrDefault(z => Commun.CompareStrings(z.Name, this.Name)) ?? throw new NullReferenceException("ZoneChange.ListErrors.Target");
            User.Code_user = dataConnexion.GetCodeUserByPlatformPseudo(this.User);
            await dataConnexion.SetUserZone(User.Code_user, target.Name);
            return $"Zone changée pour {User.Pseudo} vers {target.Name}";
        }

        internal List<string> ListErrors(AppSettings settings, GlobalAppSettings gas)
        {
            List<string> result = [];
            Zone target = settings.Zones.FirstOrDefault(z => Commun.CompareStrings(z.Name, this.Name)) ?? throw new NullReferenceException("ZoneChange.ListErrors.Target");
            User.generateStats();
            User.generateStatsAchievement(apS: settings, gas: gas);
            if (User.Stats.dexCount < target.DexRequirement)
            {
                result.Add($"Dex = {User.Stats.dexCount}/{target.DexRequirement}");
            }
            if (User.Stats.level < target.LevelRequirement)
            {
                result.Add($"Level = {User.Stats.level} / {target.LevelRequirement}");
            }
            return result;
        }
    }

    public class ZoneChangeAuto
    {
        public User User { get; set; }

        public bool SmartMode { get; set; } = false;

        public List<Zone> Zones { get; set; } = [];
        public List<Pokemon> Pokes { get; set; } = [];

        public ZoneChangeAuto()
        { }

        public ZoneChangeAuto(User user)
        {
            User = user;
        }

        public void SetPokesZones(List<Pokemon> pokes, List<Zone> zones)
        {
            Pokes = pokes;
            Zones = zones;
        }

        public async Task<string> DoResult(AppSettings settings, DataConnexion dataConnexion, GlobalAppSettings globalAppSettings)
        {
            string result = string.Empty;
            User.generateStats();
            User.generateStatsAchievement(apS: settings, gas: globalAppSettings);
            List<Zone> zones = Zones.Where(z => z.LevelRequirement <= User.Stats.level && z.DexRequirement <= User.Stats.dexCount).ToList();
            Zone? target = null;

            // Check if the user has any zone that match the requirements
            if (zones.Count == 0)
            {
                return "Aucune zone disponible pour le changement automatique.";
            }

            if (SmartMode)
            {
                // select the zone with the most pokemons available
                List<Entrie> entries = await dataConnexion.GetEntrieByCodeUser(User.Code_user, settings);

                Dictionary<Zone, (int pokeAvailable, int pokeCaught)> zonePokemonCount = new();
                foreach (var zone in zones)
                {
                    // Calcul du nombre de poké de la zone
                    int count = Pokes.Count(p => p.ZonesList.Any(z => z.Name.Equals(zone.Name, StringComparison.OrdinalIgnoreCase)));

                    // Calcul du nombre de poké capturés dans la zone
                    int caughtCount = Pokes.Count(p => p.ZonesList.Any(z => z.Name.Equals(zone.Name, StringComparison.OrdinalIgnoreCase)) && entries.Any(e => Commun.isSamePoke(p, e.PokeName)));
                    zonePokemonCount[zone] = (count, caughtCount);
                }

                // On prend la zone avec le plus gros pourcentage de pokémon non capturés et disponibles
                target = zonePokemonCount.OrderByDescending(zonePokemonCount => (zonePokemonCount.Value.pokeAvailable / zonePokemonCount.Value.pokeCaught)).FirstOrDefault().Key;
            }
            else
            {
                // Select a random zone from the available ones
                target = zones[new Random().Next(zones.Count)];
            }

            if (target == null)
            {
                return "Aucune zone valide trouvée pour le changement automatique.";
            }

            result = await new ZoneChange
            {
                User = User,
                Name = target.Name
            }.DoResult(settings, dataConnexion);
            return result;
        }
    }

    public class Zone
    {
        /// <summary>
        /// Nom de la zone
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description de la zone. Peut être vide
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Level requis. 0 si non indiqué.
        /// </summary>
        public int LevelRequirement { get; set; }

        /// <summary>
        /// Nombre de pokémon enregistrés minimum dans le dex.
        /// </summary>
        public int DexRequirement { get; set; }

        /// <summary>
        /// Image de la zone. Peut être vide.
        /// </summary>
        public string Image { get; set; } = "#";

        public string Region { get; set; } = "none";

        public Zone()
        { }

        public Zone(string name, string description = "", int levelRequirement = 0, int dexRequirement = 0, string image = "#", string region = "none")
        {
            Name = name;
            Description = description;
            LevelRequirement = levelRequirement;
            DexRequirement = dexRequirement;
            Image = image;
            Region = region;
        }
    }
}