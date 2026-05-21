using PKServ.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Raid
{
    public static class CreatureImpl
    {
        public static Pokemon GetPokeRandom(List<string> rarity, List<string> serie, List<string> type, bool includeLocked, bool includeLegendary, AppSettings settings)
        {
            List<Pokemon> pokemons = settings.pokemons;
            if (rarity is not null && rarity.Count != 0 && !rarity.Contains(CreatureRarity._ANY))
            {
                pokemons = [.. pokemons.Where(p => rarity.Any(filter => Commun.CompareStrings(filter, p.Rarity)))];
            }
            if (serie is not null && serie.Count != 0)
            {
                pokemons = [.. pokemons.Where(p => serie.Any(filter => Commun.CompareStrings(filter, p.Serie)))];
            }
            if (type is not null && type.Count != 0)
            {
                pokemons = [.. pokemons.Where(p => type.Any(filter => Commun.CompareStrings(filter, p.Type1) || Commun.CompareStrings(filter, p.Type2)))];
            }
            if (!includeLocked)
            {
                pokemons = [.. pokemons.Where(p => !p.isLock)];
            }
            if (!includeLegendary)
            {
                pokemons = [.. pokemons.Where(p => !p.isLegendary)];
            }

            if (pokemons.Count == 0)
            {
                throw new Exception("No Creature available with the specified filters.");
            }

            Random random = new Random();
            int randomIndex = random.Next(pokemons.Count);
            return pokemons[randomIndex];
        }

        internal static Pokemon GetPokeRandomFromNameList(List<string> creatureList, AppSettings settings)
        {
            List<Pokemon> pokemons = settings.pokemons;

            if (creatureList is not null && creatureList.Count != 0)
            {
                pokemons = [.. pokemons.Where(p => creatureList.Any(filter => Commun.isSamePoke(p, filter)))];
            }
            Random random = new Random();
            int randomIndex = random.Next(pokemons.Count);
            return pokemons[randomIndex];
        }
    }
}