using PKServ.Binding;
using PKServ.Entity.Raid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKServ.Business
{
    public static class SettingsCheckerImpl
    {
        public static void CheckAutoraid(
    AutoRaidSettings autoRaidSettings,
    AppSettings settings)
        {
            // 1. Ne garder que les pokémons déverrouillés
            var available = settings.pokemons
                .Where(p => !p.isLock)
                .ToList();
            if (!available.Any())
                throw new Exception(
                    "No unlocked Pokémon found. Please unlock at least one Pokémon for AutoRaid.");

            // 2. Vérifier chaque série demandée
            if (autoRaidSettings.BossCreatureSerie != null
                && autoRaidSettings.BossCreatureSerie.Any())
            {
                foreach (var serie in autoRaidSettings.BossCreatureSerie)
                {
                    bool exists = available
                        .Any(p => Commun.CompareStrings(p.Serie, serie));
                    if (!exists)
                        throw new Exception(
                            $"The series “{serie}” is not present on any unlocked Pokémon. Please adjust your filters.");
                }
            }

            // 3. Vérifier chaque rareté demandée (sauter le “_ANY”)
            if (autoRaidSettings.BossCreatureRarity != null
                && autoRaidSettings.BossCreatureRarity.Any(r => r != CreatureRarity._ANY))
            {
                foreach (var rarity in autoRaidSettings.BossCreatureRarity
                                                   .Where(r => r != CreatureRarity._ANY))
                {
                    bool exists = available
                        .Any(p => Commun.CompareStrings(p.Rarity, rarity));
                    if (!exists)
                        throw new Exception(
                            $"The rarity “{rarity}” is not present on any unlocked Pokémon. Please adjust your filters.");
                }
            }
        }
    }
}