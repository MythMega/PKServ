using PKServ.Binding;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public class CatchingImpl(AppSettings appSettings, GlobalAppSettings globalAppSettings, DataConnexion dataConnexion)
    {
        private readonly AppSettings _appSettings = appSettings;
        private readonly GlobalAppSettings _globalAppSettings = globalAppSettings;
        private readonly DataConnexion _dataConnexion = dataConnexion;

        public string msgResult = string.Empty;

        public async Task<string> Capture(BallThrowTreatement request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), "BallThrowTreatement request cannot be null");
            }
            // Validate the user
            if (request.User == null)
            {
                throw new ArgumentException("User cannot be null", nameof(request.User));
            }
            // Validate the ball
            if (request.Ball == null)
            {
                throw new ArgumentException("Pokeball cannot be null", nameof(request.Ball));
            }
            // Process the capture logic here
            return await DoCatchRandomPoke(request);
        }

        public async Task<string> DoCatchRandomPoke(BallThrowTreatement ballThrowTreatement)
        {
            await CatchPokemon(ballThrowTreatement);
            await SaveStats(ballThrowTreatement);
            return msgResult;
        }

        private async Task SaveStats(BallThrowTreatement ballThrowTreatement)
        {
            await _dataConnexion.UpdateUserStatsMoneyBall(ballThrowTreatement.User.Pseudo, ballThrowTreatement.User.Platform, 1, ballThrowTreatement.Price, ballThrowTreatement.User.Code_user);
        }

        private async Task CatchPokemon(BallThrowTreatement ballThrowTreatement)
        {
            Pokemon onePoke = appSettings.getOnePokeFromBall(ballThrowTreatement.Ball, ballThrowTreatement.User.Location);
            List<Entrie> entriesByPseudo = await _dataConnexion.GetEntriesByPseudoAsync(ballThrowTreatement.User.Pseudo, ballThrowTreatement.User.Platform);
            onePoke.isShiny = false;
            int bonusCatchRate = 0;
            int bonusShinyRate = 0;
            // bonus idée Zulwan
            if (ballThrowTreatement.User.Code_user == "495923644" || ballThrowTreatement.User.Code_user == "UCvC9vajQ3jskWCHv7dncwbw")
                bonusCatchRate = 5;
            // bonus idée + actif
            if (ballThrowTreatement.User.Pseudo.ToLower() == "sawancyberpotes")
                bonusCatchRate = 10;
            // cheat pass
            if (ballThrowTreatement.User.Code_user == "UCjfy-kv-tDhFKbgfMcCwokw" ||
                ballThrowTreatement.User.Code_user == "157090915" ||
                ballThrowTreatement.User.Code_user == "6914082147880846338")
            {
                bonusCatchRate = 35;
                bonusShinyRate = 15;
                RerollNewPoke(10, ref onePoke, ballThrowTreatement, entriesByPseudo);
            }

            if (ballThrowTreatement.Ball.rerollItemForUncaught > 0)
                RerollNewPoke(ballThrowTreatement.Ball.rerollItemForUncaught, ref onePoke, ballThrowTreatement, entriesByPseudo);

            bonusCatchRate += DateTime.Now.Hour < 6 || DateTime.Now.Hour > 18 ? ballThrowTreatement.Ball.nightAdditionalRate : 0;
            bonusCatchRate += entriesByPseudo.Where(x => x.PokeName == onePoke.Name_FR).Any() ? ballThrowTreatement.Ball.alreadyCaughtAdditionalRate : 0;
            decimal bonCat = ballThrowTreatement.Ball.dexRelativeBonusCatchrate / 100;
            bonusCatchRate += (int)Math.Floor(bonCat);

            decimal bonShiny = ballThrowTreatement.Ball.dexRelativeBonusShinyrate / 100;
            if (bonShiny > 10) { bonShiny = 10; }
            bonusShinyRate += (int)Math.Floor(bonShiny);

            Random random = new Random();

            int newCatchrate = ballThrowTreatement.Ball.catchrate;

            bool validate = ballThrowTreatement.Ball.catchrate >= 100 || onePoke.Rarity == CreatureRarity.COMMON;
            while (!validate && onePoke.Rarity != CreatureRarity.COMMON)
            {
                if (CreatureRarity.ValidateSelection(onePoke.Rarity))
                {
                    validate = true;
                }
                else
                {
                    onePoke = appSettings.getOnePokeFromBall(ballThrowTreatement.Ball, ballThrowTreatement.User.Location);
                }
            }

            bool isCaught = random.Next(0, 100) <= newCatchrate + bonusCatchRate;
            bool isShiny = random.Next(0, 100) <= ballThrowTreatement.Ball.shinyrate + bonusShinyRate;

            if (onePoke.isShinyLock && isShiny)
            {
                onePoke = appSettings.getOnePokeFromBall(ballThrowTreatement.Ball, ballThrowTreatement.User.Location, true);
            }
            onePoke.isShiny = isShiny;
            string preinfo = onePoke.isLegendary ? "LEGENDAIRE ! " : "";
            preinfo += onePoke.isCustom ? "CUSTOM ! " : "";
            string str1 = isCaught ? "Le pokémon " + onePoke.Name_FR + " a été capturé. ✅" : "Le pokémon " + onePoke.Name_FR + " s'est échappé. ❌";
            string str2 = isShiny ? ". ✧✧ Le pokémon était shiny. ✧✧" : "";
            msgResult = preinfo + str1;
            msgResult += str2;
            if (isCaught)
                msgResult += $" [{ballThrowTreatement.User.Location.Name}]";
            if (!isCaught)
                return;
            appSettings.catchHistory.Add(new CatchHistory { Ball = ballThrowTreatement.Ball, Pokemon = onePoke, User = new User { Pseudo = ballThrowTreatement.User.Pseudo, Platform = ballThrowTreatement.User.Platform }, shiny = isShiny, price = ballThrowTreatement.Price });
            SaveData(onePoke, ballThrowTreatement);
            onePoke.isShiny = false;
        }

        private void RerollNewPoke(int reroll, ref Pokemon onePoke, BallThrowTreatement ballThrowTreatement, List<Entrie> entriesByPseudo)
        {
            int count = 0;
            string pokemonam = onePoke.Name_FR;
            while (entriesByPseudo.Where(x => x.PokeName == pokemonam).Any() && reroll <= count)
            {
                onePoke = appSettings.getOnePokeFromBall(ballThrowTreatement.Ball, ballThrowTreatement.User.Location);
                pokemonam = onePoke.Name_FR;
                count++;
            }
        }

        private void SaveData(Pokemon poke, BallThrowTreatement ballThrowTreatement)
        {
            List<Entrie> entriesByPseudo = _dataConnexion.GetEntriesByPseudo(ballThrowTreatement.User.Pseudo, ballThrowTreatement.User.Platform);

            entriesByPseudo.ForEach(e =>
            {
                if (ballThrowTreatement.User.Code_user != "unset" && ballThrowTreatement.User.Code_user != "unset in UserRequest" && (e.code == "unset" || e.code == "unset in UserRequest"))
                {
                    e.code = ballThrowTreatement.User.Code_user;
                    e.Validate(false);
                }
            });

            int count = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Count();
            if (entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Any())
            {
                Entrie entrie = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).FirstOrDefault();
                if (poke.isShiny)
                    entrie.CountShiny++;
                else
                    entrie.CountNormal++;
                entrie.Validate(false);
            }
            else
            {
                int pokeCaught = entriesByPseudo.Count;
                int totalPoke = appSettings.pokemons.Count;
                msgResult += $" ♥ Nouveau Pokémon ♥ [{pokeCaught + 1}/{totalPoke}].";
                (!poke.isShiny ?
                    new Entrie(-1, ballThrowTreatement.User.Pseudo, ballThrowTreatement.ChannelSource, ballThrowTreatement.User.Platform, poke.Name_FR, 1, 0, DateTime.Now, DateTime.Now, ballThrowTreatement.User.Code_user) :
                    new Entrie(-1, ballThrowTreatement.User.Pseudo, ballThrowTreatement.ChannelSource, ballThrowTreatement.User.Platform, poke.Name_FR, 0, 1, DateTime.Now, DateTime.Now, ballThrowTreatement.User.Code_user)).Validate(true);
            }
        }
    }
}