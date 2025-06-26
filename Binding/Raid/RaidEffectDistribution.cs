using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Binding.Raid
{
    public class RaidEffectDistribution(string effect, string mode)
    {
        public string Effect { get; set; } = effect;
        public string Mode { get; set; } = mode;
    }

    public static class RaidEffectMode
    {
        public const string MODE_EVERYONE = "EVERYONE";
        public const string MODE_RANDOM = "RANDOM";
        public const string MODE_LAST = "LAST";
    }
}