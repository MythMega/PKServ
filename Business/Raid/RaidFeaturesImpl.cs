using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business.Raid
{
    public class RaidFeaturesImpl
    {
        public static int MultiplyPVByRarity(int pVMax, string rarity)
        {
            int multiplier = rarity.ToUpper() switch
            {
                Binding.CreatureRarity.COMMON => 1,
                Binding.CreatureRarity.UNCOMMON => 2,
                Binding.CreatureRarity.RARE => 3,
                Binding.CreatureRarity.EPIC => 4,
                Binding.CreatureRarity.LEGENDARY => 5,
                Binding.CreatureRarity.MYTHICAL => 6,
                _ => throw new ArgumentException("Invalid rarity specified.")
            };

            return pVMax * multiplier;
        }
    }
}