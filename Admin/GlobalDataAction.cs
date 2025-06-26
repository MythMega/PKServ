using PKServ.Configuration;
using PKServ.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PKServ.Admin
{
    public static class GlobalDataAction
    {
        public static void FixUserCodeDB(DataConnexion _db, bool log)
        {
            List<User> users = _db.GetAllUserPlatforms();
            List<Entrie> entriesToFix = _db.GetAllEntries().Where(x => x.code.ToLower().Contains("unset") && x.Platform != "system").ToList();
            int countFixed = 0;
            int countError = 0;

            foreach (Entrie entry in entriesToFix)
            {
                User user = users.FirstOrDefault(u => entry.Pseudo == u.Pseudo && entry.Platform == u.Platform);
                if (user is not null && !user.Code_user.ToLower().Contains("unset"))
                {
                    entry.code = user.Code_user;
                    countFixed++;
                    entry.Validate(false);
                }
                else
                {
                    if (log)
                    {
                        if (user is not null)
                        {
                            Console.WriteLine($"Error Fixing entry number {entry.id} ([{entry.Platform}] {entry.Pseudo} - {entry.PokeName}) : the user doesn't have valid code !\n");
                        }
                        else
                        {
                            Console.WriteLine($"Error Fixing entry number {entry.id} ([{entry.Platform}] {entry.Pseudo} - {entry.PokeName}) : no user found with that username/platform !\n");
                        }
                    }
                    countError++;
                }
            }

            if (log)
            {
                Console.WriteLine($"\n\n\nFixed entries : {countFixed}.\nErrored entries : {countError}.\n");
            }
        }

        public static async Task UserClean(User user, AppSettings appSettings, DataConnexion data)
        {
            try
             {
                User KnowUser = data.GetUserBaseInfo(user.Code_user, user.Platform, appSettings);
                if (user.AvatarUrl is not null && KnowUser.AvatarUrl != user.AvatarUrl)
                {
                    data.UpdateAvatar(KnowUser.Code_user, user.AvatarUrl);
                    KnowUser.AvatarUrl = user.AvatarUrl;
                }
                if (KnowUser.Pseudo != user.Pseudo)
                {
                    await data.UpdateUserPseudo(KnowUser, user.Pseudo);
                    KnowUser.Pseudo = user.Pseudo;
                }
            }
            catch
            {
                User KnowUser = new User
                {
                    Pseudo = user.Pseudo,
                    Platform = user.Platform,
                    Code_user = user.Code_user,
                    AvatarUrl = user.AvatarUrl,
                    Location = Commun.GetBaseZone(),
                };
                await data.CreateUser(KnowUser);
            }
        }
    }
}