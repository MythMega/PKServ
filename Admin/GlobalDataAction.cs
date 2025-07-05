using PKServ.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}