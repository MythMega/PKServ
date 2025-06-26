using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity._DATA
{
    public class BDD_USER
    {
        public int? Id { get; set; }
        public string CODE_USER { get; set; }
        public string? Pseudo { get; set; }
        public string? Platform { get; set; }
        public int Stat_BallLaunched { get; set; } = 0;
        public int Stat_MoneySpent { get; set; } = 0;
        public int pokeReceived_normal { get; set; } = 0;
        public int pokeReceived_shiny { get; set; } = 0;
        public int pokeScrapped_normal { get; set; } = 0;
        public int pokeScrapped_shiny { get; set; } = 0;
        public int customMoney { get; set; } = 0;
        public int Stat_tradeCount { get; set; } = 0;
        public int Stat_RaidCount { get; set; } = 0;
        public int Stat_RaidTotalDmg { get; set; } = 0;
        public string? favoriteCreature { get; set; }
        public string? avatarUrl { get; set; }
        public string? cardsUrl { get; set; }
        public string selectedZone { get; set; } = Commun.GetBaseZone().Name;

        public static User FromBDD(BDD_USER data, AppSettings appSettings)
        {
            return new User
            {
                Id = data.Id,
                Code_user = data.CODE_USER,
                Pseudo = data.Pseudo,
                Platform = data.Platform,
                Stats = new Stats
                {
                    ballLaunched = data.Stat_BallLaunched,
                    moneySpent = data.Stat_MoneySpent,
                    giveawayNormal = data.pokeReceived_normal,
                    giveawayShiny = data.pokeReceived_shiny,
                    scrappedNormal = data.pokeScrapped_normal,
                    scrappedShiny = data.pokeScrapped_shiny,
                    CustomMoney = data.customMoney,
                    TradeCount = data.Stat_tradeCount,
                    RaidCount = data.Stat_RaidCount
                },
                FavoritePoke = BDD_USER.PokeFavToPoke(data.favoriteCreature, appSettings),
                AvatarUrl = data.avatarUrl,
                CardBackgroundUrl = data.cardsUrl,
                Location = appSettings.Zones.FirstOrDefault(zone => Commun.CompareStrings(data.selectedZone, zone.Name))
            };
        }

        public static BDD_USER ToBDD(User user, AppSettings appSettings)
        {
            // Crée un nouvel objet BDD_USER à partir de l'objet User
            var data = new BDD_USER
            {
                Id = user.Id,
                CODE_USER = user.Code_user,
                Pseudo = user.Pseudo,
                Platform = user.Platform,

                // Stats
                Stat_BallLaunched = user.Stats.ballLaunched,
                Stat_MoneySpent = user.Stats.moneySpent,
                pokeReceived_normal = user.Stats.giveawayNormal,
                pokeReceived_shiny = user.Stats.giveawayShiny,
                pokeScrapped_normal = user.Stats.scrappedNormal,
                pokeScrapped_shiny = user.Stats.scrappedShiny,
                customMoney = user.Stats.CustomMoney,
                Stat_tradeCount = user.Stats.TradeCount,
                Stat_RaidCount = user.Stats.RaidCount,
                Stat_RaidTotalDmg = user.Stats.RaidTotalDmg,  // si présent dans Stats

                // Liens et préférences
                favoriteCreature = user.FavoritePoke != null
                    ? new BDD_USER().PokeToPokeFav(user.FavoritePoke)
                    : null,
                avatarUrl = user.AvatarUrl,
                cardsUrl = user.CardBackgroundUrl,

                // Zone sélectionnée (on écrit son Name, ou on remet "<void>" par défaut)
                selectedZone = user.Location != null
                    ? user.Location.Name
                    : Commun.GetBaseZone().Name,
            };

            return data;
        }

        public string PokeToPokeFav(Pokemon pokemon)
        {
            return pokemon.Name_FR + "#" + (pokemon.isShiny ? "s" : "n");
        }

        public static Pokemon PokeFavToPoke(string pokefav, AppSettings appSettings)
        {
            try
            {
                var decomposedPokefav = pokefav.Split('#');
                if (decomposedPokefav.Length != 2)
                {
                    throw new Exception($"error while decomposing pokefav PKServ.Entity._DATA.BDD_USER.PokeFavToPoke {decomposedPokefav}");
                }
                string name = pokefav.Split('#')[0];
                Pokemon result = appSettings.allPokemons.FirstOrDefault(f => Commun.isSamePoke(f, name));
                if (result == null)
                {
                    throw new Exception("Creature not found PKServ.Entity._DATA.BDD_USER.PokeFavToPoke");
                }
                result.isShiny = decomposedPokefav[1].ToLower().Equals("s");
                return result;
            }
            catch
            {
                throw new Exception("Error occured PKServ.Entity._DATA.BDD_USER.PokeFavToPoke");
            }
        }
    }
}