using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Users
{
    public static class UserStatsImpl
    {
        public static string GetUserStatsHTML(AppSettings appSettings, GlobalAppSettings globalAppSettings, PKServ.User user)
        {
            string data = "";

            data += $"<p>Nombre d'espèce enregistrée : {user.Stats.dexCount} / {appSettings.pokemons.Count}</p>";
            float dexProgressPourcent = user.Stats.dexCount * 100 / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexProgressPourcent}%;\" aria-valuenow=\"{dexProgressPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexProgressPourcent}%</div>\r\n</div>";
            data += $"<p>Nombre d'espèce shiny enregistrée : {user.Stats.shinydex}</p>";
            float dexShinyPourcent = user.Stats.shinydex * 100 / appSettings.pokemons.Count;
            data += $"<div class=\"progress\">\r\n  <div class=\"progress-bar\" role=\"progressbar\" style=\"width: {dexShinyPourcent}%;\" aria-valuenow=\"{dexShinyPourcent}\" aria-valuemin=\"0\" aria-valuemax=\"100\">{dexShinyPourcent}%</div>\r\n</div>";
            data += "<br>";

            data += $"<p>Total argent dépensé : {user.Stats.moneySpent}</p>";
            data += $"<p>Total de ball lancées : {user.Stats.ballLaunched}</p>";
            data += "<br>";

            data += $"<p>Nombre de pokémon non shiny capturé : {user.Stats.normalCaught - user.Stats.giveawayNormal}</p>";
            data += $"<p>Nombre de pokémon shiny capturé : {user.Stats.shinyCaught - user.Stats.giveawayShiny}</p>";
            data += $"<p>Total de pokémon attrapé : {user.Stats.pokeCaught - (user.Stats.giveawayNormal + user.Stats.giveawayShiny)}</p>";
            data += $"<p>Pokémon le plus attrapé : {user.Stats.favoritePoke}</p>";
            TimeSpan diff = DateTime.Now - user.Stats.firstCatch;
            data += $"<p>Dresseur depuis : {user.Stats.firstCatch} (depuis {diff.Days} jours.)</p>";

            return data;
        }
    }
}