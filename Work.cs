using PKServ.Binding;
using PKServ.Business;
using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Work
    {
        public string msgResult;
        public UserRequest uc;
        public DataConnexion connexion;
        public AppSettings appSettings;
        public GlobalAppSettings globalAppSettings;

        public Work(UserRequest uc, DataConnexion connexion, AppSettings appSettings, GlobalAppSettings globalAppSettings)
        {
            msgResult = "NO DATA";
            this.uc = uc;
            this.connexion = connexion;
            this.appSettings = appSettings;
            this.globalAppSettings = globalAppSettings;
        }

        public string DoCatchRandomPoke(bool ballForced = false, Pokeball pkbForced = null)
        {
            Pokeball pkb;
            if (!ballForced)
            {
                Trigger selectedTrigger = appSettings.triggers.Where(trigger => trigger.name.ToLower() == uc.TriggerName.ToLower() && trigger.effect == "BALL" && trigger.ballName != null).FirstOrDefault();
                if (selectedTrigger == null)
                {
                    return "ERROR : Name of that trigger not found";
                }
                else
                {
                    pkb = appSettings.pokeballs.Where(ball => ball.Name.ToLower() == selectedTrigger.ballName.ToLower()).FirstOrDefault();
                }
            }
            else
            {
                pkb = pkbForced;
            }
            Zone zone = connexion.GetZoneUser(uc.UserCode, appSettings.Zones);
            CatchPokemon(pkb, zone);
            saveStats();
            return msgResult;
        }

        private void saveStats()
        {
            connexion.UpdateUserStatsMoneyBall(uc.UserName, uc.Platform, 1, uc.Price, uc.UserCode).Wait();
        }

        private void CatchPokemon(Pokeball pkb, Zone zone)
        {
            Pokemon onePoke = appSettings.getOnePokeFromBall(pkb, zone);
            onePoke.isShiny = false;
            int bonusCatchRate = 0;
            int bonusShinyRate = 0;
            // bonus idée
            if (uc.UserName.ToLower() == "zulwantv")
                bonusCatchRate = 5;
            // bonus idée + actif
            if (uc.UserName.ToLower() == "sawancyberpotes")
                bonusCatchRate = 10;
            // cheat pass
            if (uc.UserName.ToLower() == "mythmega")
            {
                bonusCatchRate = 35;
                bonusShinyRate = 15;
                RerollNewPoke(10, ref onePoke, pkb, zone);
            }

            if (pkb.rerollItemForUncaught > 0)
                RerollNewPoke(pkb.rerollItemForUncaught, ref onePoke, pkb, zone);

            bonusCatchRate += DateTime.Now.Hour < 6 || DateTime.Now.Hour > 18 ? pkb.nightAdditionalRate : 0;
            bonusCatchRate += isAlreadyCatch(onePoke) ? pkb.alreadyCaughtAdditionalRate : 0;
            decimal bonCat = pkb.dexRelativeBonusCatchrate / 100;
            bonusCatchRate += (int)Math.Floor(bonCat);

            decimal bonShiny = pkb.dexRelativeBonusShinyrate / 100;
            if (bonShiny > 10) { bonShiny = 10; }
            bonusShinyRate += (int)Math.Floor(bonShiny);

            Random random = new Random();

            int newCatchrate = pkb.catchrate;

            bool validate = pkb.catchrate >= 100;
            while (!validate)
            {
                if (CreatureRarity.ValidateSelection(onePoke.Rarity))
                {
                    validate = true;
                }
                else
                {
                    RerollNewPoke(1, ref onePoke, pkb, zone);
                }
            }

            bool isCaught = random.Next(0, 100) <= newCatchrate + bonusCatchRate;
            bool isShiny = random.Next(0, 100) <= pkb.shinyrate + bonusShinyRate;
            if (onePoke.isShinyLock && isShiny)
            {
                onePoke = appSettings.getOnePokeFromBall(pkb, zone, true);
            }
            onePoke.isShiny = isShiny;
            string preinfo = onePoke.isLegendary ? "LEGENDAIRE ! " : "";
            preinfo += onePoke.isCustom ? "CUSTOM ! " : "";
            string str1 = isCaught ? "Le pokémon " + onePoke.Name_FR + " a été capturé. ✅" : "Le pokémon " + onePoke.Name_FR + " s'est échappé. ❌";
            string str2 = isShiny ? ". ✧✧ Le pokémon était shiny. ✧✧" : "";
            msgResult = preinfo + str1;
            msgResult += str2;
            if (!isCaught)
                return;
            appSettings.catchHistory.Add(new CatchHistory { Ball = pkb, Pokemon = onePoke, User = new User { Pseudo = uc.UserName, Platform = uc.Platform }, shiny = isShiny, price = uc.Price });
            saveData(onePoke);
            onePoke.isShiny = false;
        }

        private void RerollNewPoke(int reroll, ref Pokemon onePoke, Pokeball pkb, Zone zone)
        {
            int count = 0;
            while (isAlreadyCatch(onePoke) && reroll <= count)
            {
                onePoke = appSettings.getOnePokeFromBall(pkb, zone);
                count++;
            }
        }

        private void saveData(Pokemon poke)
        {
            List<Entrie> entriesByPseudo = connexion.GetEntriesByPseudo(uc.UserName, uc.Platform);

            entriesByPseudo.ForEach(e =>
            {
                if (uc.UserCode != "unset" && uc.UserCode != "unset in UserRequest" && (e.code == "unset" || e.code == "unset in UserRequest"))
                {
                    e.code = uc.UserCode;
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
                (!poke.isShiny ? new Entrie(-1, uc.UserName, uc.ChannelSource, uc.Platform, poke.Name_FR, 1, 0, DateTime.Now, DateTime.Now, uc.UserCode) : new Entrie(-1, uc.UserName, uc.ChannelSource, uc.Platform, poke.Name_FR, 0, 1, DateTime.Now, DateTime.Now, uc.UserCode)).Validate(true);
            }
        }

        public bool isAlreadyCatch(Pokemon poke)
        {
            List<Entrie> entriesByPseudo = connexion.GetEntriesByPseudo(uc.UserName, uc.Platform);
            return entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Any();
        }

        public bool isAlreadyCatchByUser(Pokemon poke, User user)
        {
            List<Entrie> entriesByPseudo = connexion.GetEntriesByPseudo(user.Pseudo, user.Platform);
            return entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Any();
        }

        internal string DistributePoke(List<User> usersHere = null)
        {
            string pokename = uc.TriggerName.Split('+')[0];
            bool shiny = uc.TriggerName.Split('+')[1] == "True";
            Pokemon poke = appSettings.pokemons.Where(pok => pok.Name_FR == pokename).FirstOrDefault();
            if (poke == null)
            {
                return "poke not found";
            }
            poke.isShiny = shiny;

            string result = "";
            string mode = "giveaway";
            int count = 0;
            // cas distribution multiple
            if (uc.UserName.StartsWith('+'))
            {
                switch (uc.UserName)
                {
                    case "+Everyone":
                        mode = "Giveaway everyone alltime";
                        result = GiveAwayPoke(false, poke, ref count, null);
                        break;

                    // distribution a tous les gens de la liste de gens actifs (pas encore implémentée)
                    case "+Here":
                        mode = "Giveaway everyone here";
                        result = GiveAwayPoke(true, poke, ref count, usersHere);
                        break;
                }
            }
            // cas distribution unique
            else
            {
                count = 1;
                User target = new User(uc.UserName, uc.Platform);
                target.Code_user = connexion.GetCodeUserByPlatformPseudo(target);
                ObtainPoke(target, poke);
                mode = $"Giveaway {target.Pseudo} [{target.Platform}]";
                result = $"{poke.Name_FR}/{poke.Name_EN} succesfully given to {uc.UserName} [on {uc.Platform}]";
            }
            mode += $" ({count})";
            Commun.AddRecords(mode, poke, poke.isShiny, connexion);
            RecordsGeneratorImpl.GenerateRecords(connexion, appSettings, globalAppSettings);
            return result;
        }

        private string GiveAwayPoke(bool isOnlyForActive, Pokemon poke, ref int count, List<User> usersHere = null)
        {
            string result = "";
            string[] errors = [];

            if (isOnlyForActive)
            {
                foreach (User user in usersHere)
                {
                    try
                    {
                        count++;
                        ObtainPoke(user, poke);
                        connexion.UpdateUserStatsGiveaway(pseudo: user.Pseudo, platform: user.Platform, isShiny: poke.isShiny);
                    }
                    catch (Exception e)
                    {
                        errors.Append($"Error : Error for {user.Pseudo} [{user.Platform}].\n{e.InnerException}\nError Message : {e.Message}\nError Data : {e.Data}");
                    }
                }
                result = $"{poke.Name_EN}/{poke.Name_FR} distributed to {usersHere.Count} users.";
            }
            else
            {
                List<User> users = connexion.GetAllUserPlatforms();
                foreach (User user in users)
                {
                    try
                    {
                        count++;
                        ObtainPoke(user, poke);
                        connexion.UpdateUserStatsGiveaway(pseudo: user.Pseudo, platform: user.Platform, isShiny: poke.isShiny);
                    }
                    catch (Exception e)
                    {
                        errors.Append($"Error : Error for {user.Pseudo} [{user.Platform}].\n{e.InnerException}\nError Message : {e.Message}\nError Data : {e.Data}");
                    }
                }
                result = $"{poke.Name_EN}/{poke.Name_FR} distributed to {users.Count} users.";
            }
            result += "\n" + string.Join("\n", result);
            return result;
        }

        public void ObtainPoke(User user, Pokemon poke)
        {
            user.Code_user = connexion.GetCodeUserByPlatformPseudo(user);
            List<Entrie> entriesByPseudo = connexion.GetEntriesByPseudo(user.Pseudo, user.Platform);
            int count = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Count();
            if (entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).Any())
            {
                Entrie entrie = entriesByPseudo.Where(x => x.PokeName == poke.Name_FR).FirstOrDefault();
                if (poke.isShiny)
                    entrie.CountShiny++;
                else
                    entrie.CountNormal++;
                // a virer a terme
                if ((entrie.code == null || entrie.code == "" || entrie.code == "unset" || entrie.code == "unset in UserRequest" || entrie.code == "unset by code")
                    && user.Code_user != null && user.Code_user != "" && user.Code_user != "unset" && user.Code_user != "unset in UserRequest" && user.Code_user != "unset by code")
                {
                    entrie.code = user.Code_user;
                    foreach (var item in entriesByPseudo)
                    {
                        item.code = user.Code_user;
                        item.Validate(false);
                    }
                }
                entrie.Validate(false);
            }
            else
            {
                (!poke.isShiny ? new Entrie(-1, user.Pseudo, uc.ChannelSource, user.Platform, poke.Name_FR, 1, 0, DateTime.Now, DateTime.Now, user.Code_user) : new Entrie(-1, user.Pseudo, uc.ChannelSource, user.Platform, poke.Name_FR, 0, 1, DateTime.Now, DateTime.Now, user.Code_user)).Validate(true);
            }
        }

        public User CompleteUser(User incompleteUser)
        {
            if (incompleteUser.Code_user != null && incompleteUser.Platform != null)
            {
                incompleteUser.Pseudo = connexion.GetPseudoByPlatformCodeUser(incompleteUser);
            }
            if (incompleteUser.Code_user == null && incompleteUser.Pseudo != null && incompleteUser.Platform != null)
            {
                incompleteUser.Code_user = connexion.GetCodeUserByPlatformPseudo(incompleteUser);
            }
            return incompleteUser;
        }
    }
}