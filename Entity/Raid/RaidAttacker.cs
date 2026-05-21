using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class RaidAttacker(User User, string ChannelSource)
    {
        public User User { get; set; } = User;
        public string ChannelSource { get; set; } = ChannelSource;
    }
}