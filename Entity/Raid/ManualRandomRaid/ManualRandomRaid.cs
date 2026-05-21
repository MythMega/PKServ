using PKServ.Binding;
using System.Collections.Generic;

namespace PKServ.Entity.Raid.ManualRandomRaid
{
    public class ManualRandomRaid
    {
        public char CreatureSelectionMode { get; set; } = ManualRandomRaidSelector.CREATURE_SERIE_AND_RARITY; // mode de selection ('S' pour série & rareté, 'L' pour liste de créature)
        public List<string> BossCreatureRarity { get; set; } = [CreatureRarity._ANY]; // Forcer la rareté de la créature, par défaut _ANY pour ignorer la rareté
        public List<string> BossCreatureSerie { get; set; } = []; // Creature Serie (Kanto, Johto, Hoenn, etc.), par défaut vide pour ignorer la série

        public List<string> CreatureList { get; set; } = []; // Liste de créatures possibles, si CreatureSelectionMode = 'C'
        public int BossCreatureShinyRate { get; set; } = 2; // une chance sur X d'avoir un shiny, par défaut 2 (1 chance sur 2)

        public int BossCreatureBasePV { get; set; } = 250000; // PV de base de la créature, par défaut 250000
        public int PVAdditionalPerUser { get; set; } = 10000; // PV supplémentaires par utilisateur, par défaut 10000
        public bool IncludeLockCreatures { get; set; } = false; // Inclure les créatures verrouillées, par défaut false (ne pas inclure)
        public bool IncludeLegendaryCreatures { get; set; } = true; // Inclure les créatures légendaires, par défaut true (inclure)
    }
}