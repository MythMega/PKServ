using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ
{
    internal class Raid
    {
        public Dictionary<User, int> UserDamage { get; set; }
        public Pokemon Boss { get; set; }
        public int PV { get; set; }
        public int CatchRate { get; set; }
        public int ShinyRate { get; set; }

        public Raid(string bossName, int pV, int catchRate, int shinyRate)
        {
            UserDamage = [];
            Boss = ;
            PV = pV;
            CatchRate = catchRate;
            ShinyRate = shinyRate;
        }
    }
}
