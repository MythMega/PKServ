using System.Collections.Generic;
using System.Linq;
using PKServ.Configuration;

namespace PKServ
{
    internal class AccountTransfert
    {
        // compte qui va perdre toutes ses données
        public User AccountToDelete { get; set; }
        // compte qui va recevoir toutes les données
        public User AccountTarget { get; set; }
        public bool ChangeUsercode { get; set; } = true;
        public DataConnexion DataConnexion { get; set; }


        public AccountTransfert(User accountToDelete, User accountTarget, DataConnexion dataConnexion, bool changeCode)
        {
            AccountToDelete = accountToDelete;
            AccountTarget = accountTarget;
            DataConnexion = dataConnexion;
            ChangeUsercode = changeCode;
        }

        public AccountTransfert(User accountToDelete, User accountTarget, bool changeCode)
        {
            AccountToDelete = accountToDelete;
            AccountTarget = accountTarget;
            ChangeUsercode = changeCode;
        }

        public AccountTransfert(DataConnexion dataConnexion)
        {
            DataConnexion = dataConnexion;
        }

        public AccountTransfert() { }

        public void SetEnv(DataConnexion env)
        {
            DataConnexion = env;
        }

        /// <summary>
        /// Methode de transfert de compte
        /// </summary>
        public string DoTransfert()
        {
            List<Entrie> entriesToTransfert = DataConnexion.GetEntriesByPseudo(pseudoTriggered: AccountToDelete.Pseudo, platformTriggered: AccountToDelete.Platform);
            List<Entrie> entriesToUpdate = DataConnexion.GetEntriesByPseudo(pseudoTriggered: AccountTarget.Pseudo, platformTriggered: AccountTarget.Platform);

            foreach (Entrie entry in entriesToTransfert)
            {
                // l'entrée existe déjà sur le compte target, il faut les fusionner
                if (entriesToUpdate.Where(entr => entr.PokeName == entry.PokeName).Any())
                {
                    Entrie ToValidate = entriesToUpdate.Where(entr => entr.PokeName == entry.PokeName).FirstOrDefault();
                    ToValidate.dateLastCatch = ToValidate.dateLastCatch > entry.dateLastCatch ? ToValidate.dateLastCatch : entry.dateLastCatch;
                    ToValidate.dateFirstCatch = ToValidate.dateFirstCatch < entry.dateFirstCatch ? ToValidate.dateFirstCatch : entry.dateFirstCatch;
                    ToValidate.CountNormal += entry.CountNormal;
                    ToValidate.CountShiny += entry.CountShiny;

                    ToValidate.Validate(NewLine: false);
                }
                // l'entrée n'existe pas, il faut juste changer ses infos pour la faire valider
                else
                {
                    entry.Platform = AccountTarget.Platform;
                    entry.code = AccountTarget.Code_user;
                    entry.Pseudo = AccountTarget.Pseudo;

                    entry.Validate(NewLine: false);

                }

            }

            // Transfert des stats

            AccountTarget.generateStats();
            AccountToDelete.generateStats();

            AccountTarget.Stats.ballLaunched += AccountToDelete.Stats.ballLaunched;
            AccountTarget.Stats.moneySpent += AccountToDelete.Stats.moneySpent;
            AccountTarget.Stats.giveawayNormal += AccountToDelete.Stats.giveawayNormal;
            AccountTarget.Stats.giveawayShiny += AccountToDelete.Stats.giveawayShiny;
            AccountTarget.Stats.scrappedNormal += AccountToDelete.Stats.scrappedNormal;
            AccountTarget.Stats.scrappedShiny += AccountToDelete.Stats.scrappedShiny;
            AccountTarget.Stats.CustomMoney += AccountToDelete.Stats.CustomMoney;

            if (AccountTarget.ValidateStatsBDD())
            {
                AccountToDelete.DeleteAllEntries();
                AccountToDelete.DeleteUser();
                return "Succeed";
            }
            else
            {
                return "Canceled Validation";
            }
        }

    }
}
