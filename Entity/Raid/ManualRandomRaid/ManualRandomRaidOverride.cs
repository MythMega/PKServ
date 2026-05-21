using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Entity.Raid.ManualRandomRaid
{
    public class ManualRandomRaidOverride
    {
        public char? CreatureSelectionMode { get; set; }
        public List<string>? BossCreatureRarity { get; set; }
        public List<string>? BossCreatureSerie { get; set; }
        public List<string>? CreatureList { get; set; }
        public int? BossCreatureShinyRate { get; set; }
        public int? BossCreatureBasePV { get; set; }
        public int? PVAdditionalPerUser { get; set; }
        public bool? IncludeLockCreatures { get; set; }
        public bool? IncludeLegendaryCreatures { get; set; }

        public ManualRandomRaid ToManualRandomRaid(ManualRandomRaid manualRandomRaid)
        {
            if (CreatureSelectionMode is not null)
                manualRandomRaid.CreatureSelectionMode = CreatureSelectionMode.Value;
            if (BossCreatureRarity is not null)
                manualRandomRaid.BossCreatureRarity = BossCreatureRarity;
            if (BossCreatureSerie is not null)
                manualRandomRaid.BossCreatureSerie = BossCreatureSerie;
            if (CreatureList is not null)
                manualRandomRaid.CreatureList = CreatureList;
            if (BossCreatureShinyRate is not null)
                manualRandomRaid.BossCreatureShinyRate = BossCreatureShinyRate.Value;
            if (BossCreatureBasePV is not null)
                manualRandomRaid.BossCreatureBasePV = BossCreatureBasePV.Value;
            if (PVAdditionalPerUser is not null)
                manualRandomRaid.PVAdditionalPerUser = PVAdditionalPerUser.Value;
            if (IncludeLockCreatures is not null)
                manualRandomRaid.IncludeLockCreatures = IncludeLockCreatures.Value;
            if (IncludeLegendaryCreatures is not null)
                manualRandomRaid.IncludeLegendaryCreatures = IncludeLegendaryCreatures.Value;

            return manualRandomRaid;
        }
    }
}