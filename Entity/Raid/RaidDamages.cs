using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class RaidDamagesHistory
    {
        public List<RaidDamages> History { get; set; }
    }

    public class RaidDamages
    {
        public User User { get; set; }
        public int Damages { get; set; }
        public bool Active { get; set; } = true;
        public bool Heal { get; set; } = false;
        public bool Critical { get; set; } = false;
    }
}