using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PKServ.Binding;
using PKServ.Entity.Raid;

namespace PKServ.Business.Raid
{
    public static class RaidStatutDistributionImpl
    {
        public static string ProcessApplication(PKServ.Raid activeRaid, RaidStatutApplication raidStatutApplication)
        {
            if (activeRaid is null)
            {
                throw new Exception("RaidStatutDistributionImpl.ProcessApplication > activeRaid is null");
            }
            if (activeRaid.LastAttackerUserCode is null)
            {
                throw new Exception("RaidStatutDistributionImpl.ProcessApplication > activeRaid.LastAttackerUserCode is null");
            }
            if (activeRaid.UserRaidStats is null || activeRaid.UserRaidStats.Count <= 0)
            {
                throw new Exception("RaidStatutDistributionImpl.ProcessApplication > activeRaid.UserRaidStats is null OR activeRaid.UserRaidStats.Count <= 0");
            }

            // Initalisation des valeurs.
            string res = string.Empty;
            List<string> targets = new List<string>();

            // Liste des personnes affectées
            switch (raidStatutApplication.Mode)
            {
                case RaidEffectMode.MODE_LAST:
                    // la liste ne doit contenir que la dernière personne ayant participé au raid
                    targets = [activeRaid.LastAttackerUserCode];
                    break;

                case RaidEffectMode.MODE_RANDOM:
                    // la liste ne doit contenir qu'une des personne ayant participé au raid
                    string usercode = activeRaid.UserRaidStats[new Random().Next(activeRaid.UserRaidStats.Count)].User.Code_user;
                    targets = [usercode];
                    break;

                case RaidEffectMode.MODE_EVERYONE:
                    // la liste doit contenir toutes les personnes ayant participé au raid
                    activeRaid.UserRaidStats.ForEach(urs => targets.Add(urs.User.Code_user));
                    break;
            }

            foreach (string targetUser in targets)
            {
                if (activeRaid.UserCodeStatut.ContainsKey(targetUser))
                {
                    activeRaid.UserCodeStatut.Remove(targetUser);
                }

                ApplyEffect(activeRaid, raidStatutApplication.Effect, targetUser);
            }

            return res;
        }

        private static void ApplyEffect(PKServ.Raid activeRaid, string effect, string targetUser)
        {
            if (activeRaid.UserCodeStatut.ContainsKey(targetUser))
            {
                activeRaid.UserCodeStatut.Remove(targetUser);
            }

            if (effect == StatutBinding.RANDOM_STATUT)
            {
                effect = RandomStatut();
            }

            switch (effect)
            {
                case StatutBinding.RANDOM_STATUT:
                case StatutBinding.HEAL_STATUT:
                    break;

                case StatutBinding.STATUT_HEALINGFOUNTAIN:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_HEALINGFOUNTAIN, 3, DateTime.Now);
                    break;

                case StatutBinding.STATUT_FROZEN:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_FROZEN, 5, DateTime.Now);
                    break;

                case StatutBinding.STATUT_BURNT:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_BURNT, 5, DateTime.Now);
                    break;

                case StatutBinding.STATUT_ASLEEP:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_ASLEEP, 3, DateTime.Now);
                    break;

                case StatutBinding.STATUT_CONFUSED:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_CONFUSED, 1, DateTime.Now);
                    break;

                case StatutBinding.STATUT_BACKWIND:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_BACKWIND, 3, DateTime.Now);
                    break;

                case StatutBinding.STATUT_KO:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_KO, 0, DateTime.Now.AddMinutes(5));
                    break;

                case StatutBinding.STATUT_PARALYZED:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_PARALYZED, 3, DateTime.Now);
                    break;

                case StatutBinding.STATUT_POISONED:
                    activeRaid.UserCodeStatut[targetUser] = (StatutBinding.STATUT_POISONED, 3, DateTime.Now);
                    break;
            }
        }

        public static string RandomStatut()
        {
            Random random = new();
            List<string> list =
            [
                "STATUT_PARALYZED",
                "STATUT_KO",
                "STATUT_FROZEN",
                "STATUT_BURNT",
                "STATUT_CONFUSED",
                "STATUT_BACKWIND",
                "STATUT_ASLEEP",
                "STATUT_HEALINGFOUNTAIN",
                "STATUT_POISONED"
            ];

            return list
                .OrderBy(_ => random.Next())
                .First();
        }
    }
}