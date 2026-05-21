using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class RaidDamageBoost
    {
        public int Multiplicator { get; set; }
        public DateTime? End { get; set; }
        public int? Minute { get; set; }

        public RaidDamageBoost()
        {
        }

        /// <summary>
        /// dans le cas ou on veut une durée indéfinie, on met le max
        /// </summary>
        /// <param name="Multiplier"></param>
        public RaidDamageBoost(int Multiplicator)
        {
            this.Multiplicator = Multiplicator;
            End = DateTime.MaxValue;
        }

        /// <summary>
        /// dans le cas ou un json contiendrais une entrée "Minute"
        /// </summary>
        /// <param name="Multiplier"></param>
        /// <param name="Minute"></param>
        public RaidDamageBoost(int Multiplicator, int Minute)
        {
            this.Multiplicator = Multiplicator;
            End = DateTime.Now.AddMinutes(Minute);
        }

        public void Initialize()
        {
            if (Minute.HasValue)
            {
                End = DateTime.Now.AddMinutes(Minute.Value);
            }
            else
            {
                End = DateTime.MaxValue;
            }
        }

        public bool Validity()
        {
            return DateTime.Now < End;
        }
    }
}