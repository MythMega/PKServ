using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid.ManualRandomRaid
{
    public class ManualRandomRaidRequest
    {
        public ManualRandomRaidOverride? ManualRandomRaid { get; set; }

        public User UserTrigger { get; set; }
    }
}