using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Binding
{
    public static class StatutBinding
    {
        public const string STATUT_PARALYZED = "PARALYZED";
        public const string STATUT_KO = "KO";
        public const string STATUT_FROZEN = "FROZEN";
        public const string STATUT_BURNT = "BURNT";
        public const string STATUT_CONFUSED = "CONFUSED";
        public const string STATUT_BACKWIND = "BACKWIND";
        public const string STATUT_ASLEEP = "ASLEEP";
        public const string STATUT_HEALINGFOUNTAIN = "HEALING";
        public const string STATUT_POISONED = "POISONED";

        public const string RANDOM_STATUT = "<RANDOM>";
        public const string HEAL_STATUT = "<HEAL>";
    }

    public static class RaidEffectMode
    {
        public const string MODE_EVERYONE = "EVERYONE";
        public const string MODE_RANDOM = "RANDOM";
        public const string MODE_LAST = "LAST";
    }
}