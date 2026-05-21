using PKServ.Binding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid
{
    public class AutoRaidSettings
    {
        public bool Enabled { get; set; } = false; // Activer ou désactiver les raids automatiques, par défaut false (désactivé)
        public int DelayBetweenRaids { get; set; } = 30; // Délai entre les raids automatiques en minutes, par défaut 30 minutes
        public int MaxRaidCountPerSession { get; set; } = 5; // Nombre maximum de raids automatiques, par défaut 5
        public List<string> BossCreatureRarity { get; set; } = [CreatureRarity._ANY]; // Forcer la rareté de la créature, par défaut _ANY pour ignorer la rareté
        public List<string> BossCreatureSerie { get; set; } = []; // Creature Serie (Kanto, Johto, Hoenn, etc.), par défaut null pour ignorer la série
        public int BossCreatureShinyRate { get; set; } = 1024; // une chance sur X d'avoir un shiny, par défaut 1024 (1 chance sur 1024)
        public int BossCreatureBasePV { get; set; } = 100000; // PV de base de la créature, par défaut 100000
        public int PVAdditionalPerUser { get; set; } = 10000; // PV supplémentaires par utilisateur, par défaut 10000
        public bool IncludeLockCreatures { get; set; } = false; // Inclure les créatures verrouillées, par défaut false (ne pas inclure)
        public bool IncludeLegendaryCreatures { get; set; } = false; // Inclure les créatures légendaires, par défaut false (ne pas inclure)

        /// <summary>
        /// Multiplication par rareté.
        /// Common : 1x
        /// Uncommon : 1.5x
        /// Rare : 2x
        /// Epic : 3x
        /// Legendary : 5x
        /// Mythical : 7x
        /// </summary>
        public bool RarityMultiplier { get; set; } = true; // Multiplier les PV par la rareté de la créature, par défaut true (actif)
    }
}