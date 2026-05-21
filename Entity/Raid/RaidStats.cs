using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class RaidStats
    {
        public Dictionary<string, int> PlatformDamage { get; set; } = [];
        public Dictionary<string, int> UserDamageCount { get; set; } = [];
        public Dictionary<string, int> UserDamageTotal { get; set; } = [];

        public RaidStats()
        {
        }
    }
}