using PKServ.Admin;
using PKServ.Configuration;
using PKServ.Entity._DATA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PKServ.Business.Admin
{
    public static class DataFixerImpl
    {
        public static async Task FixEntries(GlobalAppSettings gas, DataConnexion dc)
        {
            await FixUserInEntries(gas, dc);
        }

        public static async Task FixUsers(DataConnexion dc)
        {
            await FixMultiplesUsers(dc);
        }

        public static async Task FixUserInEntries(GlobalAppSettings gas, DataConnexion dc)
        {
            try
            {
                var entries = dc.GetAllEntries();
                if (entries.Where(x => x.code.ToLower().Contains("unset") && x.Platform != "system").Count() > 0)
                {
                    if (gas.Log.logConsole.console)
                        Console.WriteLine("Fix usernames in databases");
                    GlobalDataAction.FixUserCodeDB(_db: dc, log: gas.Log.logConsole.console);
                }
            }
            catch (Exception e)
            {
                Commun.Logger(e.ToString());
            }
        }

        private static async Task FixMultiplesUsers(DataConnexion dc)
        {
            // Récupère tous les utilisateurs
            List<BDD_USER> allUsers = await dc.GetAllUsersEntitiesAsync();

            // Dictionnaire clé = CODE_USER, valeur = agrégé
            var keptItems = new Dictionary<string, BDD_USER>(StringComparer.OrdinalIgnoreCase);

            foreach (var user in allUsers)
            {
                if (user.CODE_USER is null)
                    continue;

                if (!keptItems.ContainsKey(user.CODE_USER))
                {
                    // On clone pour ne pas modifier l’objet d’origine
                    keptItems[user.CODE_USER] = new BDD_USER
                    {
                        Id = user.Id,
                        CODE_USER = user.CODE_USER,
                        Pseudo = user.Pseudo,
                        Platform = user.Platform,
                        Stat_BallLaunched = user.Stat_BallLaunched,
                        Stat_MoneySpent = user.Stat_MoneySpent,
                        pokeReceived_normal = user.pokeReceived_normal,
                        pokeReceived_shiny = user.pokeReceived_shiny,
                        pokeScrapped_normal = user.pokeScrapped_normal,
                        pokeScrapped_shiny = user.pokeScrapped_shiny,
                        customMoney = user.customMoney,
                        Stat_tradeCount = user.Stat_tradeCount,
                        Stat_RaidCount = user.Stat_RaidCount,
                        Stat_RaidTotalDmg = user.Stat_RaidTotalDmg,
                        favoriteCreature = user.favoriteCreature,
                        avatarUrl = user.avatarUrl,
                        cardsUrl = user.cardsUrl,
                        selectedZone = user.selectedZone
                    };
                }
                else
                {
                    var keep = keptItems[user.CODE_USER];

                    // 1) Pour chaque string : si keep est null et user non-null → on remplace
                    if (keep.Pseudo is null && user.Pseudo is not null)
                        keep.Pseudo = user.Pseudo;
                    if (keep.Platform is null && user.Platform is not null)
                        keep.Platform = user.Platform;
                    if (keep.favoriteCreature is null && user.favoriteCreature is not null)
                        keep.favoriteCreature = user.favoriteCreature;
                    if (keep.avatarUrl is null && user.avatarUrl is not null)
                        keep.avatarUrl = user.avatarUrl;
                    if (keep.cardsUrl is null && user.cardsUrl is not null)
                        keep.cardsUrl = user.cardsUrl;

                    // 2) Pour les int : on conserve la valeur la plus haute
                    keep.Stat_BallLaunched = Math.Max(keep.Stat_BallLaunched, user.Stat_BallLaunched);
                    keep.Stat_MoneySpent = Math.Max(keep.Stat_MoneySpent, user.Stat_MoneySpent);
                    keep.pokeReceived_normal = Math.Max(keep.pokeReceived_normal, user.pokeReceived_normal);
                    keep.pokeReceived_shiny = Math.Max(keep.pokeReceived_shiny, user.pokeReceived_shiny);
                    keep.pokeScrapped_normal = Math.Max(keep.pokeScrapped_normal, user.pokeScrapped_normal);
                    keep.pokeScrapped_shiny = Math.Max(keep.pokeScrapped_shiny, user.pokeScrapped_shiny);
                    keep.customMoney = Math.Max(keep.customMoney, user.customMoney);
                    keep.Stat_tradeCount = Math.Max(keep.Stat_tradeCount, user.Stat_tradeCount);
                    keep.Stat_RaidCount = Math.Max(keep.Stat_RaidCount, user.Stat_RaidCount);
                    keep.Stat_RaidTotalDmg = Math.Max(keep.Stat_RaidTotalDmg, user.Stat_RaidTotalDmg);

                    // 3) Pour l’Id si non défini encore
                    if (keep.Id is null && user.Id is not null)
                        keep.Id = user.Id;
                }
            }

            // IDs à garder et à supprimer
            var keptIds = keptItems.Values
                                      .Select(u => u.Id)
                                      .Where(id => id.HasValue)
                                      .Select(id => id!.Value)
                                      .ToHashSet();

            var allIds = allUsers
                              .Select(u => u.Id)
                              .Where(id => id.HasValue)
                              .Select(id => id!.Value)
                              .ToList();

            var toDelete = allIds.Except(keptIds).ToList();
            var toKeepList = keptItems.Values.ToList();

            // Suppression des doublons
            if (toDelete.Count != 0)
                await dc.DeleteListUsersByIds(toDelete);

            // Mise à jour des enregistrements conservés
            if (toKeepList.Count != 0)
                await dc.UpdateListUsersByIds(toKeepList);
        }
    }
}