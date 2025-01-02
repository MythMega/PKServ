using System;
using System.Collections.Generic;
using System.Linq;

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
            this.msgResult = "NO DATA";
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
            this.CatchPokemon(pkb);
            this.saveStats();
            return this.msgResult;
        }

        private void saveStats()
        {
            connexion.UpdateUserStatsMoneyBall(uc.UserName, uc.Platform, 1, uc.Price, uc.UserCode);
        }

        private void CatchPokemon(Pokeball pkb)
        {
            Pokemon onePoke = pkb.eclusiveType == null ? appSettings.getOnePoke() : appSettings.getOnePoke(pkb.eclusiveType);
            onePoke.isShiny = false;
            int bonusCatchRate = 0;
            int bonusShinyRate = 0;
            // bonus idée
            if (uc.UserName.ToLower() == "zulwantv")
                bonusCatchRate = 5;
            // bonus idée
            if (uc.UserName.ToLower() == "sawancyberpotes")
                bonusCatchRate = 5;
            // cheat pass
            if (uc.UserName.ToLower() == "mythmega")
            {
                bonusCatchRate = 35;
                bonusShinyRate = 15;
                RerollNewPoke(10, ref onePoke);
            }

            if (pkb.rerollItemForUncaught > 0)
                RerollNewPoke(pkb.rerollItemForUncaught, ref onePoke);

            bonusCatchRate += DateTime.Now.Hour < 6 || DateTime.Now.Hour > 18 ? pkb.nightAdditionalRate : 0;
            bonusCatchRate += isAlreadyCatch(onePoke) ? pkb.alreadyCaughtAdditionalRate : 0;
            decimal bonCat = pkb.dexRelativeBonusCatchrate / 100;
            bonusCatchRate += (int)Math.Floor(bonCat);

            decimal bonShiny = pkb.dexRelativeBonusShinyrate / 100;
            if (bonShiny > 10) { bonShiny = 10; }
            bonusShinyRate += (int)Math.Floor(bonShiny);

            Random random = new Random();

            int newCatchrate = pkb.catchrate;

            // gestion de la rarité de pokémon. On ignore tout ceux qui ont une rareté à 1 ou moins, et ceux dont la rareté n'est pas donnée
            if (onePoke.rarity is not null && onePoke.rarity > 1)
            {
                // si le catchrate est supérieur a 50%, on augmente les chance de non capture
                if (pkb.catchrate > 50) { 
                    newCatchrate = 100 - (100 - pkb.catchrate) * onePoke.rarity.Value;
                }
                // si le catchrate est inférieur ou égal à 50%, on divise le taux de capture
                else
                {
                    newCatchrate = ((int)(pkb.catchrate / onePoke.rarity.Value));
                }

                // on nerf le malus : une ball ne peut pas avoir un taux de capture en dessous de son quart de son taux de base
                if (newCatchrate < ((int)(pkb.catchrate / 2)))
                    newCatchrate = (int)Math.Floor(decimal.Ceiling(pkb.catchrate / 2));
            }
            //#if DEBUG
            //Console.WriteLine("pokeball rate : " + pkb.catchrate);
            //Console.WriteLine("real catch rate : " + newCatchrate);
            //#endif
            bool isCaught = random.Next(0, 100) <= newCatchrate + bonusCatchRate;
            bool isShiny = random.Next(0, 100) <= pkb.shinyrate + bonusShinyRate;
            if (onePoke.isShinyLock && isShiny)
            {
                onePoke = appSettings.getOnePokeShiny();
            }
            onePoke.isShiny = isShiny;
            string preinfo = onePoke.isLegendary ? "LEGENDAIRE ! " : "";
            preinfo += onePoke.isCustom ? "CUSTOM ! " : "";
            string str1 = isCaught ? "Le pokémon " + onePoke.Name_FR + " a été capturé. ✅" : "Le pokémon " + onePoke.Name_FR + " s'est échappé. ❌";
            string str2 = isShiny ? ". ✧✧ Le pokémon était shiny. ✧✧" : "";
            this.msgResult = preinfo + str1;
            this.msgResult += str2;
            if (!isCaught)
                return;
            this.appSettings.catchHistory.Add(new CatchHistory { Ball = pkb, Pokemon = onePoke, User = new User { Pseudo = uc.UserName, Platform = uc.Platform }, shiny = isShiny, price = uc.Price });
            this.saveData(onePoke);
            onePoke.isShiny = false;
        }

        private void RerollNewPoke(int reroll, ref Pokemon onePoke)
        {
            int count = 0;
            while (isAlreadyCatch(onePoke) && reroll <= count)
            {
                onePoke = appSettings.getOnePoke();
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

        private List<Pokemon> GetPokeCustom(UserRequest uc)
        {
            List<Pokemon> pokeCustom = new List<Pokemon>();

            return pokeCustom;
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
            // cas distribution multiple
            if (uc.UserName.StartsWith('+'))
            {
                switch (uc.UserName)
                {
                    case "+Everyone":
                        result = GiveAwayPoke(false, poke, null);
                        break;

                    // distribution a tous les gens de la liste de gens actifs (pas encore implémentée)
                    case "+Here":
                        result = GiveAwayPoke(true, poke, usersHere);
                        break;
                }
            }
            // cas distribution unique
            else
            {
                User target = new User(uc.UserName, uc.Platform);
                target.Code_user = connexion.GetCodeUserByPlatformPseudo(target);
                ObtainPoke(target, poke);
                result = $"{poke.Name_FR}/{poke.Name_EN} succesfully given to {uc.UserName} [on {uc.Platform}]";
            }
            return result;
        }

        private string GiveAwayPoke(bool isOnlyForActive, Pokemon poke, List<User> usersHere = null)
        {
            string result = "";
            if (isOnlyForActive)
            {
                foreach (User user in usersHere)
                {
                    ObtainPoke(user, poke);
                    connexion.UpdateUserStatsGiveaway(pseudo: user.Pseudo, platform: user.Platform, isShiny: poke.isShiny);
                }
                result = $"{poke.Name_EN}/{poke.Name_FR} distributed to {usersHere.Count} users.";
            }
            else
            {
                List<User> users = connexion.GetAllUserPlatforms();
                foreach (User user in users)
                {
                    ObtainPoke(user, poke);
                    connexion.UpdateUserStatsGiveaway(pseudo: user.Pseudo, platform: user.Platform, isShiny: poke.isShiny);
                }
                result = $"{poke.Name_EN}/{poke.Name_FR} distributed to {users.Count} users.";
            }
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
                if ((entrie.code == null || entrie.code == "" || entrie.code == "unset" || entrie.code == "unset in UserRequest")
                    && user.Code_user != null && user.Code_user != "" && user.Code_user != "unset" && user.Code_user != "unset in UserRequest")
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

        /// <summary>
        /// Lors d'un export de FULL DEX, exporte les dex solo puis le main
        /// </summary>
        /// <returns></returns>
        internal string DoFullExport()
        {
            List<User> users = connexion.GetAllUserPlatforms().Where(user => user.lastCatch() > appSettings.LastFullExport).ToList();
            int count = 0;
            foreach (User user in users)
            {
                uc.UserName = user.Pseudo;
                uc.Platform = user.Platform;

                var data = new ExportSoloDex(appSettings, uc, connexion, globalAppSettings);
                data.filename = $"{uc.UserName}.html";
                data.ExportFile(true).Wait();
                count++;
            }
            var export = new ExportRapport(appSettings, uc, connexion, globalAppSettings);
            export.filename = "main.html";
            export.ExportFile(true, true).Wait();

            appSettings.LastFullExport = DateTime.Now;

            return $"Export done. {count}/{users.Count} personal files created + main file created.";
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