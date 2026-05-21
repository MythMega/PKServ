using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class RaidStatutApplication(string effect, string mode)
    {
        public string Effect { get; set; } = effect;
        public string Mode { get; set; } = mode;
    }
}