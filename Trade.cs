using System.Collections.Generic;
using System.Linq;

namespace PKServ
{
    internal class Trade
    {
        public User trader1;
        public User trader2;
        public Pokemon pokemon1;
        public Pokemon pokemon2;
        public DataConnexion cnx;

        public Trade(User trader1, User trader2, Pokemon pokemon1, Pokemon pokemon2)
        {
            this.trader1 = trader1;
            this.trader2 = trader2;
            this.pokemon1 = pokemon1;
            this.pokemon2 = pokemon2;
            this.cnx = new DataConnexion();
        }

        /// <summary>
        /// méthode principale, retourne le string de retours
        /// </summary>
        /// <returns></returns>
        internal string DoWork()
        {
            List<Entrie> entrie_User1 = cnx.GetEntriesByPseudo(trader1.Pseudo, trader1.Platform);
            List<Entrie> entrie_User2 = cnx.GetEntriesByPseudo(trader2.Pseudo, trader2.Platform);

            bool trader1FillCondition = CheckCondition(entrie_User1, pokemon1);
            bool trader2FillCondition = CheckCondition(entrie_User2, pokemon2);

            if (trader2FillCondition && trader1FillCondition)
            {
                // Enlever les Pokémon des utilisateurs initiaux
                UpdateEntries(entrie_User1, pokemon1, increase: false);
                UpdateEntries(entrie_User2, pokemon2, increase: false);

                // Ajouter les Pokémon aux nouveaux utilisateurs
                UpdateEntries(entrie_User1, pokemon2, increase: true);
                UpdateEntries(entrie_User2, pokemon1, increase: true);

                string state1 = pokemon1.isShiny ? "(Shiny)" : "(normal)";
                string state2 = pokemon2.isShiny ? "(Shiny)" : "(normal)";

                List<Entrie> entries = new List<Entrie>();
                entries.AddRange(entrie_User1);
                entries.AddRange(entrie_User2);
                entries.ForEach(x => x.PreValidate(cnx));

                return $"{trader1.Pseudo} sent {pokemon1.Name_EN}/{pokemon1.Name_FR} {state1} and received {pokemon2.Name_EN}/{pokemon2.Name_FR} {state2}\n{trader2.Pseudo} sent {pokemon2.Name_EN}/{pokemon2.Name_FR} {state2} and received {pokemon1.Name_EN}/{pokemon1.Name_FR} {state1}";
            }
            else
            {
                return GenerateBadResult(trader1FillCondition, trader2FillCondition, trader1.Pseudo, pokemon1, trader2.Pseudo, pokemon2);
            }
        }

        private bool CheckCondition(List<Entrie> entries, Pokemon pokemon)
        {
            return pokemon.isShiny
                ? entries.Any(p => p.PokeName == pokemon.Name_FR && p.CountShiny >= 1)
                : entries.Any(p => p.PokeName == pokemon.Name_FR && p.CountNormal >= 1);
        }

        private void UpdateEntries(List<Entrie> entries, Pokemon pokemon, bool increase)
        {
            var entry = entries.FirstOrDefault(p => p.PokeName == pokemon.Name_FR);
            if (entry != null)
            {
                if (pokemon.isShiny)
                {
                    entry.CountShiny += increase ? 1 : -1;
                }
                else
                {
                    entry.CountNormal += increase ? 1 : -1;
                }
            }
            else if (increase)
            {
                // Ajouter une nouvelle entrée si elle n'existe pas et si on doit augmenter le compteur
                entry = new Entrie(trader1.Pseudo, null, trader1.Platform, pokemon.Name_FR)
                {
                    CountShiny = pokemon.isShiny ? 1 : 0,
                    CountNormal = pokemon.isShiny ? 0 : 1
                };
                entries.Add(entry);
            }
        }

        private string GenerateBadResult(bool trader1Condition, bool trader2Condition, string pseudo1, Pokemon poke1, string pseudo2, Pokemon poke2)
        {
            string badresult = "trade cannot be completed. ";
            if (!trader1Condition)
            {
                badresult += $"{pseudo1} doesn't have the {poke1.Name_EN}/{poke1.Name_FR}. ";
            }
            if (!trader2Condition)
            {
                badresult += $"{pseudo2} doesn't have the {poke2.Name_EN}/{poke2.Name_FR}. ";
            }
            return badresult;
        }
    }
}