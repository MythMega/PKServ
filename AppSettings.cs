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

        public readonly List<Badge> badges = [];

        public Dictionary<string, string> trads = [];

        public List<CatchHistory> catchHistory = [];

        public DateTime LastFullExport = DateTime.MinValue;

        public AppSettings()
        {
        }

        public Pokemon getOnePoke() => this.pokemons.Where(x => !x.isLock).ToList()[new Random().Next(this.pokemons.Where(x => !x.isLock).Count())];

        public Pokemon getOnePokeShiny() => this.pokemons.Where(x => !x.isLock && !x.isShinyLock).ToList()[new Random().Next(this.pokemons.Where(x => !x.isLock && !x.isShinyLock).Count())];

        public Pokemon getOnePoke(string type)
        {
            var a = this.pokemons.Where(x => !x.isLock).ToList();
            List<Pokemon> pokemonsCompatibles = new List<Pokemon>();
            foreach (var poke in a)
            {
                try
                {
                    if(poke.Type2 is null) { poke.Type2 = ""; }
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
            return pokemonsCompatibles[new Random().Next(pokemonsCompatibles.Count)];
        }

        public string GetText(string code) => this.trads[code];

        public int GetIdPokeByPoke(Pokemon poke) => this.pokemons.IndexOf(this.pokemons.Where(w => w.Name_FR == poke.Name_FR).FirstOrDefault());

        public int GetIdPokeByName(string pokeName) => this.pokemons.IndexOf(this.pokemons.Where(w => w.Name_FR == pokeName).FirstOrDefault());
    }
}