using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    public class AppSettings
    {
        public int version = 13;

        public List<Pokemon> pokemons = [];

        public List<Pokemon> allPokemons = [];

        public List<Pokeball> pokeballs = [];

        public List<Trigger> triggers = [];

        public List<Badge> badges = [];

        public Dictionary<string, string> trads = [];

        public List<CatchHistory> catchHistory = [];

        public List<BallThrowHistory> ballThrowHistory = [];

        public DateTime LastFullExport = DateTime.MinValue;

        public List<CustomOverlay> customOverlays = new List<CustomOverlay>();

        public List<TradeRequest> TradeRequests = new List<TradeRequest>();

        public Raid? ActiveRaid = null;

        public List<Background> TrainerCardsBackgrounds = new List<Background>();

        public List<Giveaway> giveaways = [];

        public List<User> UsersToExport = [];

        public List<(string Serie, int Count)> SeriesData = [];

        public List<Zone> Zones = [];

        public AppSettings()
        {
        }

        // OPTIM P6 : anciennement chaque appel à getOnePoke() créait un new Random() (coût
        // système pour l'entropie) et appelait Where(...).ToList() (allocation d'une nouvelle
        // liste à chaque tirage). On utilise Random.Shared (instance thread-safe .NET 6+) et
        // on calcule les listes filtrées à la demande en évitant le double-Where.
        private static readonly Random _rng = Random.Shared;

        public Pokemon getOnePoke()
        {
            // OPTIM P6 : un seul Where + ToList, une seule instance Random réutilisée
            var available = pokemons.Where(x => !x.isLock).ToList();
            return available[_rng.Next(available.Count)];
        }

        public Pokemon getOnePokeShiny()
        {
            // OPTIM P6 : idem
            var available = pokemons.Where(x => !x.isLock && !x.isShinyLock).ToList();
            return available[_rng.Next(available.Count)];
        }

        public Pokemon getOnePoke(string type)
        {
            var a = pokemons.Where(x => !x.isLock).ToList();
            List<Pokemon> pokemonsCompatibles = new List<Pokemon>();
            foreach (var poke in a)
            {
                try
                {
                    if (poke.Type2 is null) { poke.Type2 = ""; }
                    if (poke.Type1.ToLower() == type.ToLower() || poke.Type2.ToLower() == type.ToLower())
                    {
                        pokemonsCompatibles.Add(poke);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine($"{poke.Name_FR} does not have type 1");
                }
            }
            // OPTIM P6 : Random.Shared à la place de new Random()
            return pokemonsCompatibles[_rng.Next(pokemonsCompatibles.Count)];
        }

        public string GetText(string code) => trads[code];

        public int GetIdPokeByPoke(Pokemon poke) => pokemons.IndexOf(pokemons.Where(w => w.Name_FR == poke.Name_FR).FirstOrDefault());

        public int GetIdPokeByName(string pokeName) => pokemons.IndexOf(pokemons.Where(w => w.Name_FR == pokeName).FirstOrDefault());

        internal Pokemon getOnePokeFromBall(Pokeball pkb, Zone zone, bool shinyForced = false)
        {
            List<Pokemon> pokemonsAvailable = pokemons.Where(x => !x.isLock).ToList();

            if (pokemonsAvailable.Count == 0)
                throw new Exception("No Pokemon available (maybe they're all locked = true ?");

            List<Pokemon> zoneFilteredPokemon = [];

            foreach (Pokemon pokemon in pokemonsAvailable)
            {
                if (!pokemon.IsZoneExclusive || pokemon.ZonesList.Any(z => z.Name.ToLower() == (zone.Name.ToLower() ?? Commun.GetBaseZone().Name.ToLower())))
                {
                    zoneFilteredPokemon.Add(pokemon);
                }
            }
            pokemonsAvailable = zoneFilteredPokemon;

            if (pokemonsAvailable.Count == 0)
                throw new Exception($"No Pokemon available in the zone {zone.Name}.");

            if (pkb.exclusiveType is not null)
            {
                pokemonsAvailable = pokemonsAvailable.Where(p => p.Type1.ToLower() == pkb.exclusiveType.ToLower() || (p.Type2 is not null && p.Type2.ToLower() == pkb.exclusiveType.ToLower())).ToList();
                if (pokemonsAvailable.Count == 0)
                    throw new Exception($"No Pokemon available (found no creature with type {pkb.exclusiveType} forced by the ball).");
            }
            if (pkb.exclusiveSerie is not null)
            {
                pokemonsAvailable = pokemonsAvailable.Where(p => p.Serie.ToLower() == pkb.exclusiveSerie.ToLower()).ToList();
                if (pokemonsAvailable.Count == 0)
                    throw new Exception($"No Pokemon available (found no creature with serie {pkb.exclusiveSerie} forced by the ball).");
            }

            if (pkb.exclusiveZone is not null)
            {
                pokemonsAvailable = pokemonsAvailable.Where(p => p.ZonesList.Where(zone => zone.Name.ToLower() == pkb.exclusiveZone.ToLower()).Any()).ToList();
                if (pokemonsAvailable.Count == 0)
                    throw new Exception($"No Pokemon available (found no creature with serie {pkb.exclusiveSerie} forced by the ball).");
            }
            if (shinyForced)
            {
                // OPTIM P6 : Random.Shared à la place de new Random()
                var shinyAvailable = pokemonsAvailable.Where(x => !x.isShinyLock).ToList();
                return shinyAvailable[_rng.Next(shinyAvailable.Count)];
            }
            else
                return pokemonsAvailable[_rng.Next(pokemonsAvailable.Count)];
        }
    }
}